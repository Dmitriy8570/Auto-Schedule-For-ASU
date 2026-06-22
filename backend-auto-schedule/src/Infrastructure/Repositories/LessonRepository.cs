using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.schedule;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class LessonRepository(ApplicationDbContext context) : ILessonRepository
{
    public async Task<Lesson?> GetLessonByIdAsync(Guid id, CancellationToken cancellationToken)
          => await WithDisplayIncludes(context.Lessons.AsNoTracking())
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<Lesson?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken)
          => await context.Lessons.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task AddAsync(Lesson lesson, CancellationToken cancellationToken)
          => await context.Lessons.AddAsync(lesson, cancellationToken);

    public async Task<IReadOnlyList<ScheduleConflict>> FindConflictsAsync(
        Guid classroomId, Guid timeSlotId, Guid streamId, Guid? curriculumId,
        Guid? excludeLessonId, CancellationToken ct)
    {
        // Все занятия в этом же слоте — с навигациями для сравнения преподавателей и групп.
        // При редактировании исключаем само занятие, иначе оно конфликтовало бы с собой.
        var inSlot = await context.Lessons
            .AsNoTracking()
            .Where(l => l.TimeSlotId == timeSlotId)
            .Where(l => excludeLessonId == null || l.Id != excludeLessonId.Value)
            .Include(l => l.Curriculum)
            .Include(l => l.Stream).ThenInclude(s => s.Curriculums)
            .Include(l => l.Stream).ThenInclude(s => s.StreamGroups)
            .ToListAsync(ct);

        if (inSlot.Count == 0) return Array.Empty<ScheduleConflict>();

        var conflicts = new List<ScheduleConflict>();

        // 1. Аудитория занята в этом слоте.
        if (inSlot.Any(l => l.ClassroomId == classroomId))
            conflicts.Add(new ScheduleConflict(ScheduleConflictKind.Classroom, "Аудитория уже занята в этом слоте."));

        // 2. Преподаватель нового занятия (из плана) уже ведёт занятие в этом слоте.
        if (curriculumId is { } cid)
        {
            var newTeacher = await context.Curriculums.AsNoTracking()
                .Where(c => c.Id == cid)
                .Select(c => (Guid?)c.TeacherId)
                .FirstOrDefaultAsync(ct);

            if (newTeacher is { } teacherId && inSlot.Any(l => TeachersOf(l).Contains(teacherId)))
                conflicts.Add(new ScheduleConflict(ScheduleConflictKind.Teacher, "Преподаватель уже занят в этом слоте."));
        }

        // 3. Хотя бы одна группа потока уже занята в этом слоте.
        var newGroups = await context.StreamGroups.AsNoTracking()
            .Where(sg => sg.StreamId == streamId)
            .Select(sg => sg.GroupId)
            .ToListAsync(ct);

        if (newGroups.Count > 0)
        {
            var newGroupSet = newGroups.ToHashSet();
            if (inSlot.Any(l => l.Stream.StreamGroups.Any(sg => newGroupSet.Contains(sg.GroupId))))
                conflicts.Add(new ScheduleConflict(ScheduleConflictKind.Group, "Учебная группа уже занята в этом слоте."));
        }

        return conflicts;
    }

    /// <summary>Преподаватели занятия: из его плана, либо из всех планов потока, если план у занятия не задан.</summary>
    private static IEnumerable<Guid> TeachersOf(Lesson lesson) =>
        lesson.Curriculum is not null
            ? new[] { lesson.Curriculum.TeacherId }
            : lesson.Stream.Curriculums.Select(c => c.TeacherId);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
          => await context.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Подгружает навигации, нужные для отображения занятия в сетке расписания
    /// (день/пара/неделя, дисциплина/преподаватель/тип, аудитория/корпус, группы потока).
    /// </summary>
    private static IQueryable<Lesson> WithDisplayIncludes(IQueryable<Lesson> query) =>
        query
            .Include(l => l.TimeSlot).ThenInclude(t => t.WeekDay).ThenInclude(wd => wd.Week)
            .Include(l => l.Classroom).ThenInclude(c => c.Building)
            .Include(l => l.Curriculum!).ThenInclude(c => c.Subject)
            .Include(l => l.Curriculum!).ThenInclude(c => c.Teacher)
            .Include(l => l.Stream).ThenInclude(s => s.StreamGroups).ThenInclude(sg => sg.Group);

    public async Task<IReadOnlyList<Lesson>> GetLessonByTeacherAsync(Guid teacherId, Guid? weekId, CancellationToken cancellationToken) =>
        await WithDisplayIncludes(context.Lessons.AsNoTracking())
            // Если у занятия задан учебный план — по преподавателю плана; иначе — по любому плану потока.
            .Where(l => l.Curriculum != null
                ? l.Curriculum.TeacherId == teacherId
                : l.Stream.Curriculums.Any(c => c.TeacherId == teacherId))
            .Where(InWeek(weekId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetLessonByGroupAsync(Guid groupId, Guid? weekId, CancellationToken cancellationToken) =>
        await WithDisplayIncludes(context.Lessons.AsNoTracking())
            .Where(l => l.Stream.StreamGroups.Any(sg => sg.GroupId == groupId))
            .Where(InWeek(weekId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetLessonByRoomAsync(Guid classroomId, Guid? weekId, CancellationToken cancellationToken) =>
        await WithDisplayIncludes(context.Lessons.AsNoTracking())
            .Where(l => l.ClassroomId == classroomId)
            .Where(InWeek(weekId))
            .ToListAsync(cancellationToken);

    /// <summary>Фильтр по неделе через TimeSlot → WeekDay → Week; если неделя не задана — пропускает всё.</summary>
    private static System.Linq.Expressions.Expression<Func<Lesson, bool>> InWeek(Guid? weekId) =>
        weekId is null
            ? _ => true
            : l => l.TimeSlot.WeekDay.WeekId == weekId.Value;

    public async Task<IReadOnlyList<Lesson>> GetByInstituteAsync(Guid instituteId, CancellationToken cancellationToken) =>
        await context.Lessons
            .Where(l => l.Stream.StreamGroups
                .Any(sg => sg.Group.Course.Degree.InstituteId == instituteId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetByInstituteAndSemesterAsync(
        Guid instituteId, Guid semesterId, CancellationToken cancellationToken) =>
        await context.Lessons
            .Where(l => l.SemesterId == semesterId
                && l.Stream.StreamGroups.Any(sg => sg.Group.Course.Degree.InstituteId == instituteId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetBySemesterAsync(Guid semesterId, CancellationToken cancellationToken) =>
        await context.Lessons
            .Where(l => l.SemesterId == semesterId)
            .ToListAsync(cancellationToken);

    public void RemoveRange(IEnumerable<Lesson> lessons) => context.Lessons.RemoveRange(lessons);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await context.Lessons.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lesson is null) return false;

        context.Lessons.Remove(lesson);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}