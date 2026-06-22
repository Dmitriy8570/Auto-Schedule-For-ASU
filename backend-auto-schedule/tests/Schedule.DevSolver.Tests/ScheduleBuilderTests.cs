using Application.Solver.Builder;
using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Builder.BuildSections;
using Application.Solver.Model;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.groups;
using Domain.university.teachers;
using Domain.workload;
using Google.OrTools.Sat;
using Schedule.DevSolver.Tests.Reflection;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Модульные тесты ключевых секционных строителей жёстких ограничений на крошечных
/// детерминированных моделях: число пар по нагрузке (TotalHours), запрет несовместимых
/// аудиторий по вместимости (Capacity), запрет пересечений в одной аудитории (Intersection).
/// </summary>
public sealed class ScheduleBuilderTests
{
    private static readonly Guid Sem = Guid.NewGuid();
    private static readonly Guid Bld = Guid.NewGuid();

    private static SemesterWorkload Workload(int hours) =>
        SemesterWorkload.Create(Guid.NewGuid(), hours, Guid.NewGuid(), Sem);

    private static Classroom Room(int capacity) =>
        Classroom.Create(Guid.NewGuid(), $"r{capacity}-{Guid.NewGuid():N}", capacity, Bld);

    private static TimeSlot Slot(int number) =>
        TimeSlot.Create(Guid.NewGuid(), Guid.NewGuid(), number);

    /// <summary>Нагрузка только со скалярным TeacherId плана — для OccupiedResources (по преподавателю).</summary>
    private static SemesterWorkload WorkloadWithTeacher(int hours, Guid teacherId)
    {
        var curriculum = DomainFactory.New<Curriculum>().Set(nameof(Curriculum.TeacherId), teacherId);
        return DomainFactory.New<SemesterWorkload>()
            .Set(nameof(SemesterWorkload.Id), Guid.NewGuid())
            .Set(nameof(SemesterWorkload.Hours), hours)
            .Set(nameof(SemesterWorkload.Curriculum), curriculum);
    }

    /// <summary>
    /// Нагрузка с мини-графом навигаций (свой преподаватель, поток с одной группой) — нужна
    /// строителям, читающим Curriculum.Teacher / Curriculum.Stream.StreamGroups (Intersection).
    /// </summary>
    private static SemesterWorkload WorkloadWithSubject(int hours, int students = 1)
    {
        var group = DomainFactory.New<Group>().Set(nameof(Group.Id), Guid.NewGuid());
        var streamGroup = DomainFactory.New<StreamGroups>().Set(nameof(StreamGroups.Group), group);
        var stream = DomainFactory.New<AcademicStream>()
            .Set(nameof(AcademicStream.Id), Guid.NewGuid())
            .Set(nameof(AcademicStream.StudentsCount), students)
            .Set(nameof(AcademicStream.StreamGroups), new List<StreamGroups> { streamGroup });
        var teacher = DomainFactory.New<Teacher>().Set(nameof(Teacher.Id), Guid.NewGuid());
        var curriculum = DomainFactory.New<Curriculum>()
            .Set(nameof(Curriculum.Teacher), teacher)
            .Set(nameof(Curriculum.Stream), stream);
        return DomainFactory.New<SemesterWorkload>()
            .Set(nameof(SemesterWorkload.Id), Guid.NewGuid())
            .Set(nameof(SemesterWorkload.Hours), hours)
            .Set(nameof(SemesterWorkload.Curriculum), curriculum);
    }

