using Domain.constraints;
using Domain.workload;

namespace Domain.university.teachers
{
    /// <summary>Преподаватель университета.</summary>
    public class Teacher
    {
        private Teacher() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public Guid DepartmentId { get; private set; }
        public Department Department { get; private set; }

        /// <summary>Учебные планы, которые ведёт данный преподаватель.</summary>
        public List<Curriculum> Curriculums { get; private set; }

        /// <summary>Ограничения доступности преподавателя по дням и парам.</summary>
        public List<TeacherAvailability> TeacherAvailabilities { get; private set; }
    }
}
