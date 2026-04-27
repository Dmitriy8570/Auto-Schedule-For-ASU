using Domain.workload;

namespace Domain.constraints.equipments
{
    /// <summary>Связующая таблица: оборудование, необходимое для конкретного учебного плана (многие-ко-многим).</summary>
    public class NeededEquipment
    {
        private NeededEquipment() { }

        public Guid CurriculumId { get; private set; }
        public Guid EquipmentId { get; private set; }

        public Curriculum Curriculum { get; private set; }
        public Equipment Equipment { get; private set; }
    }
}
