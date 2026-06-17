using Domain.common;
using Domain.workload;

namespace Domain.university.buildings
{
    /// <summary>Учебный корпус университета.</summary>
    public class Building
    {
        private Building() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        /// <summary>Аудитории, расположенные в данном корпусе.</summary>
        public List<Classroom> Classrooms { get; private set; }

        /// <summary>Учебные планы, предпочитающие данный корпус.</summary>
        public List<Curriculum> Curriculums { get; private set; }

        /// <summary>Создать учебный корпус.</summary>
        public static Building Create(Guid id, string name) => new()
        {
            Id = Guard.NotEmpty(id, nameof(id)),
            Name = Guard.NotBlank(name, nameof(name)),
            Classrooms = new List<Classroom>(),
            Curriculums = new List<Curriculum>()
        };
    }
}
