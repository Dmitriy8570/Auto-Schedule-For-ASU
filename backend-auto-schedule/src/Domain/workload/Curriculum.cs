using Domain.constraints.equipments;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.teachers;
using System.Text.RegularExpressions;

namespace Domain.workload
{
    /// <summary>Тип проводимого занятия.</summary>
    public enum LessonType
    {
        Lecture = 0,
        Seminar = 1,
        Laboratory = 2,
        Consultation = 3,
        Examination = 4
    }

    /// <summary>
    /// Учебный план — запись о том, что конкретный преподаватель ведёт конкретную дисциплину
    /// для конкретного потока в определённом формате занятия.
    /// </summary>
    public class Curriculum
    {
        private Curriculum() { }

        public Guid Id { get; private set; }

        /// <summary>
        /// Если <c>true</c>, занятие может проводиться параллельно с другими потоками
        /// (напр., общая лекция для нескольких групп).
        /// </summary>
        public bool Parallelism { get; private set; }

        /// <summary>Если <c>true</c>, занятие занимает два подряд идущих слота (двойная пара).</summary>
        public bool Double { get; private set; }

        public Guid TeacherId { get; private set; }
        public Teacher Teacher { get; private set; }

        public Guid StreamId { get; private set; }
        public AcademicStream Stream { get; private set; }

        public Guid SubjectId { get; private set; }
        public Subject Subject { get; private set; }

        public LessonType LessonType { get; private set; }

        /// <summary>Предпочтительный корпус для проведения занятий (мягкое ограничение).</summary>
        public Guid FavoriteBuildingId { get; private set; }
        public Building FavoriteBuilding { get; private set; }

        public List<WeekWorkload> WeekWorkloads { get; private set; }
        public List<SemesterWorkload> SemesterWorkloads { get; private set; }

        /// <summary>Оборудование, необходимое для проведения занятий по данному плану.</summary>
        public List<NeededEquipment> NeededEquipments { get; private set; }
    }
}
