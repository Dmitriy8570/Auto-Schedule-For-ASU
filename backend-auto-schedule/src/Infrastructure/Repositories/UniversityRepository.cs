using Application.Common.DTO.Lookups;
using Application.Common.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Чтение организационной структуры университета. Все запросы — read-only (<see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/>)
/// с серверной проекцией в DTO, поиск по имени — регистронезависимый ILike (PostgreSQL).
/// </summary>
public sealed class UniversityRepository(ApplicationDbContext context) : IUniversityRepository
{
    /// <summary>Потолок размера выборки для «листовых» справочников (группы, преподаватели), чтобы не грузить БД на каждый вызов.</summary>
    private const int MaxResults = 20;

    public async Task<IReadOnlyList<InstituteDto>> GetInstitutesAsync(string? search, CancellationToken ct)
    {
        var query = context.Institutes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => EF.Functions.ILike(i.Name, $"%{search}%"));

        return await query
            .OrderBy(i => i.Name)
            .Select(i => new InstituteDto(i.Id, i.Name))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DegreeDto>> GetDegreesAsync(Guid? instituteId, CancellationToken ct)
    {
        var query = context.Degrees.AsNoTracking();

        if (instituteId is { } id)
            query = query.Where(d => d.InstituteId == id);

        return await query
            .OrderBy(d => d.TypeDegree)
            .Select(d => new DegreeDto(d.Id, d.TypeDegree, d.InstituteId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CourseDto>> GetCoursesAsync(Guid? degreeId, Guid? instituteId, CancellationToken ct)
    {
        var query = context.Courses.AsNoTracking();

        if (degreeId is { } did)
            query = query.Where(c => c.DegreeId == did);
        if (instituteId is { } iid)
            query = query.Where(c => c.Degree.InstituteId == iid);

        return await query
            .OrderBy(c => c.Number)
            .Select(c => new CourseDto(c.Id, c.Number, c.DegreeId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GroupDto>> GetGroupsAsync(
        Guid? courseId, Guid? degreeId, Guid? instituteId, string? search, CancellationToken ct)
    {
        var query = context.Groups.AsNoTracking();

        if (courseId is { } cid)
            query = query.Where(g => g.CourseId == cid);
        if (degreeId is { } did)
            query = query.Where(g => g.Course.DegreeId == did);
        if (instituteId is { } iid)
            query = query.Where(g => g.Course.Degree.InstituteId == iid);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(g => EF.Functions.ILike(g.Name, $"%{search}%"));

        return await query
            .OrderBy(g => g.Name)
            .Take(MaxResults)
            .Select(g => new GroupDto(g.Id, g.Name, g.Shift, g.StudentCount, g.CourseId, g.Course.Number))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(Guid? instituteId, string? search, CancellationToken ct)
    {
        var query = context.Departments.AsNoTracking();

        if (instituteId is { } id)
            query = query.Where(d => d.InstituteId == id);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => EF.Functions.ILike(d.Name, $"%{search}%"));

        return await query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.InstituteId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TeacherDto>> GetTeachersAsync(
        Guid? instituteId, Guid? departmentId, string? search, CancellationToken ct)
    {
        var query = context.Teachers.AsNoTracking();

        if (departmentId is { } did)
            query = query.Where(t => t.DepartmentId == did);
        if (instituteId is { } iid)
            query = query.Where(t => t.Department.InstituteId == iid);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => EF.Functions.ILike(t.Name, $"%{search}%"));

        return await query
            .OrderBy(t => t.Name)
            .Take(MaxResults)
            .Select(t => new TeacherDto(t.Id, t.Name, t.DepartmentId, t.Department.Name))
            .ToListAsync(ct);
    }
}
