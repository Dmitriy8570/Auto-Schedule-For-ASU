using Domain.university.buildings;

namespace Domain.constraints.equipments
{
    /// <summary>Связующая таблица: оборудование, установленное в конкретной аудитории (многие-ко-многим).</summary>
    public class EquipmentRoom
    {
        private EquipmentRoom() { }

        public Guid EquipmentId { get; private set; }
        public Guid ClassroomId { get; private set; }

        public Equipment Equipment { get; private set; }
        public Classroom Classroom { get; private set; }
    }
}
