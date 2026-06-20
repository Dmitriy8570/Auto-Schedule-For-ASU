using Domain.calendar;
using Domain.common;
using Domain.university.buildings;
using Domain.university.teachers;

namespace Domain.constraints;

/// <summary>
/// Ограничение доступности аудитории в конкретном слоте (день + пара). Знак <see cref="Penalty"/>
/// задаёт градацию: положительный вес штрафует назначение, отрицательный — поощряет
/// (см. <see cref="AvailabilityStates"/>).
/// </summary>
public class ClassroomAvailability
{
    private ClassroomAvailability() { }

    public Guid Id { get; private set; }

    /// <summary>Знаковый штраф за назначение занятия в данный слот (см. <see cref="AvailabilityStates"/>).</summary>
    public int Penalty { get; private set; }

    public Classroom Classroom { get; private set; } = null!;
    public Guid ClassroomId { get; private set; }

    /// <summary>Номер пары, к которой относится ограничение.</summary>
    public int NumberLesson { get; private set; }

    public WeekDayType DayOfWeek { get; private set; }

    /// <summary>Создать запись доступности аудитории для слота.</summary>
    public static ClassroomAvailability Create(Guid classroomId, WeekDayType dayOfWeek, int numberLesson, int penalty) => new()
    {
        Id = Guid.NewGuid(),
        ClassroomId = Guard.NotEmpty(classroomId, nameof(classroomId)),
        DayOfWeek = Guard.Defined(dayOfWeek, nameof(dayOfWeek)),
        NumberLesson = Guard.Positive(numberLesson, nameof(numberLesson)),
        Penalty = penalty,
    };
}

/// <summary>
/// Ограничение доступности преподавателя в конкретном слоте (день + пара). Знак <see cref="Penalty"/>
/// задаёт градацию: положительный вес штрафует назначение, отрицательный — поощряет
/// (см. <see cref="AvailabilityStates"/>).
/// </summary>
public class TeacherAvailability
{
    private TeacherAvailability() { }

    public Guid Id { get; private set; }

    /// <summary>Знаковый штраф за назначение занятия в данный слот (см. <see cref="AvailabilityStates"/>).</summary>
    public int Penalty { get; private set; }

    public Teacher Teacher { get; private set; } = null!;
    public Guid TeacherId { get; private set; }

    /// <summary>Номер пары, к которой относится ограничение.</summary>
    public int NumberLesson { get; private set; }

    public WeekDayType DayOfWeek { get; private set; }

    /// <summary>Создать запись доступности преподавателя для слота.</summary>
    public static TeacherAvailability Create(Guid teacherId, WeekDayType dayOfWeek, int numberLesson, int penalty) => new()
    {
        Id = Guid.NewGuid(),
        TeacherId = Guard.NotEmpty(teacherId, nameof(teacherId)),
        DayOfWeek = Guard.Defined(dayOfWeek, nameof(dayOfWeek)),
        NumberLesson = Guard.Positive(numberLesson, nameof(numberLesson)),
        Penalty = penalty,
    };
}