    /// <summary>
    /// Нагрузка с заданными идентификаторами преподавателя и группы (экземпляры сущностей —
    /// каждый раз новые, как при AsNoTracking без identity resolution). Нужна для проверки, что
    /// эксклюзивность считается по Id, а не по ссылке на объект.
    /// </summary>
    private static SemesterWorkload WorkloadFor(int hours, Guid teacherId, Guid groupId)
    {
        var group = DomainFactory.New<Group>().Set(nameof(Group.Id), groupId);
        var streamGroup = DomainFactory.New<StreamGroups>()
            .Set(nameof(StreamGroups.GroupId), groupId)
            .Set(nameof(StreamGroups.Group), group);
        var stream = DomainFactory.New<AcademicStream>()
            .Set(nameof(AcademicStream.Id), Guid.NewGuid())
            .Set(nameof(AcademicStream.StudentsCount), 1)
            .Set(nameof(AcademicStream.StreamGroups), new List<StreamGroups> { streamGroup });
        var teacher = DomainFactory.New<Teacher>().Set(nameof(Teacher.Id), teacherId);
        var curriculum = DomainFactory.New<Curriculum>()
            .Set(nameof(Curriculum.Teacher), teacher)
            .Set(nameof(Curriculum.TeacherId), teacherId)
            .Set(nameof(Curriculum.Stream), stream)
            .Set(nameof(Curriculum.StreamId), stream.Id);
        return DomainFactory.New<SemesterWorkload>()
            .Set(nameof(SemesterWorkload.Id), Guid.NewGuid())
            .Set(nameof(SemesterWorkload.Hours), hours)
            .Set(nameof(SemesterWorkload.Curriculum), curriculum);
    }

    /// <summary>Сдвоенная нагрузка (Curriculum.Double = true) — для DoubleLessonSectionBuilder.</summary>
    private static SemesterWorkload WorkloadDouble(int hours)
    {
        var curriculum = DomainFactory.New<Curriculum>().Set(nameof(Curriculum.Double), true);
        return DomainFactory.New<SemesterWorkload>()
            .Set(nameof(SemesterWorkload.Id), Guid.NewGuid())
            .Set(nameof(SemesterWorkload.Hours), hours)
            .Set(nameof(SemesterWorkload.Curriculum), curriculum);
    }

    private static ScheduleData Data(
        IReadOnlyList<SemesterWorkload> workloads,
        IReadOnlyList<Classroom> classrooms,
        IReadOnlyList<TimeSlot> timeSlots) =>
        new(workloads, classrooms, timeSlots, Array.Empty<ConstraintConfig>());

    private static (CpSolverStatus status, CpSolver solver, ScheduleModel model) Solve(
        ScheduleData data, params IModelSectionBuilder[] builders)
    {
        var model = new ScheduleModelDirector(builders).Build(data);
        var solver = new CpSolver();
        var status = solver.Solve(model.Model);
        return (status, solver, model);
    }

    private static bool IsSolved(CpSolverStatus s) =>
        s is CpSolverStatus.Optimal or CpSolverStatus.Feasible;

    private static int Assigned(CpSolver solver, ScheduleModel model, int w, int? room = null)
    {
        int count = 0;
        for (int r = 0; r < model.ClassroomCount; r++)
        {
            if (room is { } only && r != only) continue;
            for (int t = 0; t < model.TimeSlotCount; t++)
                // Отсечённые прунингом ячейки (null) — не назначены.
                if (model.Lessons[w, r, t] is { } var && solver.Value(var) != 0) count++;
        }
        return count;
    }

    // --- TotalHours: суммарное число пар по нагрузке == Hours / 2 ---

    [Fact]
    public void TotalHours_AssignsExactlyHalfHoursAsPairs()
    {
        var data = Data(
            new[] { Workload(hours: 6) },                       // 3 пары
            new[] { Room(50) },
            new[] { Slot(1), Slot(2), Slot(3), Slot(4) });

        var (status, solver, model) = Solve(data, new VariablesSectionBuilder(), new TotalHoursSectionBuilder());

        Assert.True(IsSolved(status));
        Assert.Equal(3, Assigned(solver, model, w: 0));
    }

    [Fact]
    public void TotalHours_OddHours_ThrowsDuringBuild()
    {
        var data = Data(new[] { Workload(hours: 3) }, new[] { Room(50) }, new[] { Slot(1), Slot(2) });
        var director = new ScheduleModelDirector(new IModelSectionBuilder[]
            { new VariablesSectionBuilder(), new TotalHoursSectionBuilder() });

        Assert.Throws<InvalidOperationException>(() => director.Build(data));
    }

