using Domain.calendar;
using Domain.common;

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

        /// <summary>Создать временной слот для дня недели.</summary>
        public static TimeSlot Create(Guid id, Guid weekDayId, int number) => new()
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            WeekDayId = Guard.NotEmpty(weekDayId, nameof(weekDayId)),
            Number = Guard.Positive(number, nameof(number))
        };
    }
}
