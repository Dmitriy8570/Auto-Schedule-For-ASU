using Domain.schedule;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.calendar
{
    public enum DayOfWeek
    {
        Monday = 0,
        Tuesday = 1,
        Wednesday = 2,
        Thursday = 3,
        Friday = 4,
        Saturday = 5,
        Sunday = 6
    }

    public class WeekDay
    {
        public Guid Id { get; set; }
        

        public Guid WeekId {  get; set; }
        public Week Week { get; set; }


        public DayOfWeek DayOfWeek { get; set; }

        public List<TimeSlot> TimeSlots { get; set; }
    }
}
