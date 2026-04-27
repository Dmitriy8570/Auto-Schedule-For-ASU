using Domain.university.common;

namespace Domain.university.teachers
{
    /// <summary>Кафедра университета, объединяющая преподавателей по научно-методическому направлению.</summary>
    public class Department
    {
        private Department() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        /// <summary>Преподаватели, прикреплённые к кафедре.</summary>
        public List<Teacher> Teachers { get; private set; }

        public Guid InstituteId { get; private set; }
        public Institute Institute { get; private set; }
    }
}
