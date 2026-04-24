using Domain.workload;

namespace Domain.calendar
{
    /// <summary>Тип учебной недели в соответствии с принятой в АГУ системой чередования.</summary>
    public enum WeekType
    {
        /// <summary>Красная неделя (числитель).</summary>
        Red = 0,
        /// <summary>Синяя неделя (знаменатель).</summary>
        Blue = 1,
    }

    /// <summary>Учебная неделя семестра с привязкой к датам и типу чередования.</summary>
    public class Week
    {
        public Guid Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public WeekType WeekType { get; set; }

        public Guid SemesterId { get; set; }
        public Semester Semester { get; set; }

        /// <summary>Рабочие дни недели.</summary>
        public List<WeekDay> WeekDays { get; set; }

        /// <summary>Понедельные нагрузки, запланированные на эту неделю.</summary>
        public List<WeekWorkload> WeekWorkloads { get; set; }
    }
}
