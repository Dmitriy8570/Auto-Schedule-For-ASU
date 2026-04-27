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
        private SemesterLog() { }

        public Guid Id { get; private set; }
        public LogAction Action { get; private set; }

        /// <summary>Значение часов до изменения.</summary>
        public int OldValue { get; private set; }

        /// <summary>Значение часов после изменения.</summary>
        public int NewValue { get; private set; }

        public DateTime TimeStamp { get; private set; }

        public SemesterWorkload SemesterWorkload { get; private set; }
        public Guid SemesterWorkloadId { get; private set; }
    }

    /// <summary>Запись журнала изменений понедельной нагрузки.</summary>
    public class WeekLog
    {
        private WeekLog() { }

        public Guid Id { get; private set; }
        public LogAction Action { get; private set; }

        /// <summary>Значение часов до изменения.</summary>
        public int OldValue { get; private set; }

        /// <summary>Значение часов после изменения.</summary>
        public int NewValue { get; private set; }

        public DateTime TimeStamp { get; private set; }

        public WeekWorkload WeekWorkload { get; private set; }
        public Guid WeekWorkloadId { get; private set; }
    }
}
