using Domain.workload;

namespace Application.Common.DTO.Schedule;

/// <summary>Временной слот недели: позволяет сопоставить ячейку сетки (день, пара) с TimeSlotId.</summary>
public sealed record TimeSlotDto(Guid Id, int DayOfWeek, int Number);

/// <summary>
/// Вариант учебного плана для формы добавления пары: дисциплина + преподаватель + тип + поток.
/// </summary>
public sealed record CurriculumOptionDto(
    Guid Id,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    LessonType LessonType,
    Guid StreamId,
    string GroupNames);
