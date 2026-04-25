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
        public Guid Id { get; set; }

        /// <summary>
        /// Если <c>true</c>, занятие может проводиться параллельно с другими потоками
        /// (напр., общая лекция для нескольких групп).
        /// </summary>
        public bool Parallelism { get; set; }

        /// <summary>Если <c>true</c>, занятие занимает два подряд идущих слота (двойная пара).</summary>
        public bool Double { get; set; }

        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; }

        public Guid StreamId { get; set; }
        public AcademicStream Stream { get; set; }

        public Guid SubjectId { get; set; }
        public Subject Subject { get; set; }

        public LessonType LessonType { get; set; }

        /// <summary>Предпочтительный корпус для проведения занятий (мягкое ограничение).</summary>
        public Guid FavoriteBuildingId { get; set; }
        public Building FavoriteBuilding { get; set; }

        public List<WeekWorkload> WeekWorkloads { get; set; }
        public List<SemesterWorkload> SemesterWorkloads { get; set; }

        /// <summary>Оборудование, необходимое для проведения занятий по данному плану.</summary>
        public List<NeededEquipment> NeededEquipments { get; set; }
    }
}
