using Application.Common.DTO.Lookups;

namespace Application.Common.Interfaces;

/// <summary>
/// Чтение организационной структуры университета для выбора, чьё расписание смотреть.
/// Все фильтры опциональны и сужают выборку по доменной иерархии
/// (Институт → Ступень → Курс → Группа; Институт → Кафедра → Преподаватель).
/// </summary>
public interface IUniversityRepository
{
    Task<IReadOnlyList<InstituteDto>> GetInstitutesAsync(string? search, CancellationToken ct);

    Task<IReadOnlyList<DegreeDto>> GetDegreesAsync(Guid? instituteId, CancellationToken ct);

    Task<IReadOnlyList<CourseDto>> GetCoursesAsync(Guid? degreeId, Guid? instituteId, CancellationToken ct);

    Task<IReadOnlyList<GroupDto>> GetGroupsAsync(
        Guid? courseId, Guid? degreeId, Guid? instituteId, string? search, CancellationToken ct);

    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(Guid? instituteId, string? search, CancellationToken ct);

    Task<IReadOnlyList<TeacherDto>> GetTeachersAsync(
        Guid? instituteId, Guid? departmentId, string? search, CancellationToken ct);
}
