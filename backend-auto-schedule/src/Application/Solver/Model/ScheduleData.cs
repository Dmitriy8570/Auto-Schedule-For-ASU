using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.workload;

namespace Application.Solver.Model;

/// <summary>
/// Входные данные задачи составления расписания на <em>одну учебную неделю</em>.
/// Ось «нагрузка» — <see cref="WorkloadItem"/> (часы недели + учебный план); остальные сущности
/// (преподаватели, группы, оборудование, корпуса, доступность) доступны через навигацию
/// <see cref="WorkloadItem.Curriculum"/> и <see cref="Classroom"/>. <see cref="TimeSlots"/> —
/// слоты только этой недели.
///
/// Для покомпонентной/поинститутской декомпозиции внутри недели заполняются дополнительные поля:
/// <list type="bullet">
/// <item><see cref="OccupiedClassroomSlots"/> и <see cref="OccupiedTeacherSlots"/> —
/// ресурсы, уже занятые в этой неделе ранее посчитанными компонентами/институтами
/// (жёсткие блокировки);</item>
/// <item><see cref="Anchors"/> — мягкие предпочтения слота/корпуса, перенесённые из уже решённой
/// недели того же типа (или из прошлого семестра) для стабильности расписания.</item>
/// </list>
/// Эти поля необязательны: при совместном решении всей недели сразу они равны <c>null</c>.
/// </summary>
public readonly record struct ScheduleData(
    IReadOnlyList<WorkloadItem> Workloads,
    IReadOnlyList<Classroom> Classrooms,
    IReadOnlyList<TimeSlot> TimeSlots,
    IReadOnlyList<ConstraintConfig> Penalties,
    IReadOnlyList<OccupiedSlot>? OccupiedClassroomSlots = null,
    IReadOnlyList<OccupiedTeacherSlot>? OccupiedTeacherSlots = null,
    IReadOnlyList<WorkloadAnchor>? Anchors = null,
    Guid SemesterId = default
);

/// <summary>
/// Ось «нагрузка» модели на неделю: сколько пар нужно поставить за эту неделю по конкретному
/// учебному плану. Проекция доменной <see cref="WeekWorkload"/> (или семестровой нагрузки в тестах):
/// <see cref="Hours"/> — часы именно этой недели, <see cref="Curriculum"/> — общий учебный план
/// (преподаватель, дисциплина, поток, оборудование, корпус).
/// </summary>
public readonly record struct WorkloadItem(int Hours, Curriculum Curriculum)
{
    /// <summary>Идентификатор учебного плана (для диагностики); пуст, если план не загружен.</summary>
    public Guid CurriculumId => Curriculum?.Id ?? Guid.Empty;
}

/// <summary>Аудитория, занятая в конкретном слоте этой недели (другим компонентом/институтом).</summary>
public readonly record struct OccupiedSlot(Guid ClassroomId, Guid TimeSlotId);

/// <summary>Преподаватель, занятый в конкретном слоте этой недели (другим компонентом/институтом).</summary>
public readonly record struct OccupiedTeacherSlot(Guid TeacherId, Guid TimeSlotId);

/// <summary>
/// Мягкое предпочтение для одной нагрузки, перенесённое из уже решённой недели того же типа
/// (или из прошлого семестра). Индекс ссылается на позицию нагрузки в <see cref="ScheduleData.Workloads"/>.
/// </summary>
/// <param name="WorkloadIndex">Индекс нагрузки в осях модели.</param>
/// <param name="PreferredBuildingId">Корпус, в котором аналогичное занятие шло ранее; <c>null</c> — нет данных.</param>
/// <param name="PreferredTimeSlotIds">Слоты этой недели, в которых аналогичное занятие шло ранее.</param>
public readonly record struct WorkloadAnchor(
    int WorkloadIndex,
    Guid? PreferredBuildingId,
    IReadOnlyList<Guid> PreferredTimeSlotIds
);
