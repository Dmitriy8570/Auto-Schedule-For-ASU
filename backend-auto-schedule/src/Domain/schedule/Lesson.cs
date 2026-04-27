using Domain.calendar;
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

        public static Lesson Create(Guid id, Guid classroomId, Guid timeSlotId, Guid streamId)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));
            if (classroomId == Guid.Empty)
                throw new ArgumentException("ClassroomId cannot be empty.", nameof(classroomId));
            if (timeSlotId == Guid.Empty)
                throw new ArgumentException("TimeSlotId cannot be empty.", nameof(timeSlotId));
            if (streamId == Guid.Empty)
                throw new ArgumentException("StreamId cannot be empty.", nameof(streamId));

            return new Lesson
            {
                Id = id,
                ClassroomId = classroomId,
                TimeSlotId = timeSlotId,
                StreamId = streamId
            };
        }

        public void Publish() => Version = ScheduleVersion.Current;
    }
}
