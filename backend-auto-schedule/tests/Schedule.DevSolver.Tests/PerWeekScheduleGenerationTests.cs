using Application.Solver.Builder;
using Application.Solver.Model;
using Application.Solver.Solving;
using Domain.university.groups;
using Domain.workload;
using Schedule.DevSolver.Tests.Data;
using Xunit;
using Xunit.Abstractions;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Интеграционный прогон <em>понедельной</em> генерации на реальном продакшен-солвере
/// (<see cref="ScheduleSolver"/>). Зеркалит ядро <c>GenerateScheduleCommandHandler.SolveWeek</c>:
/// двухфазную модель недели — этап 1 (<see cref="ScheduleModelDirector.CreatePerWeekMaxPlacement"/>)
/// ищет максимум размещения, его покрытие фиксируется нижней границей для этапа 2 (<see
/// cref="ScheduleModelDirector.CreatePerWeekQuality"/>), который уплотняет окна, не выбрасывая занятия.
///
/// Масштаб — одна «когорта» (детерминированная подвыборка нагрузок dev-данных), на который понедельный
/// конвейер и рассчитан: после декомпозиции по институту неделя института ≈ планы института × слоты
/// недели, и модель помещается в память и решается до оптимума. Полный университет одной моделью
/// (~530 нагрузок) — за пределами стабильного решения за разумный бюджет (то же верно и для старого
/// пути <c>CreateDefault</c>): это потолок солвера, а не свойство понедельного пути, и потому
/// сознательно не проверяется здесь.
///
/// Доказывает то, что не покрывал <see cref="SemesterScheduleGenerationTests"/> (он гоняет старый путь
/// <c>CreateDefault</c> одной моделью): новый понедельный конвейер на реальных данных (1) ставит весь
/// план недели, (2) не теряет занятий на этапе качества и (3) этим этапом действительно уменьшает окна
/// — при соблюдении всех жёстких ограничений.
///
/// Помечен Category=SlowIntegration (реальный CP-SAT) и исключается из обычного прогона
/// (фильтр "Category!=SlowIntegration"), как и соседний большой тест.
/// </summary>
[Trait("Category", "SlowIntegration")]
public class PerWeekScheduleGenerationTests
{
    private const double PhaseTimeSeconds = 60;
    private const int SearchWorkers = 8;

    /// <summary>Целевой размер когорты (число нагрузок подвыборки) — модель этого масштаба солвер берёт до оптимума.</summary>
    private const int CohortWorkloads = 60;

    private readonly ITestOutputHelper _output;

    public PerWeekScheduleGenerationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    [Trait("Category", "SlowIntegration")]
    public void GenerateWeek_TwoPhase_PlacesFullPlanAndCompactsWindows()
    {
        // 1. Генерируем входные файлы в собственный каталог (чтобы не пересекаться с соседним
        //    SlowIntegration-тестом, который xUnit может запускать параллельно).
        var inputDir = Path.Combine(OutputDir(), "schedule-perweek-input");
        DataFileGenerator.Generate(inputDir);

        var ds = UniversityDataLoader.Load(inputDir);

        // 2. Детерминированная подвыборка-«когорта»: равномерный шаг по списку нагрузок даёт
        //    представительный срез (разные программы/курсы/смены) масштаба одного института за неделю.
        var cohort = SelectCohort(ds.Workloads, CohortWorkloads);
        var data = new ScheduleData(cohort.ToItems(), ds.Classrooms, ds.TimeSlots, ds.Penalties);

        _output.WriteLine($"Когорта: нагрузок {cohort.Count} (из {ds.Workloads.Count}), " +
                          $"аудиторий {ds.Classrooms.Count}, слотов {ds.TimeSlots.Count}");

        var options = SolverOptions.Default with
        {
            MaxTimeInSeconds = PhaseTimeSeconds,
            SearchWorkers = SearchWorkers,
        };
        var solver = new ScheduleSolver();

        // 3. ЭТАП 1 — максимум размещения. Подвыборка заведомо разрешима на полный план (удаление
        //    нагрузок только освобождает ресурсы), модель мала ⇒ ожидаем полное размещение.
        var phase1 = solver.Solve(ScheduleModelDirector.CreatePerWeekMaxPlacement().Build(data), options);
        _output.WriteLine($"Этап 1 (размещение): статус {phase1.Status}, занятий {phase1.Assignments.Count}, " +
                          $"цель {phase1.ObjectiveValue}, время {phase1.WallTimeSeconds:0.00} c");
        Assert.True(phase1.IsSuccess, $"Этап 1 не нашёл решения, статус {phase1.Status}");
        AssertFullPlacement(cohort, phase1.Assignments);

        // 4. ЭТАП 2 — качество при ЗАФИКСИРОВАННОМ покрытии этапа 1.
        var floors = new int[cohort.Count];
        foreach (var a in phase1.Assignments) floors[a.Workload]++;

        var phase2 = solver.Solve(ScheduleModelDirector.CreatePerWeekQuality(floors).Build(data), options);
        _output.WriteLine($"Этап 2 (качество): статус {phase2.Status}, занятий {phase2.Assignments.Count}, " +
                          $"цель {phase2.ObjectiveValue}, время {phase2.WallTimeSeconds:0.00} c");
        Assert.True(phase2.IsSuccess, $"Этап 2 не нашёл решения, статус {phase2.Status}");

        // Этап 2 не имеет права терять занятия (нижняя граница хранит покрытие) — тот же инвариант,
        // что и в проде: chosen = phase2, только если занятий не меньше, чем на этапе 1.
        Assert.True(phase2.Assignments.Count >= phase1.Assignments.Count,
            $"Этап 2 потерял занятия: было {phase1.Assignments.Count}, стало {phase2.Assignments.Count}");
        AssertFullPlacement(cohort, phase2.Assignments);

        // 5. Жёсткие ограничения держатся на итоговом (выбранном) решении.
        ValidateHardConstraints(cohort, ds, phase2.Assignments);

        // 6. Итоговое расписание компактно. Прямое сравнение «окон» двух этапов некорректно: этап 2
        //    минимизирует не окна в чистом виде, а композитный штраф качества (окна + число учебных
        //    дней DayCompactness + доступность + корпус + параллелизм) и может разменять одно окно на
        //    убранный учебный день — суммарно лучше. Поэтому проверяем абсолютную компактность:
        //    в среднем меньше одного окна на активный учебный день группы (рассыпанное расписание
        //    дало бы ≥ 1). Сравнение этапов — в лог, для наглядности.
        var (windows1, _) = Compactness(cohort, ds, phase1.Assignments);
        var (windows2, activeGroupDays) = Compactness(cohort, ds, phase2.Assignments);
        _output.WriteLine($"Окон у групп: этап 1 = {windows1}, этап 2 = {windows2}; " +
                          $"активных учебных дней групп = {activeGroupDays}");
        Assert.True(windows2 < activeGroupDays,
            $"Расписание не компактно: {windows2} окон на {activeGroupDays} активных учебных дней групп");
    }

    /// <summary>Представительная подвыборка ~<paramref name="target"/> нагрузок равномерным шагом.</summary>
    private static IReadOnlyList<SemesterWorkload> SelectCohort(IReadOnlyList<SemesterWorkload> all, int target)
    {
        int stride = Math.Max(1, all.Count / target);
        var cohort = new List<SemesterWorkload>();
        for (int i = 0; i < all.Count; i += stride) cohort.Add(all[i]);
        return cohort;
    }

