using Domain.common;
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
        private Group() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public Shift Shift { get; private set; }

        /// <summary>Родительская группа (для подгрупп, напр. при делении на лабораторные); null — для основной группы.</summary>
        public Guid? ParentGroupId { get; private set; }
        public Group? ParentGroup { get; private set; }

        /// <summary>Количество студентов в группе.</summary>
        public int StudentCount { get; private set; }

        /// <summary>Подгруппы данной группы.</summary>
        public List<Group> Groups { get; private set; }

        public Guid CourseId { get; private set; }
        public Course Course { get; private set; }

        /// <summary>Потоки, в которые входит данная группа.</summary>
        public List<StreamGroups> StreamGroups { get; private set; }

        /// <summary>Является ли подгруппой (имеет родительскую группу).</summary>
        public bool IsSubgroup => ParentGroupId is not null;

        /// <summary>Создать основную учебную группу курса.</summary>
        public static Group Create(Guid id, string name, Shift shift, int studentCount, Guid courseId) => new()
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            Name = Guard.NotBlank(name, nameof(name)),
            Shift = Guard.Defined(shift, nameof(shift)),
            StudentCount = Guard.NotNegative(studentCount, nameof(studentCount)),
            CourseId = Guard.NotEmpty(courseId, nameof(courseId)),
            Groups = new List<Group>(),
            StreamGroups = new List<StreamGroups>()
        };

        /// <summary>
        /// Создать подгруппу данной группы (для деления на лабораторные и т.п.).
        /// Подгруппа наследует смену и курс родителя; её размер не может превышать размер родительской группы.
        /// </summary>
        public Group CreateSubgroup(Guid id, string name, int studentCount)
        {
            if (studentCount > StudentCount)
                throw new ArgumentOutOfRangeException(nameof(studentCount), studentCount,
                    "Размер подгруппы не может превышать размер родительской группы.");

            var subgroup = Create(id, name, Shift, studentCount, CourseId);
            subgroup.ParentGroupId = Id;

            Groups ??= new List<Group>();
            Groups.Add(subgroup);
            return subgroup;
        }
    }
}
