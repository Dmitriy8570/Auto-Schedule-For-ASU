using Domain.calendar;
using Domain.common;
using Domain.university.buildings;
using Domain.workload;

namespace Domain.schedule
{
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

        public Classroom Classroom { get; private set; }
        public Guid ClassroomId { get; private set; }

        public Guid TimeSlotId { get; private set; }
        public TimeSlot TimeSlot { get; private set; }

        public AcademicStream Stream { get; private set; }
        public Guid StreamId { get; private set; }

        public ScheduleVersion Version { get; private set; } = ScheduleVersion.Draft;

        public static Lesson Create(Guid id, Guid classroomId, Guid timeSlotId, Guid streamId) => new()
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            ClassroomId = Guard.NotEmpty(classroomId, nameof(classroomId)),
            TimeSlotId = Guard.NotEmpty(timeSlotId, nameof(timeSlotId)),
            StreamId = Guard.NotEmpty(streamId, nameof(streamId))
        };

        public void Publish() => Version = ScheduleVersion.Current;
    }
}
