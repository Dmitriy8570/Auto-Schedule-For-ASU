namespace Domain.workload.logs
{
    /// <summary>Тип операции, зафиксированной в журнале изменений нагрузки.</summary>
    public enum LogAction
    {
        Add = 0,
        Update = 1,
        Delete = 2,
    }

    /// <summary>Запись журнала изменений семестровой нагрузки.</summary>
    public class SemesterLog
    {
        public Guid Id { get; set; }
        public LogAction Action { get; set; }

        /// <summary>Значение часов до изменения.</summary>
        public int OldValue { get; set; }

        /// <summary>Значение часов после изменения.</summary>
        public int NewValue { get; set; }

        public DateTime TimeStamp { get; set; }

        public SemesterWorkload SemesterWorkload { get; set; }
        public Guid SemesterWorkloadId { get; set; }
    }

    /// <summary>Запись журнала изменений понедельной нагрузки.</summary>
    public class WeekLog
    {
        public Guid Id { get; set; }
        public LogAction Action { get; set; }

        /// <summary>Значение часов до изменения.</summary>
        public int OldValue { get; set; }

        /// <summary>Значение часов после изменения.</summary>
        public int NewValue { get; set; }

        public DateTime TimeStamp { get; set; }

        public WeekWorkload WeekWorkload { get; set; }
        public Guid WeekWorkloadId { get; set; }
    }
}
