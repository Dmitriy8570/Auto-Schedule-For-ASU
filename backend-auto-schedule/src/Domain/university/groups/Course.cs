using Domain.common;

namespace Domain.university.groups
{
    /// <summary>Курс обучения (1-й, 2-й, …) внутри конкретной ступени.</summary>
    public class Course
    {
        private Course() { }

        public Guid Id { get; private set; }

        /// <summary>Номер курса (1, 2, 3, …).</summary>
        public int Number { get; private set; }

        /// <summary>Учебные группы на данном курсе.</summary>
        public List<Group> Groups { get; private set; }

        public Guid DegreeId { get; private set; }
        public Degree Degree { get; private set; }

        /// <summary>Создать курс в составе ступени.</summary>
        public static Course Create(Guid id, int number, Guid degreeId) => new()
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            Number = Guard.Positive(number, nameof(number)),
            DegreeId = Guard.NotEmpty(degreeId, nameof(degreeId)),
            Groups = new List<Group>()
        };
    }
}