    [Fact]
    public void TotalHours_Infeasible_WhenFewerCellsThanRequiredPairs()
    {
        // Требуется 2 пары, но всего одна ячейка (1 аудитория × 1 слот).
        var data = Data(new[] { Workload(hours: 4) }, new[] { Room(50) }, new[] { Slot(1) });

        var (status, _, _) = Solve(data, new VariablesSectionBuilder(), new TotalHoursSectionBuilder());

        Assert.False(IsSolved(status));
    }

    // --- Capacity: аудитория меньше потока запрещена ---

    [Fact]
    public void Capacity_PlacesWorkloadOnlyInRoomThatFitsTheStream()
    {
        var stream = AcademicStream.Create(Guid.NewGuid(), studentsCount: 30);
        var curriculum = DomainFactory.New<Curriculum>().Set(nameof(Curriculum.Stream), stream);
        var workload = DomainFactory.New<SemesterWorkload>()
            .Set(nameof(SemesterWorkload.Id), Guid.NewGuid())
            .Set(nameof(SemesterWorkload.Hours), 2)            // 1 пара
            .Set(nameof(SemesterWorkload.Curriculum), curriculum);

        var small = Room(20); // не вмещает 30 (индекс 0)
        var big = Room(40);   // вмещает (индекс 1)
        var data = Data(new[] { workload }, new[] { small, big }, new[] { Slot(1), Slot(2) });

        var (status, solver, model) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(), new CapacitySectionBuilder());

