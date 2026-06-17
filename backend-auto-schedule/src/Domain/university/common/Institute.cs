using Domain.common;
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

        /// <summary>Создать институт с заданным наименованием.</summary>
        public static Institute Create(Guid id, string name) => new()
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            Name = Guard.NotBlank(name, nameof(name)),
            Degrees = new List<Degree>(),
            Departments = new List<Department>()
        };
    }
}
