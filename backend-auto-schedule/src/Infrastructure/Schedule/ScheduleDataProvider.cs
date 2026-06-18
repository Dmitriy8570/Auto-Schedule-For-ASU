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
/// EF Core-реализация поставщика данных солвера.
/// Помимо генерации на весь семестр (<see cref="GetAsync"/>) поддерживает декомпозицию
/// по институту (<see cref="GetForInstituteAsync"/>): ограничивает нагрузки институтом,
/// собирает занятые ресурсы других институтов этого семестра (жёсткие блокировки, «B»)
/// и якорь к расписанию прошлого семестра (мягкие предпочтения, «C»).
///
/// Допущения, требующие проверки на реальных данных:
/// <list type="bullet">
/// <item>принадлежность нагрузки/занятия институту определяется по группам потока
/// (Stream → StreamGroups → Group → Course → Degree → Institute);</item>
/// <item>прошлый семестр — это семестр с наибольшей датой начала, меньшей текущей;</item>
/// <item>занятие из прошлого семестра сопоставляется текущей нагрузке по ключу
/// (преподаватель, дисциплина, тип занятия); слоты переносятся по паре
/// (день недели, номер пары), т.к. идентификаторы <see cref="TimeSlot"/> уникальны для семестра.</item>
/// </list>
/// </summary>
public sealed class ScheduleDataProvider : IScheduleDataProvider
{
    private readonly ApplicationDbContext _context;

    public ScheduleDataProvider(ApplicationDbContext context) => _context = context;

    public async Task<ScheduleData> GetAsync(Guid semesterId, CancellationToken cancellationToken)
    {
        var workloads = await LoadWorkloadsAsync(semesterId, cancellationToken);
        var classrooms = await LoadClassroomsAsync(cancellationToken);
        var timeSlots = await LoadTimeSlotsAsync(semesterId, cancellationToken);
        var penalties = await _context.ConstraintConfigs.AsNoTracking().ToListAsync(cancellationToken);

        return new ScheduleData(workloads, classrooms, timeSlots, penalties);
    }

    public async Task<ScheduleData> GetForInstituteAsync(
        Guid semesterId, Guid instituteId, CancellationToken cancellationToken)
    {
        var allWorkloads = await LoadWorkloadsAsync(semesterId, cancellationToken);
        var workloads = allWorkloads.Where(w => BelongsToInstitute(w.Curriculum, instituteId)).ToList();

        var classrooms = await LoadClassroomsAsync(cancellationToken);
        var timeSlots = await LoadTimeSlotsAsync(semesterId, cancellationToken);
        var penalties = await _context.ConstraintConfigs.AsNoTracking().ToListAsync(cancellationToken);

        var (occupiedClassrooms, occupiedTeachers) =
            await LoadOccupiedResourcesAsync(semesterId, instituteId, cancellationToken);

        var anchors = await BuildAnchorsAsync(semesterId, instituteId, workloads, timeSlots, cancellationToken);

        return new ScheduleData(
            workloads, classrooms, timeSlots, penalties,
            occupiedClassrooms, occupiedTeachers, anchors);
    }

    // ----- Загрузка осей модели --------------------------------------------------------------

    private async Task<List<SemesterWorkload>> LoadWorkloadsAsync(Guid semesterId, CancellationToken ct) =>
        await _context.SemesterWorkloads
            .AsNoTracking()
            .Where(sw => sw.SemesterId == semesterId)
            .Include(sw => sw.Curriculum).ThenInclude(c => c.Teacher)
            .Include(sw => sw.Curriculum).ThenInclude(c => c.Subject)
            .Include(sw => sw.Curriculum).ThenInclude(c => c.FavoriteBuilding)
            .Include(sw => sw.Curriculum).ThenInclude(c => c.NeededEquipments)
            .Include(sw => sw.Curriculum).ThenInclude(c => c.Stream)
                .ThenInclude(s => s.StreamGroups).ThenInclude(sg => sg.Group)
                    .ThenInclude(g => g.Course).ThenInclude(co => co.Degree)
            .ToListAsync(ct);

    private async Task<List<Classroom>> LoadClassroomsAsync(CancellationToken ct) =>
        await _context.Classrooms
            .AsNoTracking()
            .Include(c => c.Building)
            .Include(c => c.ClassroomAvailabilities)
            .Include(c => c.EquipmentRooms)
            .ToListAsync(ct);

    private async Task<List<TimeSlot>> LoadTimeSlotsAsync(Guid semesterId, CancellationToken ct) =>
        await _context.TimeSlots
            .AsNoTracking()
            .Include(t => t.WeekDay)
            .Where(t => t.WeekDay.Week.SemesterId == semesterId)
            .ToListAsync(ct);

    // ----- B: занятые ресурсы других институтов этого семестра -------------------------------

    private async Task<(List<OccupiedSlot> Classrooms, List<OccupiedTeacherSlot> Teachers)>
        LoadOccupiedResourcesAsync(Guid semesterId, Guid instituteId, CancellationToken ct)
    {
        var semesterLessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.TimeSlot.WeekDay.Week.SemesterId == semesterId)
            .Include(l => l.Stream).ThenInclude(s => s.Curriculums)
            .Include(l => l.Stream)
                .ThenInclude(s => s.StreamGroups).ThenInclude(sg => sg.Group)
                    .ThenInclude(g => g.Course).ThenInclude(co => co.Degree)
            .ToListAsync(ct);

        var otherInstitute = semesterLessons
            .Where(l => !LessonBelongsToInstitute(l, instituteId))
            .ToList();

        var classrooms = otherInstitute
            .Select(l => new OccupiedSlot(l.ClassroomId, l.TimeSlotId))
            .Distinct()
            .ToList();

        var teachers = otherInstitute
            .SelectMany(l => l.Stream.Curriculums
                .Select(c => new OccupiedTeacherSlot(c.TeacherId, l.TimeSlotId)))
            .Distinct()
            .ToList();

        return (classrooms, teachers);
    }

    // ----- C: якорь к расписанию прошлого семестра -------------------------------------------

    private async Task<List<WorkloadAnchor>> BuildAnchorsAsync(
        Guid semesterId, Guid instituteId,
        IReadOnlyList<SemesterWorkload> workloads, IReadOnlyList<TimeSlot> currentTimeSlots,
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
        foreach (var lesson in prevLessons.Where(l => LessonBelongsToInstitute(l, instituteId)))
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

        // (день недели, номер пары) → идентификаторы слотов текущего семестра.
        var slotsByPattern = currentTimeSlots
            .GroupBy(t => (t.WeekDay.DayOfWeek, t.Number))
            .ToDictionary(g => g.Key, g => g.Select(t => t.Id).ToList());

        var anchors = new List<WorkloadAnchor>();
        for (int w = 0; w < workloads.Count; w++)
        {
            if (!history.TryGetValue(CurriculumKey.Of(workloads[w].Curriculum), out var entry))
                continue;

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

    private static bool BelongsToInstitute(Curriculum curriculum, Guid instituteId) =>
        curriculum.Stream.StreamGroups
            .Any(sg => sg.Group.Course.Degree.InstituteId == instituteId);

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
