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

        /// <summary>
        /// Семестровая нагрузка, к которой относится запись. Может быть <c>null</c> после
        /// физического удаления нагрузки (запись Delete переживает удаление — связь обнуляется).
        /// </summary>
        public SemesterWorkload? SemesterWorkload { get; private set; }
        public Guid? SemesterWorkloadId { get; private set; }

        /// <summary>Создать запись журнала семестровой нагрузки.</summary>
        public static SemesterLog Create(
            LogAction action, int oldValue, int newValue, DateTime timeStamp, Guid semesterWorkloadId) => new()
        {
            Id = Guid.NewGuid(),
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            TimeStamp = timeStamp,
            SemesterWorkloadId = semesterWorkloadId
        };
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

        /// <summary>
        /// Понедельная нагрузка, к которой относится запись. Может быть <c>null</c> после
        /// физического удаления нагрузки (запись Delete переживает удаление — связь обнуляется).
        /// </summary>
        public WeekWorkload? WeekWorkload { get; private set; }
        public Guid? WeekWorkloadId { get; private set; }

        /// <summary>Создать запись журнала понедельной нагрузки.</summary>
        public static WeekLog Create(
            LogAction action, int oldValue, int newValue, DateTime timeStamp, Guid weekWorkloadId) => new()
        {
            Id = Guid.NewGuid(),
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            TimeStamp = timeStamp,
            WeekWorkloadId = weekWorkloadId
        };
    }
}
