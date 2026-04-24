using Domain.university.common;

namespace Domain.university.teachers
{
    /// <summary>Кафедра университета, объединяющая преподавателей по научно-методическому направлению.</summary>
    public class Department
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        /// <summary>Преподаватели, прикреплённые к кафедре.</summary>
        public List<Teacher> Teachers { get; set; }

        public Guid InstituteId { get; set; }
        public Institute Institute { get; set; }
    }
}
