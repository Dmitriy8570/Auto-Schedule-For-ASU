using Domain.common;

namespace Domain.constraints.penalty;

/// <summary>Тип мягкого ограничения (штрафа) при составлении расписания.</summary>
public enum ConstraintType
{
    /// <summary>Штраф за окно в расписании преподавателя.</summary>
    TeacherGap = 0,
    /// <summary>Штраф за окно в расписании студенческой группы.</summary>
    StudentGap = 1,
    /// <summary>Штраф за назначение в недоступную аудиторию.</summary>
    ClassroomAvailability = 2,
    /// <summary>Штраф за назначение в недоступный слот преподавателя.</summary>
    TeacherAvailability = 3,
}

/// <summary>Конфигурация конкретного мягкого ограничения с его штрафным весом.</summary>
public class ConstraintConfig
{
    private ConstraintConfig() { }

    public Guid Id { get; private set; }
    public ConstraintType ConstraintType { get; private set; }

    /// <summary>Вес штрафа: чем выше, тем сильнее солвер стремится избежать нарушения.</summary>
    public int Penalty { get; private set; }

    /// <summary>Создать конфигурацию мягкого ограничения с весом штрафа.</summary>
    public static ConstraintConfig Create(Guid id, ConstraintType constraintType, int penalty) => new()
    {
        Id = Guard.NotEmpty(id, nameof(id)),
        ConstraintType = Guard.Defined(constraintType, nameof(constraintType)),
        Penalty = Guard.NotNegative(penalty, nameof(penalty))
    };

    /// <summary>Изменить вес штрафа.</summary>
    public void SetPenalty(int penalty) => Penalty = Guard.NotNegative(penalty, nameof(penalty));
}
