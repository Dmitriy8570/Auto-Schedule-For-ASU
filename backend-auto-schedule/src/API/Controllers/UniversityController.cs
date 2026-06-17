using Application.Common.DTO.Lookups;
using Application.Common.Lookups.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Справочники организационной структуры для выбора, чьё расписание смотреть:
/// ветка групп (Институт → Ступень → Курс → Группа) и ветка преподавателей
/// (Институт → Кафедра → Преподаватель). Все фильтры опциональны.
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class UniversityController(IMediator mediator) : ControllerBase
{
    /// <summary>Список институтов (факультетов).</summary>
    [HttpGet("api/institutes")]
    public async Task<ActionResult<IReadOnlyList<InstituteDto>>> GetInstitutes(
        [FromQuery] string? search, CancellationToken ct)
        => Ok(await mediator.Send(new GetInstitutesQuery(search), ct));

    /// <summary>Ступени образования института.</summary>
    [HttpGet("api/degrees")]
    public async Task<ActionResult<IReadOnlyList<DegreeDto>>> GetDegrees(
        [FromQuery] Guid? instituteId, CancellationToken ct)
        => Ok(await mediator.Send(new GetDegreesQuery(instituteId), ct));

    /// <summary>Курсы (годы обучения) внутри ступени.</summary>
    [HttpGet("api/courses")]
    public async Task<ActionResult<IReadOnlyList<CourseDto>>> GetCourses(
        [FromQuery] Guid? degreeId, [FromQuery] Guid? instituteId, CancellationToken ct)
        => Ok(await mediator.Send(new GetCoursesQuery(degreeId, instituteId), ct));

    /// <summary>Учебные группы — конечный выбор для расписания группы.</summary>
    [HttpGet("api/groups")]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> GetGroups(
        [FromQuery] Guid? courseId, [FromQuery] Guid? degreeId, [FromQuery] Guid? instituteId,
        [FromQuery] string? search, CancellationToken ct)
        => Ok(await mediator.Send(new GetGroupsQuery(courseId, degreeId, instituteId, search), ct));

    /// <summary>Кафедры института.</summary>
    [HttpGet("api/departments")]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetDepartments(
        [FromQuery] Guid? instituteId, [FromQuery] string? search, CancellationToken ct)
        => Ok(await mediator.Send(new GetDepartmentsQuery(instituteId, search), ct));

    /// <summary>Преподаватели — конечный выбор для расписания преподавателя.</summary>
    [HttpGet("api/teachers")]
    public async Task<ActionResult<IReadOnlyList<TeacherDto>>> GetTeachers(
        [FromQuery] Guid? instituteId, [FromQuery] Guid? departmentId,
        [FromQuery] string? search, CancellationToken ct)
        => Ok(await mediator.Send(new GetTeachersQuery(instituteId, departmentId, search), ct));
}
