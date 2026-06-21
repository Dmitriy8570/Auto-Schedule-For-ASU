namespace Application.Common.Exceptions;

/// <summary>Вид коллизии расписания при ручном добавлении занятия.</summary>
public enum ScheduleConflictKind
{
    /// <summary>Аудитория уже занята в этом слоте.</summary>
    Classroom,
    /// <summary>Преподаватель уже ведёт занятие в этом слоте.</summary>
    Teacher,
    /// <summary>Учебная группа уже занята в этом слоте.</summary>
    Group
}

/// <summary>Описание одной обнаруженной коллизии.</summary>
public sealed record ScheduleConflict(ScheduleConflictKind Kind, string Detail);

/// <summary>
/// Ручное добавление занятия нарушает жёсткие ограничения (пересечение по аудитории,
/// преподавателю или группе в одном слоте). Обрабатывается как 409 Conflict.
/// </summary>
public sealed class ScheduleConflictException(IReadOnlyList<ScheduleConflict> conflicts)
    : Exception("Добавление занятия приводит к коллизии расписания.")
{
    public IReadOnlyList<ScheduleConflict> Conflicts { get; } = conflicts;
}
