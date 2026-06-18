using Domain.common;
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
        private Week() { }

        public Guid Id { get; private set; }
        public DateOnly StartDate { get; private set; }
        public DateOnly EndDate { get; private set; }

        public WeekType WeekType { get; private set; }

        public Guid SemesterId { get; private set; }
        public Semester Semester { get; private set; }

        /// <summary>Рабочие дни недели.</summary>
        public List<WeekDay> WeekDays { get; private set; }

        /// <summary>Понедельные нагрузки, запланированные на эту неделю.</summary>
        public List<WeekWorkload> WeekWorkloads { get; private set; }

        /// <summary>Создать учебную неделю семестра.</summary>
        public static Week Create(Guid id, DateOnly startDate, DateOnly endDate, WeekType weekType, Guid semesterId) => new()
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            StartDate = startDate,
            EndDate = endDate,
            WeekType = Guard.Defined(weekType, nameof(weekType)),
            SemesterId = Guard.NotEmpty(semesterId, nameof(semesterId)),
            WeekDays = new List<WeekDay>(),
            WeekWorkloads = new List<WeekWorkload>()
        };
    }
}