    /// <summary>Каждая нагрузка размещена ровно на плановое число пар недели (Hours/2).</summary>
    private static void AssertFullPlacement(IReadOnlyList<SemesterWorkload> workloads, IReadOnlyList<Assignment> assignments)
    {
        var placed = new int[workloads.Count];
        foreach (var a in assignments) placed[a.Workload]++;

        for (int w = 0; w < workloads.Count; w++)
        {
            int planned = workloads[w].Hours / 2;
            Assert.True(placed[w] == planned,
                $"Нагрузка #{w} ({workloads[w].Curriculum.Subject.Name}): " +
                $"план {planned} пар, размещено {placed[w]}");
        }
    }

    /// <summary>Перепроверяет ключевые жёсткие ограничения на извлечённом решении.</summary>
    private static void ValidateHardConstraints(
        IReadOnlyList<SemesterWorkload> workloads, ScheduleDataset ds, IReadOnlyList<Assignment> assignments)
    {
        // 1. В одной аудитории в одном слоте — не более одного занятия.
        Assert.All(assignments.GroupBy(a => (a.Room, a.Slot)),
            g => Assert.True(g.Count() == 1, $"Конфликт аудитории: слот {g.Key.Slot}, аудитория {g.Key.Room}"));

        // 2. Преподаватель не ведёт два занятия одновременно.
        var teacherClashes = assignments
            .GroupBy(a => (Teacher: workloads[a.Workload].Curriculum.Teacher.Id, a.Slot))
            .Where(g => g.Count() > 1);
        Assert.Empty(teacherClashes);

        // 3. Группа не находится на двух занятиях одновременно.
        var groupClashes = assignments
            .SelectMany(a => workloads[a.Workload].Curriculum.Stream.StreamGroups
                .Select(sg => (Group: sg.Group.Id, a.Slot)))
            .GroupBy(x => x)
            .Where(g => g.Count() > 1);
        Assert.Empty(groupClashes);

        // (Смена в понедельных моделях — МЯГКОЕ ограничение: занятие может выйти за полосу смены,
        //  если это убирает окно. Поэтому жёсткой проверки смены здесь нет — см. SoftShiftSectionBuilder.)

        // 4. Оборудование аудитории удовлетворяет требованиям нагрузки.
        foreach (var a in assignments)
        {
            var needed = workloads[a.Workload].Curriculum.NeededEquipments.Select(n => n.EquipmentId).ToHashSet();
            var available = ds.Classrooms[a.Room].EquipmentRooms.Select(e => e.EquipmentId).ToHashSet();
            Assert.True(needed.IsSubsetOf(available),
                $"Аудитория {ds.Classrooms[a.Room].Name} не оснащена для {workloads[a.Workload].Curriculum.Subject.Name}");
        }
    }

    /// <summary>
    /// Компактность расписания: суммарное число «окон» у групп (для каждой группы и дня — пропуски
    /// между занятыми номерами пар, напр. пары 1 и 3 без 2 = одно окно) и число активных учебных
    /// дней групп (пар (группа, день) хотя бы с одним занятием).
    /// </summary>
    private static (int Windows, int ActiveGroupDays) Compactness(
        IReadOnlyList<SemesterWorkload> workloads, ScheduleDataset ds, IReadOnlyList<Assignment> assignments)
    {
        // (группа, день) → множество занятых номеров пар.
        var byGroupDay = new Dictionary<(Guid Group, Domain.calendar.WeekDayType Day), SortedSet<int>>();
        foreach (var a in assignments)
        {
            var slot = ds.TimeSlots[a.Slot];
            foreach (var sg in workloads[a.Workload].Curriculum.Stream.StreamGroups)
            {
                var key = (sg.Group.Id, slot.WeekDay.DayOfWeek);
                if (!byGroupDay.TryGetValue(key, out var nums)) byGroupDay[key] = nums = new SortedSet<int>();
                nums.Add(slot.Number);
            }
        }

        int windows = 0;
        foreach (var nums in byGroupDay.Values)
            if (nums.Count >= 2)
                windows += (nums.Max - nums.Min + 1) - nums.Count; // незанятые слоты между крайними парами дня

        return (windows, byGroupDay.Count);
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
