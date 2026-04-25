using Domain.university.groups;
using Domain.university.teachers;

namespace Domain.university.common
{
    /// <summary>Институт (факультет) университета.</summary>
    public class Institute
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        /// <summary>Степени (бакалавриат, магистратура и т.д.), реализуемые институтом.</summary>
        public List<Degree> Degrees { get; set; }

        /// <summary>Кафедры, входящие в состав института.</summary>
        public List<Department> Departments { get; set; }
    }
}
