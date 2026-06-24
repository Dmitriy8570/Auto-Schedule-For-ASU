using Application.Solver.Builder;
using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Builder.BuildSections;
using Application.Solver.Model;
using Domain.calendar;
using Domain.constraints;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.teachers;
using Domain.workload;
using Google.OrTools.Sat;
using Schedule.DevSolver.Tests.Reflection;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Модульные тесты мягких секционных строителей (штрафы в целевой функции) на крошечных
/// детерминированных моделях: при минимизации солвер уводит занятие из штрафного слота
/// (Availability) и тянет его к «якорному» слоту прошлого семестра (PreviousScheduleAnchor).
/// </summary>
public sealed class SoftConstraintBuilderTests
{
    private static readonly Guid Bld = Guid.NewGuid();

    private static Classroom Room(int capacity) =>
        Classroom.Create(Guid.NewGuid(), $"r-{Guid.NewGuid():N}", capacity, Bld);

    private static TimeSlot SlotOn(WeekDayType day, int number)
    {
        var weekDay = DomainFactory.New<WeekDay>().Set(nameof(WeekDay.DayOfWeek), day);
        return DomainFactory.New<TimeSlot>()
            .Set(nameof(TimeSlot.Id), Guid.NewGuid())
            .Set(nameof(TimeSlot.Number), number)
            .Set(nameof(TimeSlot.WeekDay), weekDay);
    }

    private static (CpSolverStatus Status, CpSolver Solver, ScheduleModel Model) Solve(
        ScheduleData data, params IModelSectionBuilder[] builders)
    {
        var model = new ScheduleModelDirector(builders).Build(data);
        var solver = new CpSolver();
        var status = solver.Solve(model.Model);
        return (status, solver, model);
    }

    [Fact]
    public void TeacherAvailability_AvoidsPenalizedSlot()
    {
        var teacher = Teacher.Create(Guid.NewGuid(), "Иванов И. И.", Guid.NewGuid());
        // Пн, пара 1 — запрещённое время (большой штраф); остальное нейтрально.
        teacher.ReplaceAvailabilities(new[] { (WeekDayType.Monday, 1, AvailabilityState.Prohibited) });

        var curriculum = DomainFactory.New<Curriculum>().Set(nameof(Curriculum.Teacher), teacher);
        var workload = DomainFactory.New<SemesterWorkload>()
            .Set(nameof(SemesterWorkload.Id), Guid.NewGuid())
            .Set(nameof(SemesterWorkload.Hours), 2)            // 1 пара
            .Set(nameof(SemesterWorkload.Curriculum), curriculum);

        var penalized = SlotOn(WeekDayType.Monday, 1);          // индекс 0
        var free = SlotOn(WeekDayType.Monday, 2);               // индекс 1
        var data = new ScheduleData(new[] { workload }.ToItems(), new[] { Room(50) }, new[] { penalized, free },
            Array.Empty<ConstraintConfig>());

        var (status, solver, model) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(),
            new AvailabilitySectionBuilder(), new ObjectiveSectionBuilder());

        Assert.Equal(CpSolverStatus.Optimal, status);
        Assert.Equal(0L, solver.Value(model.Lessons[0, 0, 0])); // не в запрещённом слоте
        Assert.Equal(1L, solver.Value(model.Lessons[0, 0, 1])); // ушло в свободный
    }

    [Fact]
    public void PreviousScheduleAnchor_PrefersAnchoredSlot()
    {
        var workload = SemesterWorkload.Create(Guid.NewGuid(), 2, Guid.NewGuid(), Guid.NewGuid()); // 1 пара

        var other = SlotOn(WeekDayType.Monday, 1);              // индекс 0 — не якорный
        var anchored = SlotOn(WeekDayType.Monday, 2);           // индекс 1 — слот прошлого семестра

        var anchors = new[] { new WorkloadAnchor(0, null, new[] { anchored.Id }) };
        var data = new ScheduleData(new[] { workload }.ToItems(), new[] { Room(50) }, new[] { other, anchored },
            Array.Empty<ConstraintConfig>(), Anchors: anchors);

        var (status, solver, model) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(),
            new PreviousScheduleAnchorSectionBuilder(), new ObjectiveSectionBuilder());

        Assert.Equal(CpSolverStatus.Optimal, status);
        Assert.Equal(1L, solver.Value(model.Lessons[0, 0, 1])); // в якорном слоте
        Assert.Equal(0L, solver.Value(model.Lessons[0, 0, 0]));
    }
}
