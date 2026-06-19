using Domain.common;

namespace Domain.constraints.equipments;

/// <summary>Оборудование, которое может быть установлено в аудитории или требоваться для занятия.</summary>
public class Equipment
{
    private Equipment() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;

    /// <summary>Аудитории, оснащённые данным оборудованием.</summary>
    public List<EquipmentRoom> EquipmentRooms { get; private set; } = [];

    /// <summary>Учебные планы, требующие данное оборудование.</summary>
    public List<NeededEquipment> NeededEquipments { get; private set; } = [];

    /// <summary>Создать тип оборудования.</summary>
    public static Equipment Create(Guid id, string name) => new()
    {
        Id = Guard.NotEmpty(id, nameof(id)),
        Name = Guard.NotBlank(name, nameof(name))
    };

    /// <summary>Переименовать тип оборудования.</summary>
    public void Rename(string name) => Name = Guard.NotBlank(name, nameof(name));
}
