using Domain.workload;

namespace Domain.calendar
{
    /// <summary>Учебный семестр с датами начала и конца.</summary>
    public class Semester
    {
        public Guid Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        /// <summary>Недели, входящие в семестр.</summary>
        public List<Week> Weeks { get; set; }

        /// <summary>Семестровые нагрузки по всем учебным планам.</summary>
        public List<SemesterWorkload> SemesterWorkloads { get; set; }
    }
}
