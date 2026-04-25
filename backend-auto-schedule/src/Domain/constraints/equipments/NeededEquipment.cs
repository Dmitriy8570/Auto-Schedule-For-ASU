using Domain.workload;

namespace Domain.constraints.equipments
{
    /// <summary>Связующая таблица: оборудование, необходимое для конкретного учебного плана (многие-ко-многим).</summary>
    public class NeededEquipment
    {
        public Guid CurriculumId { get; set; }
        public Guid EquipmentId { get; set; }

        public Curriculum Curriculum { get; set; }
        public Equipment Equipment { get; set; }
    }
}
