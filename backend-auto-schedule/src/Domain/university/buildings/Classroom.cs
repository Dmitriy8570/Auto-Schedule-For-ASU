using Domain.calendar;
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

    /// <summary>Изменить параметры аудитории (название, вместимость, корпус).</summary>
    public void Update(string name, int capacity, Guid buildingId)
    {
        Name = Guard.NotBlank(name, nameof(name));
        Capacity = Guard.Positive(capacity, nameof(capacity));
        BuildingId = Guard.NotEmpty(buildingId, nameof(buildingId));
    }

    /// <summary>
    /// Привести оснащение аудитории к заданному набору оборудования. Реализовано через разницу
    /// (а не Clear + повторное добавление), чтобы не создавать конфликт отслеживания EF при
    /// сохранении связей с прежним составным ключом (EquipmentId, ClassroomId).
    /// </summary>
    public void SetEquipment(IEnumerable<Guid> equipmentIds)
    {
        var target = equipmentIds.Distinct().ToHashSet();
        EquipmentRooms.RemoveAll(er => !target.Contains(er.EquipmentId));

        var existing = EquipmentRooms.Select(er => er.EquipmentId).ToHashSet();
        foreach (var equipmentId in target)
            if (existing.Add(equipmentId))
                EquipmentRooms.Add(EquipmentRoom.Create(equipmentId, Id));
    }

    /// <summary>
    /// Полностью заменить сетку доступности аудитории. Нейтральные слоты не хранятся:
    /// отсутствие записи равнозначно нейтральной градации.
    /// </summary>
    public void ReplaceAvailabilities(IEnumerable<(WeekDayType DayOfWeek, int PairNumber, AvailabilityState State)> cells)
    {
        ClassroomAvailabilities.Clear();
        foreach (var (day, pair, state) in cells)
        {
            if (state == AvailabilityState.Neutral) continue;
            ClassroomAvailabilities.Add(
                ClassroomAvailability.Create(Id, day, pair, AvailabilityStates.ToPenalty(state)));
        }
    }
}
