using Application.Common.Interfaces;
using Application.Solver.Model;
using Domain.calendar;
using Domain.schedule;
using Domain.university.buildings;
using Domain.workload;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Schedule;

/// <summary>
/// EF Core-реализация поставщика данных солвера для <em>понедельной</em> генерации.
/// На каждую неделю загружаются: понедельные нагрузки (<see cref="WeekWorkload"/>) этой недели
/// (опционально ограниченные институтом), слоты только этой недели, аудитории и веса штрафов.
/// Для институтского режима дополнительно собираются ресурсы, занятые в этой неделе расписанием
/// других институтов (жёсткие блокировки), а в любом режиме — мягкий якорь к прошлому семестру.
///
/// Допущения, требующие проверки на реальных данных:
/// <list type="bullet">
/// <item>принадлежность нагрузки/занятия институту определяется по группам потока
/// (Stream → StreamGroups → Group → Course → Degree → Institute);</item>
/// <item>прошлый семестр — это семестр с наибольшей датой начала, меньшей текущей;</item>
/// <item>занятие из прошлого семестра сопоставляется текущей нагрузке по ключу
/// (преподаватель, дисциплина, тип занятия); слоты переносятся по паре
/// (день недели, номер пары) на слоты текущей недели.</item>
/// </list>
/// </summary>
public sealed class ScheduleDataProvider : IScheduleDataProvider
{
    private readonly ApplicationDbContext _context;

    public ScheduleDataProvider(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ScheduleWeek>> GetWeeksAsync(
        Guid semesterId, CancellationToken cancellationToken) =>
        await _context.Weeks
            .AsNoTracking()
            .Where(w => w.SemesterId == semesterId)
            .OrderBy(w => w.StartDate)
            .Select(w => new ScheduleWeek(w.Id, w.WeekType, w.StartDate, w.EndDate))
            .ToListAsync(cancellationToken);

    public async Task<ScheduleData> GetForWeekAsync(
        Guid semesterId, Guid weekId, Guid? instituteId, CancellationToken cancellationToken)
    {
        var workloads = await LoadWeekWorkloadsAsync(weekId, instituteId, cancellationToken);
        var classrooms = await LoadClassroomsAsync(cancellationToken);
        var timeSlots = await LoadWeekTimeSlotsAsync(weekId, cancellationToken);
        var penalties = await _context.ConstraintConfigs.AsNoTracking().ToListAsync(cancellationToken);

        IReadOnlyList<OccupiedSlot>? occupiedRooms = null;
        IReadOnlyList<OccupiedTeacherSlot>? occupiedTeachers = null;
        if (instituteId is { } inst)
            (occupiedRooms, occupiedTeachers) =
                await LoadOccupiedForWeekAsync(weekId, inst, cancellationToken);

        var anchors = await BuildPreviousSemesterAnchorsAsync(
            semesterId, instituteId, workloads, timeSlots, cancellationToken);

        return new ScheduleData(
            workloads, classrooms, timeSlots, penalties,
            occupiedRooms, occupiedTeachers, anchors, semesterId);
    }

    // ----- Загрузка осей модели --------------------------------------------------------------

    /// <summary>
    /// Понедельные нагрузки недели → ось модели <see cref="WorkloadItem"/>. Часы берутся из
    /// <see cref="WeekWorkload.Hours"/> именно этой недели; нагрузки с нулевыми часами (предмет в эту
    /// неделю не идёт) пропускаются. Identity resolution — чтобы навигации на Teacher/Group/Stream
    /// при разных путях указывали на единые экземпляры (меньше дубликатов в памяти).
    /// </summary>
    private async Task<List<WorkloadItem>> LoadWeekWorkloadsAsync(
        Guid weekId, Guid? instituteId, CancellationToken ct)
    {
        var weekWorkloads = await _context.WeekWorkloads
            .AsNoTrackingWithIdentityResolution()
            .Where(ww => ww.WeekId == weekId && ww.Hours > 0)
            .Where(ww => instituteId == null
                || ww.Curriculum.Stream.StreamGroups
                    .Any(sg => sg.Group.Course.Degree.InstituteId == instituteId.Value))
            .Include(ww => ww.Curriculum).ThenInclude(c => c.Teacher).ThenInclude(t => t.TeacherAvailabilities)
            .Include(ww => ww.Curriculum).ThenInclude(c => c.Subject)
            .Include(ww => ww.Curriculum).ThenInclude(c => c.FavoriteBuilding)
            .Include(ww => ww.Curriculum).ThenInclude(c => c.NeededEquipments)
            .Include(ww => ww.Curriculum).ThenInclude(c => c.Stream)
                .ThenInclude(s => s.StreamGroups).ThenInclude(sg => sg.Group)
                    .ThenInclude(g => g.Course).ThenInclude(co => co.Degree)
            .ToListAsync(ct);

        return weekWorkloads.Select(ww => new WorkloadItem(ww.Hours, ww.Curriculum)).ToList();
    }

    private async Task<List<Classroom>> LoadClassroomsAsync(CancellationToken ct) =>
        await _context.Classrooms
            .AsNoTracking()
            .Include(c => c.Building)
            .Include(c => c.ClassroomAvailabilities)
            .Include(c => c.EquipmentRooms)
            .ToListAsync(ct);

    private async Task<List<TimeSlot>> LoadWeekTimeSlotsAsync(Guid weekId, CancellationToken ct) =>
        await _context.TimeSlots
            .AsNoTracking()
            .Include(t => t.WeekDay)
            .Where(t => t.WeekDay.Week.Id == weekId)
            .ToListAsync(ct);

    // ----- Занятые ресурсы других институтов в этой неделе -----------------------------------

    private async Task<(List<OccupiedSlot> Classrooms, List<OccupiedTeacherSlot> Teachers)>
        LoadOccupiedForWeekAsync(Guid weekId, Guid instituteId, CancellationToken ct)
    {
        var weekLessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.TimeSlot.WeekDay.Week.Id == weekId)
            .Include(l => l.Stream).ThenInclude(s => s.Curriculums)
            .Include(l => l.Stream)
                .ThenInclude(s => s.StreamGroups).ThenInclude(sg => sg.Group)
                    .ThenInclude(g => g.Course).ThenInclude(co => co.Degree)
            .ToListAsync(ct);

        var other = weekLessons
            .Where(l => !LessonBelongsToInstitute(l, instituteId))
            .ToList();

        var classrooms = other
            .Select(l => new OccupiedSlot(l.ClassroomId, l.TimeSlotId))
            .Distinct()
            .ToList();

        var teachers = other
            .SelectMany(l => l.Stream.Curriculums
                .Select(c => new OccupiedTeacherSlot(c.TeacherId, l.TimeSlotId)))
            .Distinct()
            .ToList();

        return (classrooms, teachers);
    }

