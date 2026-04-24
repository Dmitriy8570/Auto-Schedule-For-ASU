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
        public Guid Id { get; set; }

        public Classroom Classroom { get; set; }
        public Guid ClassroomId { get; set; }

        public Guid TimeSlotId { get; set; }
        public TimeSlot TimeSlot { get; set; }

        public AcademicStream Stream { get; set; }
        public Guid StreamId { get; set; }

        public ScheduleVersion Version { get; set; }
    }
}
