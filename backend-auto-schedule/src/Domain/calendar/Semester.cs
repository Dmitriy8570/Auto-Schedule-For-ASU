using Domain.workload;

namespace Domain.calendar
{
    /// <summary>Учебный семестр с датами начала и конца.</summary>
    public class Semester
    {
        private Semester() { }

        public Guid Id { get; private set; }
        public DateOnly StartDate { get; private set; }
        public DateOnly EndDate { get; private set; }

        /// <summary>Недели, входящие в семестр.</summary>
        public List<Week> Weeks { get; private set; }

        /// <summary>Семестровые нагрузки по всем учебным планам.</summary>
        public List<SemesterWorkload> SemesterWorkloads { get; private set; }
    }
}
