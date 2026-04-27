using Domain.calendar;

namespace Domain.schedule
{
    /// <summary>Временной слот: конкретный номер пары в конкретный день недели.</summary>
    public class TimeSlot
    {
        private TimeSlot() { }

        public Guid Id { get; private set; }

        public WeekDay WeekDay { get; private set; }
        public Guid WeekDayId { get; private set; }

        /// <summary>Номер пары в течение дня (1-й, 2-й, … ).</summary>
        public int Number { get; private set; }
    }
}
