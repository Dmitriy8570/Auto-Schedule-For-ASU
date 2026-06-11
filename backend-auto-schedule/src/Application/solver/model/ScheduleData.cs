using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.workload;

namespace Application.Solver.Model;

public readonly record struct ScheduleData(
    IReadOnlyList<SemesterWorkload> SemesterWorkloads,
    IReadOnlyList<Classroom> Classrooms,
    IReadOnlyList<TimeSlot> TimeSlots,
    IReadOnlyList<ConstraintConfig> Penalties
);
