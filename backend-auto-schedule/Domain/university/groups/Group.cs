using Domain.schedule;
using Domain.workload;

namespace Domain.university.groups
{
    /// <summary>Смена обучения (форма обучения по времени суток).</summary>
    public enum Shift
    {
        First = 0,
        Second = 1,
        Evening = 2,
    }

    /// <summary>Учебная группа студентов.</summary>
    public class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public Shift Shift { get; set; }

        /// <summary>Родительская группа (для подгрупп, напр. при делении на лабораторные).</summary>
        public Group ParentGroup { get; set; }

        /// <summary>Количество студентов в группе.</summary>
        public int StudentCount { get; set; }

        /// <summary>Подгруппы данной группы.</summary>
        public List<Group> Groups { get; set; }

        public Guid CourseId { get; set; }
        public Course Course { get; set; }

        /// <summary>Потоки, в которые входит данная группа.</summary>
        public List<StreamGroups> StreamGroups { get; set; }
    }
}
