using Domain.calendar;
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
    }
}
