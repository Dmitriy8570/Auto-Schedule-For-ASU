using Domain.university.groups;

namespace Application.Common.DTO.Lookups;

/// <summary>Институт (факультет) — пункт верхнего уровня выбора.</summary>
public sealed record InstituteDto(Guid Id, string Name);

/// <summary>Ступень образования института (бакалавриат, магистратура, …).</summary>
public sealed record DegreeDto(Guid Id, TypeDegree TypeDegree, Guid InstituteId);

/// <summary>Курс (год обучения) внутри ступени.</summary>
public sealed record CourseDto(Guid Id, int Number, Guid DegreeId);

/// <summary>Учебная группа — конечный выбор для расписания группы.</summary>
public sealed record GroupDto(Guid Id, string Name, Shift Shift, int StudentCount, Guid CourseId, int CourseNumber);

/// <summary>Кафедра института.</summary>
public sealed record DepartmentDto(Guid Id, string Name, Guid InstituteId);

/// <summary>Преподаватель — конечный выбор для расписания преподавателя.</summary>
public sealed record TeacherDto(Guid Id, string Name, Guid DepartmentId, string DepartmentName);
