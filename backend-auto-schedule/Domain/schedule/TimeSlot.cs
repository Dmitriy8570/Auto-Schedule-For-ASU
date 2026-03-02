using Domain.calendar;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.schedule
{
    public class TimeSlot
    {
        public WeekDay WeekDay { get; set; }
        public Guid DayId { get; set; }

        public int Number { get; set; } //occupation number
    }
}
