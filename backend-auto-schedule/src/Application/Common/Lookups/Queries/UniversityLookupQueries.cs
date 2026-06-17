using Application.Common.DTO.Lookups;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Lookups.Queries;

// ----- Институты -----
public sealed record GetInstitutesQuery(string? Search) : IRequest<IReadOnlyList<InstituteDto>>;

public sealed class GetInstitutesQueryHandler(IUniversityRepository repo)
    : IRequestHandler<GetInstitutesQuery, IReadOnlyList<InstituteDto>>
{
    public Task<IReadOnlyList<InstituteDto>> Handle(GetInstitutesQuery request, CancellationToken ct)
        => repo.GetInstitutesAsync(request.Search, ct);
}

// ----- Ступени образования -----
public sealed record GetDegreesQuery(Guid? InstituteId) : IRequest<IReadOnlyList<DegreeDto>>;

public sealed class GetDegreesQueryHandler(IUniversityRepository repo)
    : IRequestHandler<GetDegreesQuery, IReadOnlyList<DegreeDto>>
{
    public Task<IReadOnlyList<DegreeDto>> Handle(GetDegreesQuery request, CancellationToken ct)
        => repo.GetDegreesAsync(request.InstituteId, ct);
}

// ----- Курсы -----
public sealed record GetCoursesQuery(Guid? DegreeId, Guid? InstituteId) : IRequest<IReadOnlyList<CourseDto>>;

public sealed class GetCoursesQueryHandler(IUniversityRepository repo)
    : IRequestHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>
{
    public Task<IReadOnlyList<CourseDto>> Handle(GetCoursesQuery request, CancellationToken ct)
        => repo.GetCoursesAsync(request.DegreeId, request.InstituteId, ct);
}

// ----- Группы -----
public sealed record GetGroupsQuery(Guid? CourseId, Guid? DegreeId, Guid? InstituteId, string? Search)
    : IRequest<IReadOnlyList<GroupDto>>;

public sealed class GetGroupsQueryHandler(IUniversityRepository repo)
    : IRequestHandler<GetGroupsQuery, IReadOnlyList<GroupDto>>
{
    public Task<IReadOnlyList<GroupDto>> Handle(GetGroupsQuery request, CancellationToken ct)
        => repo.GetGroupsAsync(request.CourseId, request.DegreeId, request.InstituteId, request.Search, ct);
}

// ----- Кафедры -----
public sealed record GetDepartmentsQuery(Guid? InstituteId, string? Search) : IRequest<IReadOnlyList<DepartmentDto>>;

public sealed class GetDepartmentsQueryHandler(IUniversityRepository repo)
    : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    public Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken ct)
        => repo.GetDepartmentsAsync(request.InstituteId, request.Search, ct);
}

// ----- Преподаватели -----
public sealed record GetTeachersQuery(Guid? InstituteId, Guid? DepartmentId, string? Search)
    : IRequest<IReadOnlyList<TeacherDto>>;

public sealed class GetTeachersQueryHandler(IUniversityRepository repo)
    : IRequestHandler<GetTeachersQuery, IReadOnlyList<TeacherDto>>
{
    public Task<IReadOnlyList<TeacherDto>> Handle(GetTeachersQuery request, CancellationToken ct)
        => repo.GetTeachersAsync(request.InstituteId, request.DepartmentId, request.Search, ct);
}
