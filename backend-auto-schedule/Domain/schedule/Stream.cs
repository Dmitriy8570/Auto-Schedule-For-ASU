using Domain.workload;

namespace Domain.schedule
{
    /// <summary>
    /// Учебный поток — объединение групп, которые проходят одни и те же дисциплины вместе.
    /// </summary>
    public class AcademicStream
    {
        public Guid Id { get; set; }

        /// <summary>Суммарное количество студентов во всех группах потока.</summary>
        public int StudentsCount { get; set; }

        /// <summary>Группы, входящие в поток (связующая таблица).</summary>
        public List<StreamGroups> StreamGroups { get; set; }

        /// <summary>Учебные планы, по которым занимается поток.</summary>
        public List<Curriculum> Curriculums { get; set; }

        /// <summary>Занятия, назначенные потоку.</summary>
        public List<Lesson> Lessons { get; set; }
    }
}
