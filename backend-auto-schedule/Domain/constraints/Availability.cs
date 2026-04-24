using Domain.calendar;
using Domain.university.buildings;
using Domain.university.teachers;

namespace Domain.constraints
{
    /// <summary>
    /// Ограничение доступности аудитории: если аудитория недоступна в указанный слот,
    /// назначение занятия в него штрафуется с весом <see cref="Penalty"/>.
    /// </summary>
    public class ClassroomAvailability
    {
        public Guid Id { get; set; }

        /// <summary>Штрафной коэффициент за назначение занятия в данный слот.</summary>
        public int Penalty { get; set; }

        public Classroom Classroom { get; set; }
        public Guid ClassroomId { get; set; }

        /// <summary>Номер пары, к которой относится ограничение.</summary>
        public int NumberLesson { get; set; }

        public WeekDayType DayOfWeek { get; set; }
    }

    /// <summary>
    /// Ограничение доступности преподавателя: если преподаватель недоступен в указанный слот,
    /// назначение занятия в него штрафуется с весом <see cref="Penalty"/>.
    /// </summary>
    public class TeacherAvailability
    {
        public Guid Id { get; set; }

        /// <summary>Штрафной коэффициент за назначение занятия в данный слот.</summary>
        public int Penalty { get; set; }

        public Teacher Teacher { get; set; }
        public Guid TeacherId { get; set; }

        /// <summary>Номер пары, к которой относится ограничение.</summary>
        public int NumberLesson { get; set; }

        public WeekDayType DayOfWeek { get; set; }
    }
}
