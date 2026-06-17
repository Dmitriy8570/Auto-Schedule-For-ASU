using Application.Solver.Builder;
using Application.Solver.Model;
using Domain.university.groups;
using Google.OrTools.Sat;
using Schedule.DevSolver.Tests.Data;
using Schedule.DevSolver.Tests.Output;
using Xunit;
using Xunit.Abstractions;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Большой интеграционный прогон dev-солвера на масштабе ~50 преподавателей и ~100 групп.
/// Конвейер: сгенерировать входные файлы → загрузить их в сущности → построить модель → решить →
/// проверить жёсткие ограничения → выгрузить читаемое расписание (исходные данные + папки
/// преподавателей, групп и аудиторий).
/// </summary>
public class SemesterScheduleGenerationTests
{
    private readonly ITestOutputHelper _output;

    public SemesterScheduleGenerationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void GenerateSemesterSchedule_Dev_ProducesValidSchedule()
    {
        // 1. Генерируем входные файлы (преподаватели, группы, аудитории).
        var inputDir = Path.Combine(OutputDir(), "schedule-dev-input");
        DataFileGenerator.Generate(inputDir);

        // 2. Загружаем файлы и достраиваем нагрузки.
        var ds = UniversityDataLoader.Load(inputDir);
        var data = ds.ToScheduleData();

        var weekly = ds.WeeklyPairsPerGroup();
        int minPairs = ds.Groups.Where(g => g.ParentGroup is null).Min(g => weekly.GetValueOrDefault(g.Id));
        _output.WriteLine($"Преподавателей: {ds.Teachers.Count}, групп: {ds.Groups.Count}, " +
                          $"аудиторий: {ds.Classrooms.Count}, нагрузок: {ds.Workloads.Count}, слотов: {ds.TimeSlots.Count}");
        _output.WriteLine($"Минимум пар в неделю у основной группы: {minPairs}");

        Assert.True(minPairs >= UniversityDataLoader.MinWeeklyPairs,
            $"У некоторой группы меньше {UniversityDataLoader.MinWeeklyPairs} пар в неделю: {minPairs}");

        // 3. Строим и решаем модель dev-солвера.
        var model = ScheduleModelDirector.CreateDefault().Build(data);

        var solver = new CpSolver
        {
            StringParameters = "max_time_in_seconds:180;num_search_workers:8;log_search_progress:false"
        };
        var status = solver.Solve(model.Model);

        _output.WriteLine($"Статус: {status}; цель: {solver.ObjectiveValue}; время: {solver.WallTime():0.00} c");

        Assert.True(status is CpSolverStatus.Optimal or CpSolverStatus.Feasible,
            $"Солвер не нашёл решения, статус: {status}");

        // 4. Извлекаем и проверяем решение.
        var assignments = Extract(model, solver);
        ValidateHardConstraints(ds, model, assignments);

        // 5. Выгружаем файлы расписания.
        long variables = SafeVariableCount(model);
        var stats = new SolveStats(
            status.ToString(), solver.ObjectiveValue, solver.WallTime(), variables, solver.NumBranches());

        var root = new ScheduleWriter(ds, assignments, stats, inputDir).Write();
        _output.WriteLine($"Файлы расписания: {root}");

        Assert.NotEmpty(assignments);
    }

    private static List<Assignment> Extract(ScheduleModel model, CpSolver solver)
    {
        var result = new List<Assignment>();
        for (int w = 0; w < model.WorkloadCount; w++)
            for (int r = 0; r < model.ClassroomCount; r++)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    if (solver.Value(model.Lessons[w, r, t]) > 0.5)
                        result.Add(new Assignment(w, r, t));
        return result;
    }

    /// <summary>Перепроверяет ключевые жёсткие ограничения на извлечённом решении.</summary>
    private static void ValidateHardConstraints(ScheduleDataset ds, ScheduleModel model, List<Assignment> assignments)
    {
        // 1. Плановое число пар по каждой нагрузке.
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            int expected = ds.Workloads[w].Hours / 2;
            int actual = assignments.Count(a => a.Workload == w);
            Assert.True(expected == actual,
                $"Нагрузка #{w} ({ds.Workloads[w].Curriculum.Subject.Name}): ожидалось {expected}, получено {actual}");
        }

        // 2. В одной аудитории в одном слоте — не более одного занятия.
        Assert.All(assignments.GroupBy(a => (a.Room, a.Slot)),
            g => Assert.True(g.Count() == 1, $"Конфликт аудитории: слот {g.Key.Slot}, аудитория {g.Key.Room}"));

        // 3. Преподаватель не ведёт два занятия одновременно.
        var teacherClashes = assignments
            .GroupBy(a => (Teacher: ds.Workloads[a.Workload].Curriculum.Teacher.Id, a.Slot))
            .Where(g => g.Count() > 1);
        Assert.Empty(teacherClashes);

        // 4. Группа не находится на двух занятиях одновременно.
        var groupClashes = assignments
            .SelectMany(a => ds.Workloads[a.Workload].Curriculum.Stream.StreamGroups
                .Select(sg => (Group: sg.Group.Id, a.Slot)))
            .GroupBy(x => x)
            .Where(g => g.Count() > 1);
        Assert.Empty(groupClashes);

        // 5. Соответствие смене (первая — пары 1–4, вторая — 5–6, вечерняя — 7+).
        foreach (var a in assignments)
        {
            int number = ds.TimeSlots[a.Slot].Number;
            foreach (var sg in ds.Workloads[a.Workload].Curriculum.Stream.StreamGroups)
            {
                bool ok = sg.Group.Shift switch
                {
                    Shift.First => number <= 4,
                    Shift.Second => number is >= 5 and <= 6,
                    Shift.Evening => number >= 7,
                    _ => true
                };
                Assert.True(ok, $"Нарушение смены: группа {sg.Group.Name} ({sg.Group.Shift}) в паре {number}");
            }
        }

        // 6. Оборудование аудитории удовлетворяет требованиям нагрузки.
        foreach (var a in assignments)
        {
            var needed = ds.Workloads[a.Workload].Curriculum.NeededEquipments.Select(n => n.EquipmentId).ToHashSet();
            var available = ds.Classrooms[a.Room].EquipmentRooms.Select(e => e.EquipmentId).ToHashSet();
            Assert.True(needed.IsSubsetOf(available),
                $"Аудитория {ds.Classrooms[a.Room].Name} не оснащена для {ds.Workloads[a.Workload].Curriculum.Subject.Name}");
        }
    }

    private static long SafeVariableCount(ScheduleModel model)
    {
        try { return model.Model.Model.Variables.Count; }
        catch { return 0; }
    }

    /// <summary>Каталог TestOutput рядом с файлом решения backend-auto-schedule.slnx.</summary>
    private static string OutputDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend-auto-schedule.slnx")))
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? AppContext.BaseDirectory, "TestOutput");
    }
}
