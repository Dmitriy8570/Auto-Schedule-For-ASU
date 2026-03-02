using Domain.workload;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.calendar
{
    public class Semester
    {
        public Guid Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public List<Week> Weeks { get; set; }
        public List<SemesterWorkload> semesterWorkloads { get; set; }
    }
}
