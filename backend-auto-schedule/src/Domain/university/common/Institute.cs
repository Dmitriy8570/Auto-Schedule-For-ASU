using Domain.university.groups;
using Domain.university.teachers;

namespace Domain.university.common
{
    /// <summary>Институт (факультет) университета.</summary>
    public class Institute
    {
        private Institute() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        /// <summary>Степени (бакалавриат, магистратура и т.д.), реализуемые институтом.</summary>
        public List<Degree> Degrees { get; private set; }

        /// <summary>Кафедры, входящие в состав института.</summary>
        public List<Department> Departments { get; private set; }
    }
}
