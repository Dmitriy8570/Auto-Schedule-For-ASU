using Domain.calendar;
using Domain.workload.logs;

namespace Domain.workload
{
    /// <summary>Понедельная нагрузка по конкретному учебному плану: сколько часов запланировано на данную неделю.</summary>
    public class WeekWorkload
    {
        public Guid Id { get; set; }

        /// <summary>Запланированное количество академических часов на неделю.</summary>
        public int Hours { get; set; }

        public Curriculum Curriculum { get; set; }
        public Guid CurriculumId { get; set; }

        public Week Week { get; set; }
        public Guid WeekId { get; set; }

        public SemesterWorkload SemesterWorkload { get; set; }
        public Guid SemesterWorkloadId { get; set; }

        /// <summary>История изменений данной понедельной нагрузки.</summary>
        public List<WeekLog> WeekLogs { get; set; }
    }

    /// <summary>Семестровая нагрузка по конкретному учебному плану: суммарное количество часов за семестр.</summary>
    public class SemesterWorkload
    {
        public Guid Id { get; set; }

        /// <summary>Суммарное количество академических часов за семестр.</summary>
        public int Hours { get; set; }

        public Curriculum Curriculum { get; set; }
        public Guid CurriculumId { get; set; }

        public Semester Semester { get; set; }
        public Guid SemesterId { get; set; }

        /// <summary>История изменений данной семестровой нагрузки.</summary>
        public List<SemesterLog> SemesterLogs { get; set; }

        /// <summary>Разбивка нагрузки по неделям.</summary>
        public List<WeekWorkload> WeekWorkloads { get; set; }
    }
}
