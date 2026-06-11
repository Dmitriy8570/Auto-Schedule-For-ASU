using Domain.schedule;

namespace Domain.calendar
{
    /// <summary>День недели.</summary>
    public enum WeekDayType
    {
        Monday = 0,
        Tuesday = 1,
        Wednesday = 2,
        Thursday = 3,
        Friday = 4,
        Saturday = 5,
        Sunday = 6
    }

    /// <summary>Конкретный день в рамках учебной недели с набором временных слотов.</summary>
    public class WeekDay
    {
        private WeekDay() { }

        public Guid Id { get; private set; }

        public Guid WeekId { get; private set; }
        public Week Week { get; private set; }

        public WeekDayType DayOfWeek { get; private set; }

        /// <summary>Временные слоты (пары), доступные в этот день.</summary>
        public List<TimeSlot> TimeSlots { get; private set; }
    }
}
