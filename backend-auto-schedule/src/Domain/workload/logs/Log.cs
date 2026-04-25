using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.workload.logs
{
    public enum Actions
    {
        Add = 0,
        Update = 1,
        Delete = 2,
    }
    public class SemesterLog
    {
        public Guid Id { get; set; }
        public Actions Action { get; set; }
        public int OldValue { get; set; }
        public int NewValue { get; set; }

        public DateTime TimeStamp { get; set; }

        public SemesterWorkload SemesterWorkload { get; set; }
        public Guid SemesterWorkloadId { get; set; }
    }

    public class WeekLog
    {
        public Guid Id { get; set; }
        public Actions Action { get; set; }
        public int OldValue { get; set; }
        public int NewValue { get; set; }

        public DateTime TimeStamp { get; set; }

        public WeekWorkload WeekWorkload { get; set; }
        public Guid WeekWorkloadId { get; set; }
    }
}
