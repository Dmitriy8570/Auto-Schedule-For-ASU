using Domain.common;
using Domain.workload;

namespace Domain.schedule
{
    /// <summary>Учебная дисциплина (предмет).</summary>
    public class Subject
    {
        private Subject() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        /// <summary>Учебные планы, в которых фигурирует данная дисциплина.</summary>
        public List<Curriculum> Curriculums { get; private set; }

        /// <summary>Создать учебную дисциплину.</summary>
        public static Subject Create(Guid id, string name) => new()
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            Name = Guard.NotBlank(name, nameof(name)),
            Curriculums = new List<Curriculum>()
        };
    }
}
