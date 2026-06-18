using Domain.workload;

namespace Application.Common.DTO.Workloads;

/// <summary>Строка учебной нагрузки: что и кому ведёт преподаватель, с часами за семестр и по неделям.</summary>
public sealed record WorkloadItemDto(
    Guid CurriculumId,
    string Teacher,
    string Subject,
    string Group,
    LessonType LessonType,
    int SemesterHours,
    IReadOnlyList<WeekHoursDto> WeeklyHours);

/// <summary>Часы на конкретной неделе семестра (week — порядковый номер недели с 1).</summary>
public sealed record WeekHoursDto(int Week, int Hours);
