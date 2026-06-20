using Domain.common;
using Domain.workload;

namespace Domain.constraints.equipments;

/// <summary>Связующая таблица: оборудование, необходимое для конкретного учебного плана (многие-ко-многим).</summary>
public class NeededEquipment
{
    private NeededEquipment() { }

    public Guid CurriculumId { get; private set; }
    public Guid EquipmentId { get; private set; }

    public Curriculum Curriculum { get; private set; } = null!;
    public Equipment Equipment { get; private set; } = null!;

    /// <summary>Создать связь «учебный план ↔ необходимое оборудование».</summary>
    public static NeededEquipment Create(Guid curriculumId, Guid equipmentId) => new()
    {
        CurriculumId = Guard.NotEmpty(curriculumId, nameof(curriculumId)),
        EquipmentId = Guard.NotEmpty(equipmentId, nameof(equipmentId)),
    };
}
