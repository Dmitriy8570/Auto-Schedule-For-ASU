using Domain.constraints;
using Domain.workload;

namespace Domain.university.teachers
{
    /// <summary>Преподаватель университета.</summary>
    public class Teacher
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public Guid DepartmentId { get; set; }
        public Department Department { get; set; }

        /// <summary>Учебные планы, которые ведёт данный преподаватель.</summary>
        public List<Curriculum> Curriculums { get; set; }

        /// <summary>Ограничения доступности преподавателя по дням и парам.</summary>
        public List<TeacherAvailability> TeacherAvailabilities { get; set; }
    }
}