        Assert.True(IsSolved(status));
        Assert.Equal(0, Assigned(solver, model, w: 0, room: 0)); // маленькая — пустая
        Assert.Equal(1, Assigned(solver, model, w: 0, room: 1)); // большая — одна пара
    }

    // --- Intersection: в одной аудитории в один слот — не более одного занятия ---

    [Fact]
    public void Intersection_TwoWorkloads_CannotShareSameRoomAndSlot()
    {
        // Две независимые нагрузки (без общего преподавателя/группы), одна аудитория, один слот.
        // Каждая требует 1 пару, но ячейка одна -> разместить обе нельзя -> Infeasible.
        var data = Data(
            new[] { WorkloadWithSubject(hours: 2), WorkloadWithSubject(hours: 2) },
            new[] { Room(50) },
            new[] { Slot(1) });

        var (status, _, _) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(), new IntersectionSectionBuilder());

        Assert.False(IsSolved(status));
    }

    [Fact]
    public void Intersection_TwoWorkloads_FitWhenSeparateSlotsAvailable()
    {
        // Та же пара нагрузок, но два слота — каждая занимает свой слот -> решаемо.
        var data = Data(
            new[] { WorkloadWithSubject(hours: 2), WorkloadWithSubject(hours: 2) },
            new[] { Room(50) },
            new[] { Slot(1), Slot(2) });

        var (status, solver, model) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(), new IntersectionSectionBuilder());

        Assert.True(IsSolved(status));
        Assert.Equal(1, Assigned(solver, model, w: 0));
        Assert.Equal(1, Assigned(solver, model, w: 1));
    }

    [Fact]
    public void Intersection_SameTeacherDifferentInstances_CannotShareSlot()
    {
        // Две нагрузки одного преподавателя (разные экземпляры с одним Id — как при AsNoTracking),
        // две аудитории, один слот: преподаватель не может вести оба занятия одновременно -> Infeasible.
        // Регрессия: раньше эксклюзивность группировалась по ссылке и такие занятия не блокировались.
        var teacherId = Guid.NewGuid();
        var data = Data(
            new[] { WorkloadFor(2, teacherId, Guid.NewGuid()), WorkloadFor(2, teacherId, Guid.NewGuid()) },
            new[] { Room(50), Room(50) },
            new[] { Slot(1) });

        var (status, _, _) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(), new IntersectionSectionBuilder());

        Assert.False(IsSolved(status));
    }

    [Fact]
    public void Intersection_SameGroupDifferentInstances_CannotShareSlot()
    {
        // Аналогично для группы: одна группа в двух разных потоках (разные экземпляры с одним Id),
        // две аудитории, один слот -> группа не может быть на двух занятиях сразу -> Infeasible.
        var groupId = Guid.NewGuid();
        var data = Data(
            new[] { WorkloadFor(2, Guid.NewGuid(), groupId), WorkloadFor(2, Guid.NewGuid(), groupId) },
            new[] { Room(50), Room(50) },
            new[] { Slot(1) });

        var (status, _, _) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(), new IntersectionSectionBuilder());

        Assert.False(IsSolved(status));
    }

    // --- DoubleLesson: сдвоенные пары идут двумя смежными слотами в одной аудитории ---

    [Fact]
    public void DoubleLesson_PlacesTwoAdjacentSlotsInSameRoom()
    {
        // Сдвоенная нагрузка на 2 пары обязана встать одним блоком из двух смежных слотов.
        var data = Data(
            new[] { WorkloadDouble(hours: 4) },
            new[] { Room(50) },
            new[] { Slot(1), Slot(2), Slot(3), Slot(4) });

        var (status, solver, model) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(), new DoubleLessonSectionBuilder());

        Assert.True(IsSolved(status));

        var slots = new List<int>();
        for (int t = 0; t < model.TimeSlotCount; t++)
            if (model.Lessons[0, 0, t] is { } v && solver.Value(v) != 0) slots.Add(t);

        Assert.Equal(2, slots.Count);
        Assert.Equal(1, slots[1] - slots[0]); // ровно два соседних слота — сдвоенная пара
    }

    [Fact]
    public void DoubleLesson_OddNumberOfPairs_IsInfeasible()
    {
        // Одиночная пара не образует сдвоенный блок: для Double число пар должно быть чётным.
        var data = Data(
            new[] { WorkloadDouble(hours: 2) },                 // всего 1 пара
            new[] { Room(50) },
            new[] { Slot(1), Slot(2), Slot(3), Slot(4) });

        var (status, _, _) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(), new DoubleLessonSectionBuilder());

        Assert.False(IsSolved(status));
    }

    // --- OccupiedResources (декомпозиция B): ресурсы, занятые другими институтами ---

    [Fact]
    public void OccupiedResources_ForbidsBusyClassroomSlot()
    {
        var room = Room(50);
        var busy = Slot(1);  // индекс слота 0 — занят
        var free = Slot(2);  // индекс слота 1 — свободен
        var data = new ScheduleData(
            new[] { Workload(hours: 2) },                  // 1 пара
            new[] { room },
            new[] { busy, free },
            Array.Empty<ConstraintConfig>(),
            OccupiedClassroomSlots: new[] { new OccupiedSlot(room.Id, busy.Id) });

        var (status, solver, model) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(), new OccupiedResourcesSectionBuilder());

        Assert.True(IsSolved(status));
        Assert.Equal(0L, solver.Value(model.Lessons[0, 0, 0])); // занятая (аудитория, слот) запрещена
        Assert.Equal(1, Assigned(solver, model, w: 0));         // пара ушла в свободный слот
    }

    [Fact]
    public void OccupiedResources_ForbidsBusyTeacherSlot()
    {
        var teacher = Guid.NewGuid();
        var busy = Slot(1);  // индекс слота 0 — преподаватель занят
        var free = Slot(2);
        var data = new ScheduleData(
            new[] { WorkloadWithTeacher(hours: 2, teacherId: teacher) },
            new[] { Room(50) },
            new[] { busy, free },
            Array.Empty<ConstraintConfig>(),
            OccupiedTeacherSlots: new[] { new OccupiedTeacherSlot(teacher, busy.Id) });

        var (status, solver, model) = Solve(data,
            new VariablesSectionBuilder(), new TotalHoursSectionBuilder(), new OccupiedResourcesSectionBuilder());

        Assert.True(IsSolved(status));
        Assert.Equal(0L, solver.Value(model.Lessons[0, 0, 0])); // преподаватель занят в этом слоте
        Assert.Equal(1, Assigned(solver, model, w: 0));
    }
}
