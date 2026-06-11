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
        private ClassroomAvailability() { }

        public Guid Id { get; private set; }

        /// <summary>Штрафной коэффициент за назначение занятия в данный слот.</summary>
        public int Penalty { get; private set; }

        public Classroom Classroom { get; private set; }
        public Guid ClassroomId { get; private set; }

        /// <summary>Номер пары, к которой относится ограничение.</summary>
        public int NumberLesson { get; private set; }

        public WeekDayType DayOfWeek { get; private set; }
    }

    /// <summary>
    /// Ограничение доступности преподавателя: если преподаватель недоступен в указанный слот,
    /// назначение занятия в него штрафуется с весом <see cref="Penalty"/>.
    /// </summary>
    public class TeacherAvailability
    {
        private TeacherAvailability() { }

        public Guid Id { get; private set; }

        /// <summary>Штрафной коэффициент за назначение занятия в данный слот.</summary>
        public int Penalty { get; private set; }

        public Teacher Teacher { get; private set; }
        public Guid TeacherId { get; private set; }

        /// <summary>Номер пары, к которой относится ограничение.</summary>
        public int NumberLesson { get; private set; }

        public WeekDayType DayOfWeek { get; private set; }
    }
}
