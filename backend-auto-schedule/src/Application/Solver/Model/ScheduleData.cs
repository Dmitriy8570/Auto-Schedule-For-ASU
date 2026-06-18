using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.workload;

namespace Application.Solver.Model;

/// <summary>
/// Входные данные задачи составления расписания (экспериментальная версия).
/// Остальные сущности (преподаватели, группы, оборудование, корпуса, доступность)
/// доступны через навигационные свойства <see cref="SemesterWorkload"/> и <see cref="Classroom"/>,
/// поэтому в данных хранятся только три «оси» модели и веса мягких ограничений.
///
/// Для генерации по отдельному институту (декомпозиция «B + C») заполняются
/// дополнительные поля:
/// <list type="bullet">
/// <item><see cref="OccupiedClassroomSlots"/> и <see cref="OccupiedTeacherSlots"/> —
/// общие ресурсы, уже занятые ранее посчитанными институтами этого же семестра
/// (жёсткие блокировки, вариант «B»);</item>
/// <item><see cref="Anchors"/> — мягкие предпочтения, перенесённые из расписания
/// прошлого семестра, для стабильности (вариант «C»).</item>
/// </list>
/// Все три поля необязательны: при генерации на весь семестр сразу они равны <c>null</c>.
/// </summary>
public readonly record struct ScheduleData(
    IReadOnlyList<SemesterWorkload> SemesterWorkloads,
    IReadOnlyList<Classroom> Classrooms,
    IReadOnlyList<TimeSlot> TimeSlots,
    IReadOnlyList<ConstraintConfig> Penalties,
    IReadOnlyList<OccupiedSlot>? OccupiedClassroomSlots = null,
    IReadOnlyList<OccupiedTeacherSlot>? OccupiedTeacherSlots = null,
    IReadOnlyList<WorkloadAnchor>? Anchors = null
);

/// <summary>Аудитория, занятая в конкретном слоте расписанием другого института этого семестра.</summary>
public readonly record struct OccupiedSlot(Guid ClassroomId, Guid TimeSlotId);

/// <summary>Преподаватель, занятый в конкретном слоте расписанием другого института этого семестра.</summary>
public readonly record struct OccupiedTeacherSlot(Guid TeacherId, Guid TimeSlotId);

/// <summary>
/// Мягкое предпочтение для одной нагрузки, перенесённое из расписания прошлого семестра.
/// Индекс ссылается на позицию нагрузки в <see cref="ScheduleData.SemesterWorkloads"/>.
/// </summary>
/// <param name="WorkloadIndex">Индекс нагрузки в осях модели.</param>
/// <param name="PreferredBuildingId">Корпус, в котором аналогичное занятие шло в прошлом семестре; <c>null</c> — нет данных.</param>
/// <param name="PreferredTimeSlotIds">Слоты, в которых аналогичное занятие шло в прошлом семестре.</param>
public readonly record struct WorkloadAnchor(
    int WorkloadIndex,
    Guid? PreferredBuildingId,
    IReadOnlyList<Guid> PreferredTimeSlotIds
);
