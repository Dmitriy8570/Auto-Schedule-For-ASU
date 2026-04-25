namespace Domain.university.groups
{
    /// <summary>Курс обучения (1-й, 2-й, …) внутри конкретной ступени.</summary>
    public class Course
    {
        public Guid Id { get; set; }

        /// <summary>Номер курса (1, 2, 3, …).</summary>
        public int Number { get; set; }

        /// <summary>Учебные группы на данном курсе.</summary>
        public List<Group> Groups { get; set; }

        public Guid DegreeId { get; set; }
        public Degree Degree { get; set; }
    }
}
