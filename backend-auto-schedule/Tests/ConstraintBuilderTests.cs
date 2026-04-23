using Application.solver.builder.buildSections;
using Application.solver.model;
using Domain.calendar;
using Domain.university.buildings;
using Domain.university.teachers;
using Domain.workload;
using Google.OrTools.Sat;
using ScheduleStream = Domain.schedule.Stream;
using StreamGroups = Domain.schedule.StreamGroups;
using TimeSlot = Domain.schedule.TimeSlot;
using Xunit;

namespace Tests;

// ─── Shared factory ──────────────────────────────────────────────────────────

file static class Factory
{
    public static SemesterWorkload Workload(int hours, Teacher? teacher = null, ScheduleStream? stream = null)
    {
        var t = teacher ?? new Teacher { Id = Guid.NewGuid(), Name = "T", Currilumus = [], TeacherAvailabilitys = [] };
        var s = stream ?? new ScheduleStream { Id = Guid.NewGuid(), StreamGroups = [], Curriculums = [], Lessons = [] };
        var c = new Curriculum
        {
            Id = Guid.NewGuid(),
            Teacher = t, TeacherId = t.Id,
            Stream = s, StreamId = s.Id,
            LessonType = LessonType.Lecture,
            WeekWorkloads = [], semesterWorkloads = [], NeededEquipments = []
        };
        return new SemesterWorkload
        {
            Id = Guid.NewGuid(),
            Hours = hours,
            Curriculum = c, CurriculumId = c.Id,
            SemesterLogs = [], WeekWorkloads = []
        };
    }

    public static Classroom Classroom() => new()
    {
        Id = Guid.NewGuid(), Name = "Room", Capacity = 30,
        Lessons = [], ClassroomAvailabilitys = [], EquipmentRooms = []
    };

    public static TimeSlot Slot(int number = 1)
    {
        var day = new WeekDay { Id = Guid.NewGuid(), TimeSlots = [] };
        return new TimeSlot { Id = Guid.NewGuid(), WeekDay = day, WeekDayId = day.Id, Number = number };
    }
}

// ─── TotalHoursConstraintSectionBuilder ──────────────────────────────────────

public class TotalHoursConstraintSectionBuilderTests
{
    // 1 workload, Hours=4 → нужно ровно 2 занятия.
    // 1 аудитория × 3 слота = 3 возможных переменных — достаточно для выполнения ограничения.
    [Fact]
    public void WhenSlotCountExceedsRequiredLessons_SolverIsFeasible()
    {
        var data = new ScheduleData(
            SemesterWorkloads: [Factory.Workload(hours: 4)],
            Classrooms:        [Factory.Classroom()],
            TimeSlots:         [Factory.Slot(1), Factory.Slot(2), Factory.Slot(3)],
            Penalties:         []);

        var model = new ScheduleModel(data);
        new VaribalesSectionBuilder().Build(model);
        new TotalHoursConstraintSectionBuilder().Build(model);

        var status = new CpSolver().Solve(model.Model);

        Assert.True(status is CpSolverStatus.Optimal or CpSolverStatus.Feasible);
    }

    // 1 workload, Hours=4 → нужно 2 занятия.
    // 1 аудитория × 1 слот = только 1 возможная переменная — ограничение невыполнимо.
    [Fact]
    public void WhenSlotCountBelowRequiredLessons_SolverIsInfeasible()
    {
        var data = new ScheduleData(
            SemesterWorkloads: [Factory.Workload(hours: 4)],
            Classrooms:        [Factory.Classroom()],
            TimeSlots:         [Factory.Slot(1)],
            Penalties:         []);

        var model = new ScheduleModel(data);
        new VaribalesSectionBuilder().Build(model);
        new TotalHoursConstraintSectionBuilder().Build(model);

        var status = new CpSolver().Solve(model.Model);

        Assert.Equal(CpSolverStatus.Infeasible, status);
    }
}

// ─── IntersectionSectionBuilder (classroom exclusivity) ──────────────────────

public class IntersectionSectionBuilderTests
{
    // 2 нагрузки, каждая требует ровно 1 занятие (Hours=2).
    // Только 1 аудитория и 1 слот: оба занятия должны попасть в одно и то же место →
    // IntersectionSectionBuilder запрещает это через AddAtMostOne.
    [Fact]
    public void WhenTwoWorkloadsCompeteForSingleRoomAndSlot_SolverIsInfeasible()
    {
        var data = new ScheduleData(
            SemesterWorkloads: [Factory.Workload(hours: 2), Factory.Workload(hours: 2)],
            Classrooms:        [Factory.Classroom()],
            TimeSlots:         [Factory.Slot(1)],
            Penalties:         []);

        var model = new ScheduleModel(data);
        new VaribalesSectionBuilder().Build(model);
        new TotalHoursConstraintSectionBuilder().Build(model);
        new IntersectionSectionBuilder().Build(model);

        var status = new CpSolver().Solve(model.Model);

        Assert.Equal(CpSolverStatus.Infeasible, status);
    }

    // 2 нагрузки, каждая требует 1 занятие.
    // 1 аудитория и 2 слота: каждая нагрузка может занять разный слот → конфликта нет.
    [Fact]
    public void WhenTwoWorkloadsHaveDistinctSlotsAvailable_SolverIsFeasible()
    {
        var data = new ScheduleData(
            SemesterWorkloads: [Factory.Workload(hours: 2), Factory.Workload(hours: 2)],
            Classrooms:        [Factory.Classroom()],
            TimeSlots:         [Factory.Slot(1), Factory.Slot(2)],
            Penalties:         []);

        var model = new ScheduleModel(data);
        new VaribalesSectionBuilder().Build(model);
        new TotalHoursConstraintSectionBuilder().Build(model);
        new IntersectionSectionBuilder().Build(model);

        var status = new CpSolver().Solve(model.Model);

        Assert.True(status is CpSolverStatus.Optimal or CpSolverStatus.Feasible);
    }
}
