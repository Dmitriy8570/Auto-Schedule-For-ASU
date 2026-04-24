using Domain.workload;

namespace Domain.schedule
{
    /// <summary>Учебная дисциплина (предмет).</summary>
    public class Subject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        /// <summary>Учебные планы, в которых фигурирует данная дисциплина.</summary>
        public List<Curriculum> Curriculums { get; set; }
    }
}
