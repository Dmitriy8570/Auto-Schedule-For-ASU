using Application.Common.DTO;
using Application.Common.DTO.Workloads;
using Application.Common.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// EF Core-доступ к журналам изменений нагрузки. Объединяет семестровые (<c>SemesterLog</c>)
/// и понедельные (<c>WeekLog</c>) записи, проецирует их в единый <see cref="WorkloadChangeDto"/>
/// и сортирует по времени по убыванию. Фильтры применяются на стороне БД.
/// </summary>
public sealed class WorkloadLogRepository(ApplicationDbContext context) : IWorkloadLogRepository
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<WorkloadChangeDto>> GetChangesAsync(
        WorkloadChangeFilter filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        // Семестровый и понедельный журналы объединяются в один запрос (SQL UNION ALL),
        // сортируются по времени и пагинируются на стороне БД — без выгрузки всего журнала в память.
        var combined = SemesterQuery(filter).Concat(WeekQuery(filter));

        var totalItems = await combined.CountAsync(cancellationToken);

        var items = await combined
            .OrderByDescending(c => c.TimeStamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PagedResult<WorkloadChangeDto>(items, page, pageSize, totalItems, totalPages);
    }

    private IQueryable<WorkloadChangeDto> SemesterQuery(WorkloadChangeFilter f)
    {
        var query = context.SemesterLogs.AsNoTracking().AsQueryable();

        // l.SemesterWorkload — навигация nullable (связь обнуляется при удалении нагрузки), но в
        // выражении-запросе разыменование транслируется в SQL-JOIN, поэтому null-forgiving (!) уместен.
        if (f.TeacherId is { } teacher)
            query = query.Where(l => l.SemesterWorkload!.Curriculum.TeacherId == teacher);
        if (f.SubjectId is { } subject)
            query = query.Where(l => l.SemesterWorkload!.Curriculum.SubjectId == subject);
        if (f.GroupId is { } group)
            query = query.Where(l => l.SemesterWorkload!.Curriculum.Stream.StreamGroups.Any(sg => sg.GroupId == group));
        if (f.SemesterId is { } semester)
            query = query.Where(l => l.SemesterWorkload!.SemesterId == semester);
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
            CurriculumId = l.SemesterWorkload!.CurriculumId,
            TeacherId = l.SemesterWorkload.Curriculum.TeacherId,
            SubjectId = l.SemesterWorkload.Curriculum.SubjectId,
            StreamId = l.SemesterWorkload.Curriculum.StreamId
        });
    }

    private IQueryable<WorkloadChangeDto> WeekQuery(WorkloadChangeFilter f)
    {
        var query = context.WeekLogs.AsNoTracking().AsQueryable();

        // l.WeekWorkload — навигация nullable (связь обнуляется при удалении нагрузки), но в
        // выражении-запросе разыменование транслируется в SQL-JOIN, поэтому null-forgiving (!) уместен.
        if (f.TeacherId is { } teacher)
            query = query.Where(l => l.WeekWorkload!.Curriculum.TeacherId == teacher);
        if (f.SubjectId is { } subject)
            query = query.Where(l => l.WeekWorkload!.Curriculum.SubjectId == subject);
        if (f.GroupId is { } group)
            query = query.Where(l => l.WeekWorkload!.Curriculum.Stream.StreamGroups.Any(sg => sg.GroupId == group));
        if (f.SemesterId is { } semester)
            query = query.Where(l => l.WeekWorkload!.Week.SemesterId == semester);
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
            CurriculumId = l.WeekWorkload!.CurriculumId,
            TeacherId = l.WeekWorkload.Curriculum.TeacherId,
            SubjectId = l.WeekWorkload.Curriculum.SubjectId,
            StreamId = l.WeekWorkload.Curriculum.StreamId
        });
    }
}
