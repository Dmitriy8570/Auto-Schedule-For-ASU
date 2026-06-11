using Application.solver.builder.buildSections;
using Application.solver.model;
using Domain.calendar;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.groups;
using Domain.university.teachers;
using Domain.workload;
using Google.OrTools.Sat;
using Xunit;
using Xunit.Abstractions;

namespace Tests;

/// <summary>
/// Запустить с флагом для отображения расписания:
///   dotnet test Tests/Tests.csproj --logger "console;verbosity=detailed"
/// </summary>
public class ScheduleVisualizationTests(ITestOutputHelper output)
{
    [Fact]
    public void Solver_BuildsAndPrintsWeeklyTimetable()
    {
        // ── Предметы ──────────────────────────────────────────────────────
        var mathAnalysis  = MakeSubject("Мат. анализ");
        var prog          = MakeSubject("Программирование");
        var physics       = MakeSubject("Физика");
        var history       = MakeSubject("История");
        var linAlgebra    = MakeSubject("Лин. алгебра");
        var algorithms    = MakeSubject("Алгоритмы");
        var databases     = MakeSubject("Базы данных");
        var philosophy    = MakeSubject("Философия");

        // ── Преподаватели ─────────────────────────────────────────────────
        var ivanov  = MakeTeacher("Иванов И.И.");
        var petrov  = MakeTeacher("Петров П.П.");
        var sidorov = MakeTeacher("Сидоров С.С.");
        var kozlov  = MakeTeacher("Козлов К.К.");
        var morozov = MakeTeacher("Морозов М.М.");
        var nikitin = MakeTeacher("Никитин Н.Н.");
        var orlova  = MakeTeacher("Орлова О.О.");
        var zaytsev = MakeTeacher("Зайцев З.З.");

        // ── Потоки со группами ────────────────────────────────────────────
        var (st1, _) = MakeStreamWithGroup("4.205-1");
        var (st2, _) = MakeStreamWithGroup("4.205-2");
        var (st3, _) = MakeStreamWithGroup("4.201-1");
        var (st4, _) = MakeStreamWithGroup("4.201-2");

        // ── Нагрузки: hours/2 = занятий ──────────────────────────────────
        // Два предмета на одну и ту же группу → IntersectionSectionBuilder
        // запрещает им пересекаться по слотам (как в реальном расписании).
        var w1 = MakeWorkload(ivanov,  st1, mathAnalysis, LessonType.Lecture,    hours: 10); // 5 зан.
        var w2 = MakeWorkload(petrov,  st2, prog,         LessonType.Seminar,    hours:  8); // 4 зан.
        var w3 = MakeWorkload(sidorov, st3, physics,      LessonType.Laboratory, hours:  8); // 4 зан.
        var w4 = MakeWorkload(kozlov,  st4, history,      LessonType.Lecture,    hours:  6); // 3 зан.
        var w5 = MakeWorkload(morozov, st1, linAlgebra,   LessonType.Lecture,    hours:  8); // 4 зан.
        var w6 = MakeWorkload(nikitin, st2, algorithms,   LessonType.Seminar,    hours:  6); // 3 зан.
        var w7 = MakeWorkload(orlova,  st3, databases,    LessonType.Laboratory, hours:  8); // 4 зан.
        var w8 = MakeWorkload(zaytsev, st4, philosophy,   LessonType.Lecture,    hours:  6); // 3 зан.
        // Итого: 5+4+4+3+4+3+4+3 = 30 занятий

        // ── Аудитории ─────────────────────────────────────────────────────
        var classrooms = new List<Classroom>
        {
            MakeClassroom("101", capacity: 40),
            MakeClassroom("202", capacity: 30),
            MakeClassroom("305", capacity: 25),
        };

        // ── Слоты: Пн–Пт, 8 пар в день ──────────────────────────────────
        var (weekDays, slots) = BuildWeekSlots(daysCount: 5, pairsPerDay: 8);

        // ── Штрафы за окна ────────────────────────────────────────────────
        var penalties = new List<ConstraintConfig>
        {
            new() { Id = Guid.NewGuid(), ConstraintType = ConstraintType.StudentGap, Penalty = 100 },
            new() { Id = Guid.NewGuid(), ConstraintType = ConstraintType.TeacherGap, Penalty = 100 },
        };

        // ── Сборка модели ─────────────────────────────────────────────────
        var workloads = new List<SemesterWorkload> { w1, w2, w3, w4, w5, w6, w7, w8 };
        var data  = new ScheduleData(workloads, classrooms, slots, penalties);
        var model = new ScheduleModel(data);

        new VariablesSectionBuilder().Build(model);
        new TotalHoursConstraintSectionBuilder().Build(model);
        new IntersectionSectionBuilder().Build(model);
        new WindowSectionBuilder().Build(model);

        if (model.Expr.Count > 0)
            model.Model.Minimize(LinearExpr.Sum(model.Expr));

        // ── Решение ───────────────────────────────────────────────────────
        var solver = new CpSolver();
        solver.StringParameters = "max_time_in_seconds:15.0";
        var status = solver.Solve(model.Model);

        Assert.True(
            status is CpSolverStatus.Optimal or CpSolverStatus.Feasible,
            $"Солвер не нашёл допустимого решения: {status}");

        PrintTimetable(solver, model, workloads, classrooms, slots, weekDays);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Фабричные методы

    private static Subject MakeSubject(string name) => new()
        { Id = Guid.NewGuid(), Name = name, Curriculums = [] };

    private static Teacher MakeTeacher(string name) => new()
        { Id = Guid.NewGuid(), Name = name, Curriculums = [], TeacherAvailabilities = [] };

    private static (AcademicStream stream, Group group) MakeStreamWithGroup(string groupName)
    {
        var stream = new AcademicStream { Id = Guid.NewGuid(), StreamGroups = [], Curriculums = [], Lessons = [] };
        var group  = new Group { Id = Guid.NewGuid(), Name = groupName, StudentCount = 25, Groups = [], StreamGroups = [] };
        var link   = new StreamGroups { GroupId = group.Id, Group = group, StreamId = stream.Id, Stream = stream };
        stream.StreamGroups.Add(link);
        group.StreamGroups.Add(link);
        return (stream, group);
    }

    private static Classroom MakeClassroom(string name, int capacity) => new()
        { Id = Guid.NewGuid(), Name = name, Capacity = capacity, Lessons = [], ClassroomAvailabilities = [], EquipmentRooms = [] };

    private static SemesterWorkload MakeWorkload(
        Teacher teacher, AcademicStream stream, Subject subject, LessonType type, int hours)
    {
        var c = new Curriculum
        {
            Id = Guid.NewGuid(),
            Teacher = teacher, TeacherId = teacher.Id,
            Stream  = stream,  StreamId  = stream.Id,
            Subject = subject, SubjectId = subject.Id,
            LessonType = type,
            WeekWorkloads = [], SemesterWorkloads = [], NeededEquipments = []
        };
        return new SemesterWorkload
        {
            Id = Guid.NewGuid(), Hours = hours,
            Curriculum = c, CurriculumId = c.Id,
            SemesterLogs = [], WeekWorkloads = []
        };
    }

    private static (List<WeekDay> days, List<TimeSlot> slots) BuildWeekSlots(int daysCount, int pairsPerDay)
    {
        WeekDayType[] order =
        [
            WeekDayType.Monday,
            WeekDayType.Tuesday,
            WeekDayType.Wednesday,
            WeekDayType.Thursday,
            WeekDayType.Friday,
        ];

        var days  = new List<WeekDay>();
        var slots = new List<TimeSlot>();

        for (int d = 0; d < daysCount; d++)
        {
            var day = new WeekDay { Id = Guid.NewGuid(), DayOfWeek = order[d], TimeSlots = [] };
            days.Add(day);
            for (int n = 1; n <= pairsPerDay; n++)
                slots.Add(new TimeSlot { Id = Guid.NewGuid(), WeekDay = day, WeekDayId = day.Id, Number = n });
        }

        return (days, slots);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Вывод расписания

    private void PrintTimetable(
        CpSolver solver,
        ScheduleModel model,
        List<SemesterWorkload> workloads,
        List<Classroom> classrooms,
        List<TimeSlot> slots,
        List<WeekDay> weekDays)
    {
        var entries = new List<(int dayIdx, int pair, string teacher, string subject, string group, string type, string room)>();

        for (int w = 0; w < workloads.Count; w++)
            for (int r = 0; r < classrooms.Count; r++)
                for (int s = 0; s < slots.Count; s++)
                {
                    if (!solver.BooleanValue(model.Lessons[w, r, s])) continue;
                    var cur = workloads[w].Curriculum;
                    var grp = string.Join(", ", cur.Stream.StreamGroups.Select(sg => sg.Group.Name));
                    entries.Add((
                        weekDays.IndexOf(slots[s].WeekDay),
                        slots[s].Number,
                        cur.Teacher.Name,
                        cur.Subject?.Name ?? "—",
                        grp,
                        TypeLabel(cur.LessonType),
                        classrooms[r].Name
                    ));
                }

        const string border = "══════════════════════════════════════════════════════════════════════════════════════";
        const string sep    = "  ──────────────────────────────────────────────────────────────────────────────────";
        const string rowFmt = "  │ Пара {0} {1,-11} │ {2,-14} │ {3,-8} │ {4,-18} │ {5,-11} │ ауд. {6,-4} │";
        const string hdr    = "  │ Пара  Время       │ Преподаватель  │ Группа   │ Дисциплина         │ Тип         │ Ауд.      │";

        output.WriteLine("");
        output.WriteLine(border);
        output.WriteLine("                         РАСПИСАНИЕ ЗАНЯТИЙ");
        output.WriteLine(border);

        foreach (var dayGroup in entries.GroupBy(e => e.dayIdx).OrderBy(g => g.Key))
        {
            output.WriteLine("");
            output.WriteLine($"  {DayName(weekDays[dayGroup.Key].DayOfWeek).ToUpper()}");
            output.WriteLine(sep);
            output.WriteLine(hdr);
            output.WriteLine(sep);
            foreach (var (_, pair, teacher, subject, group, type, room) in dayGroup.OrderBy(e => e.pair))
                output.WriteLine(string.Format(rowFmt, pair, SlotTime(pair), teacher, group, subject, type, room));
            output.WriteLine(sep);
        }

        output.WriteLine("");
        output.WriteLine(border);
        int expected = workloads.Sum(w => w.Hours / 2);
        output.WriteLine($"  Итого: {entries.Count}/{expected} занятий  │  {workloads.Count} преподавателей  │  {classrooms.Count} аудитории  │  {weekDays.Count} дней");
        output.WriteLine(border);
        output.WriteLine("");
    }

    private static string DayName(WeekDayType d) => d switch
    {
        WeekDayType.Monday    => "Понедельник",
        WeekDayType.Tuesday   => "Вторник",
        WeekDayType.Wednesday => "Среда",
        WeekDayType.Thursday  => "Четверг",
        WeekDayType.Friday    => "Пятница",
        _                     => d.ToString()
    };

    private static string SlotTime(int n) => n switch
    {
        1 => "08:00–09:30",
        2 => "09:40–11:10",
        3 => "11:20–12:50",
        4 => "13:20–14:50",
        5 => "15:00–16:30",
        6 => "16:40–18:10",
        7 => "18:20–19:50",
        8 => "20:00–21:30",
        _ => $"Пара {n}"
    };

    private static string TypeLabel(LessonType t) => t switch
    {
        LessonType.Lecture    => "Лекция",
        LessonType.Seminar    => "Семинар",
        LessonType.Laboratory => "Лаб. работа",
        _                     => t.ToString()
    };
}
