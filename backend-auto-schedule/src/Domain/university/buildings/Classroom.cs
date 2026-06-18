using Domain.common;
using Domain.constraints;
using Domain.constraints.equipments;
using Domain.schedule;

namespace Domain.university.buildings;

/// <summary>Учебная аудитория с информацией о вместимости и оснащении.</summary>
public class Classroom
{
    private Classroom() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;

    /// <summary>Максимальная вместимость аудитории (количество мест).</summary>
    public int Capacity { get; private set; }

    public Guid BuildingId { get; private set; }
    public Building Building { get; private set; } = null!;

    /// <summary>Занятия, назначенные в данную аудиторию.</summary>
    public List<Lesson> Lessons { get; private set; } = [];

    /// <summary>Ограничения доступности аудитории по дням и парам.</summary>
    public List<ClassroomAvailability> ClassroomAvailabilities { get; private set; } = [];

    /// <summary>Оборудование, установленное в аудитории.</summary>
    public List<EquipmentRoom> EquipmentRooms { get; private set; } = [];

    /// <summary>Вмещает ли аудитория указанное число студентов.</summary>
    public bool CanAccommodate(int studentsCount) => Capacity >= studentsCount;

    /// <summary>Создать аудиторию в составе корпуса.</summary>
    public static Classroom Create(Guid id, string name, int capacity, Guid buildingId) => new()
    {
        Id = Guard.NotEmpty(id, nameof(id)),
        Name = Guard.NotBlank(name, nameof(name)),
        Capacity = Guard.Positive(capacity, nameof(capacity)),
        BuildingId = Guard.NotEmpty(buildingId, nameof(buildingId))
    };
}
