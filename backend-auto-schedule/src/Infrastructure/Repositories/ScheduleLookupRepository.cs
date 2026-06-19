using Application.Common.DTO.Schedule;
using Application.Common.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>EF Core-реализация справочников редактора расписания. Все запросы read-only.</summary>
public sealed class ScheduleLookupRepository(ApplicationDbContext context) : IScheduleLookupRepository
{
    private const int MaxResults = 200;

    public async Task<IReadOnlyList<TimeSlotDto>> GetTimeSlotsAsync(Guid weekId, CancellationToken ct) =>
        await context.TimeSlots
            .AsNoTracking()
            .Where(t => t.WeekDay.WeekId == weekId)
            .OrderBy(t => t.WeekDay.DayOfWeek).ThenBy(t => t.Number)
            .Select(t => new TimeSlotDto(t.Id, (int)t.WeekDay.DayOfWeek, t.Number))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CurriculumOptionDto>> GetCurriculumsAsync(
        Guid? groupId, Guid? teacherId, Guid? semesterId, CancellationToken ct)
    {
        var query = context.Curriculums.AsNoTracking();

        if (groupId is { } gid)
            query = query.Where(c => c.Stream.StreamGroups.Any(sg => sg.GroupId == gid));
        if (teacherId is { } tid)
            query = query.Where(c => c.TeacherId == tid);
        if (semesterId is { } sid)
            query = query.Where(c => c.SemesterWorkloads.Any(sw => sw.SemesterId == sid));

        // Имена групп склеиваем в памяти: string.Join не транслируется в SQL.
        var rows = await query
            .OrderBy(c => c.Subject.Name)
            .Take(MaxResults)
            .Select(c => new
            {
                c.Id,
                SubjectName = c.Subject.Name,
                c.TeacherId,
                TeacherName = c.Teacher.Name,
                c.LessonType,
                c.StreamId,
                GroupNames = c.Stream.StreamGroups.Select(sg => sg.Group.Name).ToList()
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new CurriculumOptionDto(
                r.Id, r.SubjectName, r.TeacherId, r.TeacherName, r.LessonType, r.StreamId,
                string.Join(", ", r.GroupNames)))
            .ToList();
    }
}
