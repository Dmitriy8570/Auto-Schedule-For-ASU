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
/// </summary>
public readonly record struct ScheduleData(
    IReadOnlyList<SemesterWorkload> SemesterWorkloads,
    IReadOnlyList<Classroom> Classrooms,
    IReadOnlyList<TimeSlot> TimeSlots,
    IReadOnlyList<ConstraintConfig> Penalties
);