    // ----- Якорь к расписанию прошлого семестра ----------------------------------------------

    /// <summary>
    /// Мягкий якорь к прошлому семестру: для нагрузок недели, у которых в прошлом семестре было
    /// аналогичное занятие, переносит предпочтительный корпус и слоты (по паттерну день/номер пары)
    /// на слоты <em>этой</em> недели. Это «сид» стабильности для первой недели каждого типа; дальше
    /// внутри семестра якорь к предыдущей неделе того же типа добавляет оркестратор.
    /// </summary>
    private async Task<List<WorkloadAnchor>> BuildPreviousSemesterAnchorsAsync(
        Guid semesterId, Guid? instituteId,
        IReadOnlyList<WorkloadItem> workloads, IReadOnlyList<TimeSlot> weekTimeSlots,
        CancellationToken ct)
    {
        var current = await _context.Semesters.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == semesterId, ct);
        if (current is null) return new List<WorkloadAnchor>();

        var previous = await _context.Semesters.AsNoTracking()
            .Where(s => s.StartDate < current.StartDate)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(ct);
        if (previous is null) return new List<WorkloadAnchor>();

        var prevLessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.TimeSlot.WeekDay.Week.SemesterId == previous.Id)
            .Include(l => l.Classroom)
            .Include(l => l.TimeSlot).ThenInclude(t => t.WeekDay)
            .Include(l => l.Stream).ThenInclude(s => s.Curriculums)
            .Include(l => l.Stream)
                .ThenInclude(s => s.StreamGroups).ThenInclude(sg => sg.Group)
                    .ThenInclude(g => g.Course).ThenInclude(co => co.Degree)
            .ToListAsync(ct);

        // Ключ предмета (преподаватель, дисциплина, тип) → прошлые корпуса и (день, номер пары).
        var history = new Dictionary<CurriculumKey, HistoryEntry>();
        foreach (var lesson in prevLessons.Where(l => instituteId == null || LessonBelongsToInstitute(l, instituteId.Value)))
        {
            foreach (var c in lesson.Stream.Curriculums)
            {
                var key = CurriculumKey.Of(c);
                if (!history.TryGetValue(key, out var entry))
                    history[key] = entry = new HistoryEntry();

                entry.Buildings.Add(lesson.Classroom.BuildingId);
                entry.SlotKeys.Add((lesson.TimeSlot.WeekDay.DayOfWeek, lesson.TimeSlot.Number));
            }
        }

        if (history.Count == 0) return new List<WorkloadAnchor>();

        // (день недели, номер пары) → идентификаторы слотов ЭТОЙ недели.
        var slotsByPattern = weekTimeSlots
            .GroupBy(t => (t.WeekDay.DayOfWeek, t.Number))
            .ToDictionary(g => g.Key, g => g.Select(t => t.Id).ToList());

        var anchors = new List<WorkloadAnchor>();
        for (int w = 0; w < workloads.Count; w++)
        {
            if (workloads[w].Curriculum is not { } curriculum) continue;
            if (!history.TryGetValue(CurriculumKey.Of(curriculum), out var entry)) continue;

            var preferredSlots = entry.SlotKeys
                .SelectMany(p => slotsByPattern.TryGetValue(p, out var ids) ? ids : Enumerable.Empty<Guid>())
                .Distinct()
                .ToList();

            Guid? preferredBuilding = entry.Buildings.Count > 0 ? entry.Buildings.First() : null;
            if (preferredBuilding is null && preferredSlots.Count == 0) continue;

            anchors.Add(new WorkloadAnchor(w, preferredBuilding, preferredSlots));
        }

        return anchors;
    }

    // ----- Принадлежность институту ----------------------------------------------------------

    private static bool LessonBelongsToInstitute(Lesson lesson, Guid instituteId) =>
        lesson.Stream.StreamGroups
            .Any(sg => sg.Group.Course.Degree.InstituteId == instituteId);

    private readonly record struct CurriculumKey(Guid TeacherId, Guid SubjectId, LessonType LessonType)
    {
        public static CurriculumKey Of(Curriculum c) => new(c.TeacherId, c.SubjectId, c.LessonType);
    }

    private sealed class HistoryEntry
    {
        public HashSet<Guid> Buildings { get; } = new();
        public HashSet<(WeekDayType DayOfWeek, int Number)> SlotKeys { get; } = new();
    }
}
