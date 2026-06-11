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
    }
}
