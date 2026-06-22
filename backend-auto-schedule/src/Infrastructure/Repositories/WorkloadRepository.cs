using Application.Common.DTO;
using Application.Common.DTO.Generation;
using Application.Common.DTO.Workloads;
using Application.Common.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Чтение учебной нагрузки (read-only). Фильтр и сортировка — на стороне БД, страница берётся
/// Skip/Take. Часы за семестр и по неделям берутся из последнего семестра учебного плана.
/// </summary>
public sealed class WorkloadRepository(ApplicationDbContext context) : IWorkloadRepository
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<WorkloadItemDto>> GetWorkloadsAsync(
        Guid? instituteId, Guid? departmentId, Guid? teacherId, string? subjectSearch,
        int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = context.Curriculums.AsNoTracking();

        if (teacherId is { } tid) query = query.Where(c => c.TeacherId == tid);
        if (departmentId is { } did) query = query.Where(c => c.Teacher.DepartmentId == did);
        if (instituteId is { } iid) query = query.Where(c => c.Teacher.Department.InstituteId == iid);
        if (!string.IsNullOrWhiteSpace(subjectSearch))
            query = query.Where(c => EF.Functions.ILike(c.Subject.Name, $"%{subjectSearch}%"));

        var totalItems = await query.CountAsync(ct);

        // Страница с нужными данными. Семестровую нагрузку берём за последний семестр плана.
        var rows = await query
            .OrderBy(c => c.Teacher.Name).ThenBy(c => c.Subject.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new
            {
                c.Id,
                Teacher = c.Teacher.Name,
                Subject = c.Subject.Name,
                Groups = c.Stream.StreamGroups.Select(sg => sg.Group.Name).ToList(),
                c.LessonType,
                SemWl = c.SemesterWorkloads
                    .OrderByDescending(sw => sw.Semester.StartDate)
                    .Select(sw => new { sw.Id, sw.Hours })
                    .FirstOrDefault(),
                Weeks = c.WeekWorkloads
                    .Select(ww => new { ww.SemesterWorkloadId, ww.Hours, ww.Week.StartDate })
                    .ToList()
            })
            .ToListAsync(ct);

        var items = rows.Select(r =>
        {
            // Недели семестра нумеруем с 1 по возрастанию даты начала; берём только нужный семестр.
            var weekly = r.SemWl is null
                ? new List<WeekHoursDto>()
                : r.Weeks
                    .Where(w => w.SemesterWorkloadId == r.SemWl.Id)
                    .OrderBy(w => w.StartDate)
                    .Select((w, i) => new WeekHoursDto(i + 1, w.Hours))
                    .ToList();

            return new WorkloadItemDto(
                r.Id,
                r.Teacher,
                r.Subject,
                string.Join(", ", r.Groups.OrderBy(g => g)),
                r.LessonType,
                r.SemWl?.Hours ?? 0,
                weekly);
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PagedResult<WorkloadItemDto>(items, page, pageSize, totalItems, totalPages);
    }

    public async Task<IReadOnlyList<UnplacedWorkloadRow>> GetUnplacedWorkloadAsync(
        Guid semesterId, Guid? instituteId, Guid? departmentId, Guid? teacherId, CancellationToken ct)
    {
        // Берём планы, у которых есть семестровая нагрузка в этом семестре.
        var query = context.Curriculums.AsNoTracking()
            .Where(c => c.SemesterWorkloads.Any(sw => sw.SemesterId == semesterId));

        if (teacherId is { } tid) query = query.Where(c => c.TeacherId == tid);
        if (departmentId is { } did) query = query.Where(c => c.Teacher.DepartmentId == did);
        if (instituteId is { } iid) query = query.Where(c => c.Teacher.Department.InstituteId == iid);

        // План (часы за семестр) и факт (число занятий по плану в этом семестре) считаем в БД.
        var rows = await query
            .OrderBy(c => c.Teacher.Name).ThenBy(c => c.Subject.Name)
            .Select(c => new
            {
                c.Id,
                Teacher = c.Teacher.Name,
                Subject = c.Subject.Name,
                Groups = c.Stream.StreamGroups.Select(sg => sg.Group.Name).ToList(),
                c.LessonType,
                PlannedHours = c.SemesterWorkloads
                    .Where(sw => sw.SemesterId == semesterId)
                    .Select(sw => (int?)sw.Hours).FirstOrDefault() ?? 0,
                PlacedPairs = context.Lessons.Count(l => l.CurriculumId == c.Id && l.SemesterId == semesterId)
            })
            .ToListAsync(ct);

        // Одна пара = 2 ак. часа. Возвращаем только нагрузки с дефицитом (поставлено меньше плана).
        return rows
            .Select(r =>
            {
                int planned = r.PlannedHours / 2;
                return new UnplacedWorkloadRow(
                    r.Id, r.Teacher, r.Subject,
                    string.Join(", ", r.Groups.OrderBy(g => g)),
                    r.LessonType, planned, r.PlacedPairs, planned - r.PlacedPairs);
            })
            .Where(r => r.UnplacedPairs > 0)
            .ToList();
    }
}
