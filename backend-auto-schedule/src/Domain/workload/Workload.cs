using Domain.calendar;
using Domain.common;
using Domain.workload.logs;

namespace Domain.workload
{
    /// <summary>Понедельная нагрузка по конкретному учебному плану: сколько часов запланировано на данную неделю.</summary>
    public class WeekWorkload
    {
        private WeekWorkload() { }

        public Guid Id { get; private set; }

        /// <summary>Запланированное количество академических часов на неделю.</summary>
        public int Hours { get; private set; }

        public Curriculum Curriculum { get; private set; }
        public Guid CurriculumId { get; private set; }

        public Week Week { get; private set; }
        public Guid WeekId { get; private set; }

        public SemesterWorkload SemesterWorkload { get; private set; }
        public Guid SemesterWorkloadId { get; private set; }

        /// <summary>История изменений данной понедельной нагрузки.</summary>
        public List<WeekLog> WeekLogs { get; private set; }

        /// <summary>Создать понедельную нагрузку. Журнальную запись Add добавляйте через <see cref="RecordAdded"/>.</summary>
        public static WeekWorkload Create(
            Guid id, int hours, Guid curriculumId, Guid weekId, Guid semesterWorkloadId) => new()
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            Hours = Guard.NotNegative(hours, nameof(hours)),
            CurriculumId = Guard.NotEmpty(curriculumId, nameof(curriculumId)),
            WeekId = Guard.NotEmpty(weekId, nameof(weekId)),
            SemesterWorkloadId = Guard.NotEmpty(semesterWorkloadId, nameof(semesterWorkloadId)),
            WeekLogs = new List<WeekLog>()
        };

        /// <summary>Зафиксировать появление нагрузки в журнале (Add: 0 → <see cref="Hours"/>).</summary>
        public void RecordAdded(DateTime now)
        {
            WeekLogs ??= new List<WeekLog>();
            WeekLogs.Add(WeekLog.Create(LogAction.Add, 0, Hours, now, Id));
        }

        /// <summary>
        /// Изменить часы. Если значение отличается — фиксирует Update (old → new) в журнале и
        /// возвращает <c>true</c>; иначе ничего не делает и возвращает <c>false</c>.
        /// </summary>
        public bool ChangeHours(int newHours, DateTime now)
        {
            Guard.NotNegative(newHours, nameof(newHours));
            if (newHours == Hours)
                return false;

            WeekLogs ??= new List<WeekLog>();
            WeekLogs.Add(WeekLog.Create(LogAction.Update, Hours, newHours, now, Id));
            Hours = newHours;
            return true;
        }

        /// <summary>Зафиксировать удаление нагрузки в журнале (Delete: <see cref="Hours"/> → 0).</summary>
        public void RecordDeleted(DateTime now)
        {
            WeekLogs ??= new List<WeekLog>();
            WeekLogs.Add(WeekLog.Create(LogAction.Delete, Hours, 0, now, Id));
        }
    }

    /// <summary>Семестровая нагрузка по конкретному учебному плану: суммарное количество часов за семестр.</summary>
    public class SemesterWorkload
    {
        private SemesterWorkload() { }

        public Guid Id { get; private set; }

        /// <summary>Суммарное количество академических часов за семестр.</summary>
        public int Hours { get; private set; }

        public Curriculum Curriculum { get; private set; }
        public Guid CurriculumId { get; private set; }

        public Semester Semester { get; private set; }
        public Guid SemesterId { get; private set; }

        /// <summary>История изменений данной семестровой нагрузки.</summary>
        public List<SemesterLog> SemesterLogs { get; private set; }

        /// <summary>Разбивка нагрузки по неделям.</summary>
        public List<WeekWorkload> WeekWorkloads { get; private set; }

        /// <summary>Создать семестровую нагрузку. Журнальную запись Add добавляйте через <see cref="RecordAdded"/>.</summary>
        public static SemesterWorkload Create(Guid id, int hours, Guid curriculumId, Guid semesterId) => new()
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            Hours = Guard.NotNegative(hours, nameof(hours)),
            CurriculumId = Guard.NotEmpty(curriculumId, nameof(curriculumId)),
            SemesterId = Guard.NotEmpty(semesterId, nameof(semesterId)),
            SemesterLogs = new List<SemesterLog>(),
            WeekWorkloads = new List<WeekWorkload>()
        };

        /// <summary>Зафиксировать появление нагрузки в журнале (Add: 0 → <see cref="Hours"/>).</summary>
        public void RecordAdded(DateTime now)
        {
            SemesterLogs ??= new List<SemesterLog>();
            SemesterLogs.Add(SemesterLog.Create(LogAction.Add, 0, Hours, now, Id));
        }

        /// <summary>
        /// Изменить часы. Если значение отличается — фиксирует Update (old → new) в журнале и
        /// возвращает <c>true</c>; иначе ничего не делает и возвращает <c>false</c>.
        /// </summary>
        public bool ChangeHours(int newHours, DateTime now)
        {
            Guard.NotNegative(newHours, nameof(newHours));
            if (newHours == Hours)
                return false;

            SemesterLogs ??= new List<SemesterLog>();
            SemesterLogs.Add(SemesterLog.Create(LogAction.Update, Hours, newHours, now, Id));
            Hours = newHours;
            return true;
        }

        /// <summary>Зафиксировать удаление нагрузки в журнале (Delete: <see cref="Hours"/> → 0).</summary>
        public void RecordDeleted(DateTime now)
        {
            SemesterLogs ??= new List<SemesterLog>();
            SemesterLogs.Add(SemesterLog.Create(LogAction.Delete, Hours, 0, now, Id));
        }
    }
}
