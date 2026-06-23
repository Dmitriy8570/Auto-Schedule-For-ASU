using Application.Solver.Compaction;
using Application.Solver.Model;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.groups;
using Domain.university.teachers;
using Domain.workload;
using Schedule.DevSolver.Tests.Reflection;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Модульные тесты пост-уплотнения (<see cref="ScheduleCompactor"/>): занятия сдвигаются в более
/// ранние свободные слоты того же дня, убирая «окна», но только при сохранении всех жёстких
/// ограничений (пересечения ресурсов, переходы между корпусами, блокировки чужих институтов,
/// неподвижность двойных пар).
/// </summary>
public sealed class ScheduleCompactorTests
{
    private static readonly Guid Day = Guid.NewGuid();
    private static readonly Guid Sem = Guid.NewGuid();
    private static readonly Guid BuildingX = Guid.NewGuid();
    private static readonly Guid BuildingY = Guid.NewGuid();

    [Fact]
    public void Compact_ShiftsLessonIntoWindow_RemovingGap()
    {
        var slot1 = Slot(1);
        var slot2 = Slot(2);
        var slot3 = Slot(3);
        var room = Room(BuildingX);
        var cur = Curriculum(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var early = Lesson(cur, room, slot1);
        var late = Lesson(cur, room, slot3); // окно в slot2: занят slot1 и slot3, пусто между ними

        var data = Data(new[] { cur }, new[] { room }, new[] { slot1, slot2, slot3 });

        int moved = ScheduleCompactor.Compact(
            new[] { early, late }, data,
            Array.Empty<OccupiedSlot>(), Array.Empty<OccupiedTeacherSlot>());

        Assert.Equal(1, moved);
        Assert.Equal(slot2.Id, late.TimeSlotId); // подтянули в окно
        Assert.Equal(slot1.Id, early.TimeSlotId); // первая пара осталась на месте
    }

    [Fact]
    public void Compact_KeepsWindow_WhenMoveWouldForceBuildingTransition()
    {
        var slot1 = Slot(1);
        var slot2 = Slot(2);
        var slot3 = Slot(3);
        var roomX = Room(BuildingX);
        var roomY = Room(BuildingY);

        var teacher = Guid.NewGuid();
        var group = Guid.NewGuid();
        var curX = Curriculum(teacher, group, Guid.NewGuid());
        var curY = Curriculum(teacher, group, Guid.NewGuid());

        var inX = Lesson(curX, roomX, slot1);
        var inY = Lesson(curY, roomY, slot3); // перенос в slot2 сделал бы slot1(X)–slot2(Y) соседними

        var data = Data(new[] { curX, curY }, new[] { roomX, roomY }, new[] { slot1, slot2, slot3 });

        int moved = ScheduleCompactor.Compact(
            new[] { inX, inY }, data,
            Array.Empty<OccupiedSlot>(), Array.Empty<OccupiedTeacherSlot>());

        Assert.Equal(0, moved);
        Assert.Equal(slot3.Id, inY.TimeSlotId);
    }

    [Fact]
    public void Compact_RespectsResourcesOccupiedByOtherInstitutes()
    {
        var slot1 = Slot(1);
        var slot2 = Slot(2);
        var slot3 = Slot(3);
        var room = Room(BuildingX);
        var cur = Curriculum(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var early = Lesson(cur, room, slot1);
        var late = Lesson(cur, room, slot3);

        var data = Data(new[] { cur }, new[] { room }, new[] { slot1, slot2, slot3 });

        // Аудитория занята в slot2 расписанием другого института — окно убрать нельзя.
        int moved = ScheduleCompactor.Compact(
            new[] { early, late }, data,
            new[] { new OccupiedSlot(room.Id, slot2.Id) }, Array.Empty<OccupiedTeacherSlot>());

        Assert.Equal(0, moved);
        Assert.Equal(slot3.Id, late.TimeSlotId);
    }

    [Fact]
    public void Compact_DoesNotMoveDoubleLessons()
    {
        var slot1 = Slot(1);
        var slot2 = Slot(2);
        var slot3 = Slot(3);
        var room = Room(BuildingX);

        var teacher = Guid.NewGuid();
        var group = Guid.NewGuid();
        var single = Curriculum(teacher, group, Guid.NewGuid());
        var dbl = Curriculum(teacher, group, Guid.NewGuid(), @double: true);

        var fixedFirst = Lesson(single, room, slot1);
        var doubled = Lesson(dbl, room, slot3); // двойную пару не двигаем, окно остаётся

        var data = Data(new[] { single, dbl }, new[] { room }, new[] { slot1, slot2, slot3 });

        int moved = ScheduleCompactor.Compact(
            new[] { fixedFirst, doubled }, data,
            Array.Empty<OccupiedSlot>(), Array.Empty<OccupiedTeacherSlot>());

        Assert.Equal(0, moved);
        Assert.Equal(slot3.Id, doubled.TimeSlotId);
    }

    // --- Фабрики данных ---

    private static TimeSlot Slot(int number) => TimeSlot.Create(Guid.NewGuid(), Day, number);

    private static Classroom Room(Guid building) =>
        Classroom.Create(Guid.NewGuid(), $"r-{Guid.NewGuid():N}", 100, building);

    private static Curriculum Curriculum(Guid teacherId, Guid groupId, Guid streamId, bool @double = false)
    {
        var group = DomainFactory.New<Group>()
            .Set(nameof(Group.Id), groupId)
            .Set(nameof(Group.Shift), Shift.Unspecified);
        var streamGroup = DomainFactory.New<StreamGroups>()
            .Set(nameof(StreamGroups.GroupId), groupId)
            .Set(nameof(StreamGroups.Group), group);
        var stream = DomainFactory.New<AcademicStream>()
            .Set(nameof(AcademicStream.Id), streamId)
            .Set(nameof(AcademicStream.StudentsCount), 1)
            .Set(nameof(AcademicStream.StreamGroups), new List<StreamGroups> { streamGroup });
        var teacher = DomainFactory.New<Teacher>().Set(nameof(Teacher.Id), teacherId);
        return DomainFactory.New<Curriculum>()
            .Set(nameof(Domain.workload.Curriculum.Id), Guid.NewGuid())
            .Set(nameof(Domain.workload.Curriculum.Teacher), teacher)
            .Set(nameof(Domain.workload.Curriculum.TeacherId), teacherId)
            .Set(nameof(Domain.workload.Curriculum.Stream), stream)
            .Set(nameof(Domain.workload.Curriculum.StreamId), streamId)
            .Set(nameof(Domain.workload.Curriculum.Double), @double);
    }

    private static Lesson Lesson(Curriculum cur, Classroom room, TimeSlot slot) =>
        Domain.schedule.Lesson.Create(Guid.NewGuid(), room.Id, slot.Id, cur.StreamId, Sem, cur.Id);

    private static ScheduleData Data(
        IReadOnlyList<Curriculum> curricula, IReadOnlyList<Classroom> rooms, IReadOnlyList<TimeSlot> slots)
    {
        var workloads = curricula.Select(c => DomainFactory.New<SemesterWorkload>()
            .Set(nameof(SemesterWorkload.Id), Guid.NewGuid())
            .Set(nameof(SemesterWorkload.Hours), 2)
            .Set(nameof(SemesterWorkload.Curriculum), c)
            .Set(nameof(SemesterWorkload.CurriculumId), c.Id)).ToList();

        return new ScheduleData(workloads, rooms, slots, Array.Empty<ConstraintConfig>());
    }
}
