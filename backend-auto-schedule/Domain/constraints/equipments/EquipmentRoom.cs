using Domain.university.buildings;

namespace Domain.constraints.equipments
{
    /// <summary>Связующая таблица: оборудование, установленное в конкретной аудитории (многие-ко-многим).</summary>
    public class EquipmentRoom
    {
        public Guid EquipmentId { get; set; }
        public Guid ClassroomId { get; set; }

        public Equipment Equipment { get; set; }
        public Classroom Classroom { get; set; }
    }
}
