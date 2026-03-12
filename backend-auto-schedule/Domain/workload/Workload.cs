using Domain.calendar;
using Domain.workload.logs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.workload
{
    public class WeekWorkload
    {
        public Guid Id { get; set; }
        public int Hours { get; set; }

        public Curriculum Curriculum { get; set; }
        public Guid CurriculumId { get; set; }

        public Week Week { get; set; }
        public Guid WeekId { get; set; }

        public SemesterWorkload SemesterWorkload { get; set; }
        public Guid SemesterWorkloadId { get; set; }
        public List<WeekLog> WeekLogs { get; set; }
    }

    public class SemesterWorkload
    {
        public Guid Id { get; set; }
        public int Hours { get; set; }

        public Curriculum Curriculum {  get; set; }
        public Guid CurriculumId { get; set; }

        public Semester Semester { get; set; }
        public Guid Semesterid { get; set; }

        public List<SemesterLog> SemesterLogs { get; set; }
        public List<WeekWorkload> WeekWorkloads { get; set; }
    }
}
