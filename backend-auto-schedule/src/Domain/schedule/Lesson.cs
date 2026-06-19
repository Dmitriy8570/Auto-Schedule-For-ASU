using Domain.common;
using Domain.university.buildings;
using Domain.workload;

namespace Domain.schedule;

/// <summary>Состояние версии расписания.</summary>
public enum ScheduleVersion
{
    /// <summary>Текущее (опубликованное) расписание.</summary>
    Current = 0,
    /// <summary>Черновик расписания.</summary>
    Draft = 1
}

/// <summary>Конкретное занятие в расписании: привязка нагрузки к аудитории и временному слоту.</summary>
public class Lesson
{
    private Lesson() { }

    public Guid Id { get; private set; }

    public Classroom Classroom { get; private set; } = null!;
    public Guid ClassroomId { get; private set; }

    public Guid TimeSlotId { get; private set; }
    public TimeSlot TimeSlot { get; private set; } = null!;

    public AcademicStream Stream { get; private set; } = null!;
    public Guid StreamId { get; private set; }

    /// <summary>
    /// Учебный план (дисциплина + преподаватель + тип занятия), который реализует данное занятие.
    /// Поток может вести несколько планов, поэтому конкретный план фиксируется на занятии,
    /// чтобы можно было отобразить дисциплину/преподавателя/тип. Может быть <c>null</c> для
    /// «сырых» занятий, созданных без привязки к плану.
    /// </summary>
    public Guid? CurriculumId { get; private set; }
    public Curriculum? Curriculum { get; private set; }

    /// <summary>Семестр, к которому относится занятие (денормализовано для запросов и перегенерации).</summary>
    public Guid SemesterId { get; private set; }

    public ScheduleVersion Version { get; private set; } = ScheduleVersion.Draft;

    public static Lesson Create(
        Guid id, Guid classroomId, Guid timeSlotId, Guid streamId, Guid semesterId, Guid? curriculumId = null) => new()
    {
        Id = Guard.NotEmpty(id, nameof(id)),
        ClassroomId = Guard.NotEmpty(classroomId, nameof(classroomId)),
        TimeSlotId = Guard.NotEmpty(timeSlotId, nameof(timeSlotId)),
        StreamId = Guard.NotEmpty(streamId, nameof(streamId)),
        SemesterId = Guard.NotEmpty(semesterId, nameof(semesterId)),
        CurriculumId = curriculumId
    };

    public void Publish() => Version = ScheduleVersion.Current;
}
