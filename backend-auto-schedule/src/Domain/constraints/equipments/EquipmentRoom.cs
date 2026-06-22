using Domain.university.buildings;

namespace Domain.constraints.equipments;

/// <summary>Связующая таблица: оборудование, установленное в конкретной аудитории (многие-ко-многим).</summary>
public class EquipmentRoom
{
    private EquipmentRoom() { }

    public Guid EquipmentId { get; private set; }
    public Guid ClassroomId { get; private set; }

    public Equipment Equipment { get; private set; } = null!;
    public Classroom Classroom { get; private set; } = null!;

    /// <summary>Установить оборудование в аудиторию (создать связь «оборудование ↔ аудитория»).</summary>
    public static EquipmentRoom Create(Guid equipmentId, Guid classroomId) => new()
    {
        EquipmentId = equipmentId,
        ClassroomId = classroomId,
    };
}
