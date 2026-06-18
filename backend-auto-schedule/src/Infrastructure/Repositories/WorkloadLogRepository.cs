using Application.Common.DTO.Workload;
using Application.Common.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// EF Core-доступ к журналам изменений нагрузки. Объединяет семестровые (<c>SemesterLog</c>)
/// и понедельные (<c>WeekLog</c>) записи, проецирует их в единый <see cref="WorkloadChangeDto"/>
/// и сортирует по времени по убыванию. Фильтры применяются на стороне БД.
/// </summary>
public sealed class WorkloadLogRepository : IWorkloadLogRepository
{
    private readonly ApplicationDbContext _context;

    public WorkloadLogRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<WorkloadChangeDto>> GetChangesAsync(
        WorkloadChangeFilter filter, CancellationToken cancellationToken)
    {
        var semesterChanges = await SemesterQuery(filter).ToListAsync(cancellationToken);
        var weekChanges = await WeekQuery(filter).ToListAsync(cancellationToken);

        return semesterChanges
            .Concat(weekChanges)
            .OrderByDescending(c => c.TimeStamp)
            .ToList();
    }

    private IQueryable<WorkloadChangeDto> SemesterQuery(WorkloadChangeFilter f)
    {
        var query = _context.SemesterLogs.AsNoTracking().AsQueryable();

        if (f.TeacherId is { } teacher)
            query = query.Where(l => l.SemesterWorkload.Curriculum.TeacherId == teacher);
        if (f.SubjectId is { } subject)
            query = query.Where(l => l.SemesterWorkload.Curriculum.SubjectId == subject);
        if (f.GroupId is { } group)
            query = query.Where(l => l.SemesterWorkload.Curriculum.Stream.StreamGroups.Any(sg => sg.GroupId == group));
        if (f.SemesterId is { } semester)
            query = query.Where(l => l.SemesterWorkload.SemesterId == semester);
        if (f.From is { } from)
            query = query.Where(l => l.TimeStamp >= from);
        if (f.To is { } to)
            query = query.Where(l => l.TimeStamp <= to);

        return query.Select(l => new WorkloadChangeDto
        {
            Action = l.Action,
            OldValue = l.OldValue,
            NewValue = l.NewValue,
            TimeStamp = l.TimeStamp,
            Scope = "Semester",
            CurriculumId = l.SemesterWorkload.CurriculumId,
            TeacherId = l.SemesterWorkload.Curriculum.TeacherId,
            SubjectId = l.SemesterWorkload.Curriculum.SubjectId,
            StreamId = l.SemesterWorkload.Curriculum.StreamId
        });
    }

    private IQueryable<WorkloadChangeDto> WeekQuery(WorkloadChangeFilter f)
    {
        var query = _context.WeekLogs.AsNoTracking().AsQueryable();

        if (f.TeacherId is { } teacher)
            query = query.Where(l => l.WeekWorkload.Curriculum.TeacherId == teacher);
        if (f.SubjectId is { } subject)
            query = query.Where(l => l.WeekWorkload.Curriculum.SubjectId == subject);
        if (f.GroupId is { } group)
            query = query.Where(l => l.WeekWorkload.Curriculum.Stream.StreamGroups.Any(sg => sg.GroupId == group));
        if (f.SemesterId is { } semester)
            query = query.Where(l => l.WeekWorkload.Week.SemesterId == semester);
        if (f.From is { } from)
            query = query.Where(l => l.TimeStamp >= from);
        if (f.To is { } to)
            query = query.Where(l => l.TimeStamp <= to);

        return query.Select(l => new WorkloadChangeDto
        {
            Action = l.Action,
            OldValue = l.OldValue,
            NewValue = l.NewValue,
            TimeStamp = l.TimeStamp,
            Scope = "Week",
            CurriculumId = l.WeekWorkload.CurriculumId,
            TeacherId = l.WeekWorkload.Curriculum.TeacherId,
            SubjectId = l.WeekWorkload.Curriculum.SubjectId,
            StreamId = l.WeekWorkload.Curriculum.StreamId
        });
    }
}
