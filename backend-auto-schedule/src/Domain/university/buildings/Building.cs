using Domain.workload;

namespace Domain.university.buildings
{
    /// <summary>Учебный корпус университета.</summary>
    public class Building
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        /// <summary>Аудитории, расположенные в данном корпусе.</summary>
        public List<Classroom> Classrooms { get; set; }

        /// <summary>Учебные планы, предпочитающие данный корпус.</summary>
        public List<Curriculum> Curriculums { get; set; }
    }
}
