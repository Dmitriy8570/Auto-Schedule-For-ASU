using Domain.calendar;

namespace Domain.schedule
{
    /// <summary>Временной слот: конкретный номер пары в конкретный день недели.</summary>
    public class TimeSlot
    {
        public Guid Id { get; set; }

        public WeekDay WeekDay { get; set; }
        public Guid WeekDayId { get; set; }

        /// <summary>Номер пары в течение дня (1-й, 2-й, … ).</summary>
        public int Number { get; set; }
    }
}
