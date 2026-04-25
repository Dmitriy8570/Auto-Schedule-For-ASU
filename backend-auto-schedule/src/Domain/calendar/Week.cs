using Domain.workload;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.calendar
{
    //There are two types of weeks at the university: red and blue.
    public enum WeekType
    {
        Red = 0,
        Blue = 1,
    }
    public class Week
    {
        public Guid Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public WeekType WeekType { get; set; }


        public Guid SemesterId { get; set; }
        public Semester Semester { get; set; }

        public List<WeekDay> WeekDays { get; set; }
        public List<WeekWorkload> WeekWorkloads { get; set; }
    }
}
