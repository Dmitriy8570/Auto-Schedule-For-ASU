using Domain.common;
using Domain.constraints.equipments;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.teachers;

namespace Domain.workload;

/// <summary>Тип проводимого занятия.</summary>
public enum LessonType
{
    Lecture = 0,
    Seminar = 1,
    Laboratory = 2,
    Consultation = 3,
    Examination = 4
}

/// <summary>
/// Учебный план — запись о том, что конкретный преподаватель ведёт конкретную дисциплину
/// для конкретного потока в определённом формате занятия.
/// </summary>
public class Curriculum
{
    private Curriculum() { }

    public Guid Id { get; private set; }

    /// <summary>
    /// Если <c>true</c>, занятие может проводиться параллельно с другими потоками
    /// (напр., общая лекция для нескольких групп).
    /// </summary>
    public bool Parallelism { get; private set; }

    /// <summary>Если <c>true</c>, занятие занимает два подряд идущих слота (двойная пара).</summary>
    public bool Double { get; private set; }

    public Guid TeacherId { get; private set; }
    public Teacher Teacher { get; private set; } = null!;

    public Guid StreamId { get; private set; }
    public AcademicStream Stream { get; private set; } = null!;

    public Guid SubjectId { get; private set; }
    public Subject Subject { get; private set; } = null!;

    public LessonType LessonType { get; private set; }

    /// <summary>Предпочтительный корпус для проведения занятий (мягкое ограничение); null — если не задан.</summary>
    public Guid? FavoriteBuildingId { get; private set; }
    public Building? FavoriteBuilding { get; private set; }

    public List<WeekWorkload> WeekWorkloads { get; private set; } = [];
    public List<SemesterWorkload> SemesterWorkloads { get; private set; } = [];

    /// <summary>Оборудование, необходимое для проведения занятий по данному плану.</summary>
    public List<NeededEquipment> NeededEquipments { get; private set; } = [];

    /// <summary>Создать учебный план.</summary>
    public static Curriculum Create(
        Guid id, Guid teacherId, Guid streamId, Guid subjectId,
        LessonType lessonType, bool parallelism, bool @double, Guid? favoriteBuildingId = null) => new()
    {
        Id = Guard.NotEmpty(id, nameof(id)),
        TeacherId = Guard.NotEmpty(teacherId, nameof(teacherId)),
        StreamId = Guard.NotEmpty(streamId, nameof(streamId)),
        SubjectId = Guard.NotEmpty(subjectId, nameof(subjectId)),
        LessonType = Guard.Defined(lessonType, nameof(lessonType)),
        Parallelism = parallelism,
        Double = @double,
        FavoriteBuildingId = favoriteBuildingId
    };

    /// <summary>
    /// Изменить по-нагрузочные ограничения плана: параллельность, двойную пару и предпочтительный корпус.
    /// Состав необходимого оборудования (<see cref="NeededEquipments"/>) синхронизируется отдельно
    /// методом <see cref="SetRequiredEquipment"/>.
    /// </summary>
    public void SetConstraints(bool parallelism, bool @double, Guid? favoriteBuildingId)
    {
        Parallelism = parallelism;
        Double = @double;
        FavoriteBuildingId = favoriteBuildingId;
    }

    /// <summary>
    /// Задать предпочтительный корпус (мягкое ограничение), не затрагивая остальные ограничения плана.
    /// В отличие от <see cref="SetConstraints"/>, не сбрасывает <see cref="Parallelism"/>/<see cref="Double"/>.
    /// </summary>
    public void SetFavoriteBuilding(Guid? favoriteBuildingId) => FavoriteBuildingId = favoriteBuildingId;

    /// <summary>
    /// Привести состав необходимого оборудования к заданному набору. Реализовано через разницу
    /// (а не Clear + повторное добавление), чтобы не создавать конфликт отслеживания EF при
    /// сохранении связей с прежним составным ключом (CurriculumId, EquipmentId).
    /// </summary>
    public void SetRequiredEquipment(IEnumerable<Guid> equipmentIds)
    {
        var target = equipmentIds.Distinct().ToHashSet();
        NeededEquipments.RemoveAll(n => !target.Contains(n.EquipmentId));

        var existing = NeededEquipments.Select(n => n.EquipmentId).ToHashSet();
        foreach (var equipmentId in target)
            if (existing.Add(equipmentId))
                NeededEquipments.Add(NeededEquipment.Create(Id, equipmentId));
    }
}
