using System.Runtime;
using Application.Common.Interfaces;
using Application.Solver.Builder;
using Application.Solver.Builder.BuildSections;
using Application.Solver.Compaction;
using Application.Solver.Mapping;
using Application.Solver.Model;
using Application.Solver.Solving;
using Domain.schedule;
using Domain.workload;
using MediatR;

namespace Application.Common.Lessons.Commands;

/// <summary>
/// Сгенерировать черновик расписания для одного института на семестр (декомпозиция «B + C»).
/// Перед генерацией текущее расписание института удаляется; новые занятия создаются как черновик
/// (<see cref="Domain.schedule.ScheduleVersion.Draft"/>).
/// </summary>
public sealed class GenerateInstituteScheduleCommand : IRequest<GenerateScheduleResult>
{
    public Guid SemesterId { get; init; }
    public Guid InstituteId { get; init; }

    /// <summary>Переопределить лимит времени солвера (секунды) для этого запроса; иначе из конфигурации.</summary>
    public double? MaxTimeInSeconds { get; init; }

    /// <summary>Переопределить число параллельных воркеров солвера для этого запроса; иначе из конфигурации.</summary>
    public int? SearchWorkers { get; init; }
}

public sealed class GenerateInstituteScheduleCommandHandler
    : IRequestHandler<GenerateInstituteScheduleCommand, GenerateScheduleResult>
{
    private readonly IScheduleDataProvider _dataProvider;
    private readonly IScheduleSolver _solver;
    private readonly IScheduleResultMapper _mapper;
    private readonly ILessonRepository _lessonRepository;
    private readonly ITransactionRunner _transactionRunner;
    private readonly SolverOptions _solverOptions;

    public GenerateInstituteScheduleCommandHandler(
        IScheduleDataProvider dataProvider,
        IScheduleSolver solver,
        IScheduleResultMapper mapper,
        ILessonRepository lessonRepository,
        ITransactionRunner transactionRunner,
        SolverOptions solverOptions)
    {
        _dataProvider = dataProvider;
        _solver = solver;
        _mapper = mapper;
        _lessonRepository = lessonRepository;
        _transactionRunner = transactionRunner;
        _solverOptions = solverOptions;
    }

    public async Task<GenerateScheduleResult> Handle(
        GenerateInstituteScheduleCommand request, CancellationToken cancellationToken)
    {
        var data = await _dataProvider.GetForInstituteAsync(
            request.SemesterId, request.InstituteId, cancellationToken);

        // Параметры солвера из конфигурации с необязательным переопределением на запрос.
        var options = _solverOptions;
        if (request.MaxTimeInSeconds is { } maxTime) options = options with { MaxTimeInSeconds = maxTime };
        if (request.SearchWorkers is { } workers) options = options with { SearchWorkers = workers };

        // Декомпозиция института на независимые подзадачи (компоненты связности по общим группам).
        // Монолитная модель института (сотни нагрузок × все аудитории × все слоты семестра) не
        // помещалась в память и приводила к падению процесса (OOM) во время генерации. Каждый
        // компонент решается небольшой моделью; уже размещённые (аудитория, слот) и (преподаватель,
        // слот) передаются следующим компонентам как занятые ресурсы — тем же механизмом, что и при
        // межинститутской декомпозиции, поэтому жёсткие ограничения сохраняются. Группы не делятся
        // между компонентами, поэтому пересечения по группам остаются внутри одной подзадачи.
        var components = PartitionBySharedGroups(data.SemesterWorkloads);

        // Бюджет времени делим между подзадачами, чтобы общий бюджет института был ограничен
        // (компоненты невелики и обычно решаются за секунды; деление страхует от патологий).
        var perComponentOptions = options with
        {
            MaxTimeInSeconds = Math.Max(15, options.MaxTimeInSeconds / Math.Max(1, components.Count)),
        };

        // Аварийный (best-effort) проход запускается лишь для не решившихся компонентов и нужен
        // только чтобы быстро разместить максимум возможного + собрать диагностику. Ограничиваем его
        // время отдельно (короче основного слота), чтобы провалившиеся компоненты не удваивали общий
        // бюджет института и генерация укладывалась в обещанный пользователю лимит.
        var salvageOptions = perComponentOptions with
        {
            MaxTimeInSeconds = Math.Min(perComponentOptions.MaxTimeInSeconds, 60),
        };

        var occupiedRooms = new HashSet<OccupiedSlot>(data.OccupiedClassroomSlots ?? Array.Empty<OccupiedSlot>());
        var occupiedTeachers = new HashSet<OccupiedTeacherSlot>(data.OccupiedTeacherSlots ?? Array.Empty<OccupiedTeacherSlot>());

        var placed = new List<Lesson>();
        var shortfalls = new List<WorkloadShortfall>();
        double objective = 0, wallSeconds = 0;
        int optimal = 0, feasible = 0, degraded = 0, failed = 0;

        foreach (var indices in components)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subset = indices.Select(i => data.SemesterWorkloads[i]).ToList();
            var subData = data with
            {
                SemesterWorkloads = subset,
                OccupiedClassroomSlots = occupiedRooms.ToList(),
                OccupiedTeacherSlots = occupiedTeachers.ToList(),
                Anchors = null, // якорь прошлого семестра опускаем в подзадачах (мягкое предпочтение)
            };

            var solution = SolveComponent(subData, perComponentOptions);
            wallSeconds += solution.WallTimeSeconds;

            if (solution.IsSuccess)
            {
                if (solution.Status == ScheduleSolveStatus.Optimal) optimal++; else feasible++;
                objective += solution.ObjectiveValue;

                placed.AddRange(_mapper.ToLessons(subData, solution.Assignments)); // Lesson.Create() => Draft.
                Occupy(subData, solution.Assignments, occupiedRooms, occupiedTeachers);
            }
            else
            {
                // Жёсткая модель компонента неразрешима/не решилась за время — иначе все его нагрузки
                // теряются целиком (преподаватели остаются вовсе без занятий). Пробуем разместить
                // столько, сколько физически возможно, сохраняя все жёсткие ограничения.
                var salvage = SolveComponentBestEffort(subData, salvageOptions);
                wallSeconds += salvage.WallTimeSeconds;

                if (salvage.IsSuccess && salvage.Assignments.Count > 0)
                {
                    degraded++;
                    placed.AddRange(_mapper.ToLessons(subData, salvage.Assignments));
                    Occupy(subData, salvage.Assignments, occupiedRooms, occupiedTeachers);
                    shortfalls.AddRange(CollectShortfalls(subData, salvage.Assignments));
                }
                else
                {
                    // Не удалось разместить даже частично — компонент целиком без занятий.
                    failed++;
                    shortfalls.AddRange(CollectShortfalls(subData, Array.Empty<Assignment>()));
                }
            }

            // Возвращаем память ОС между компонентами (с уплотнением LOH): без этого рабочий набор
            // растёт суммой по компонентам и упирается в потолок виртуалки. Блокирующая уплотняющая
            // сборка освобождает крупные буферы модели/пресолва перед построением следующей подзадачи.
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }

        // Ничего не разместили — не трогаем БД (сохраняем прежнее расписание), сообщаем статус.
        if (placed.Count == 0)
            return new GenerateScheduleResult(
                failed > 0 ? "Infeasible" : "Empty", 0, objective, wallSeconds)
            { Unplaced = shortfalls };

        // Пост-уплотнение: последовательная декомпозиция оставляет «окна» (преподаватель/группа
        // получают слот «застолблённым» ранее обработанной подзадачей). Штраф за окно мягкий и не
        // пересиливает жёсткую блокировку, поэтому окна убираем здесь — детерминированной
        // перестановкой уже размещённых пар в более ранние свободные слоты того же дня при сохранении
        // всех жёстких ограничений (ресурсы других институтов берём из исходных занятых слотов).
        ScheduleCompactor.Compact(
            placed, data,
            data.OccupiedClassroomSlots ?? Array.Empty<OccupiedSlot>(),
            data.OccupiedTeacherSlots ?? Array.Empty<OccupiedTeacherSlot>());

        // Заменяем расписание института новым черновиком одной SERIALIZABLE-транзакцией (поиск
        // выполнен выше, вне транзакции): удаление прежнего и вставка нового атомарны и согласованы
        // с параллельной генерацией.
        var lessonCount = await _transactionRunner.ExecuteSerializableAsync(async ct =>
        {
            var existing = await _lessonRepository.GetByInstituteAndSemesterAsync(
                request.InstituteId, request.SemesterId, ct);
            _lessonRepository.RemoveRange(existing);

            foreach (var lesson in placed)
                await _lessonRepository.AddAsync(lesson, ct);

            await _lessonRepository.SaveChangesAsync(ct);
            return placed.Count;
        }, cancellationToken);

        // Полностью решены не все компоненты (часть пришлось доразмещать частично или они вовсе без
        // занятий) — статус «Partial» с разбивкой; иначе обычный Optimal/Feasible.
        var status = (degraded > 0 || failed > 0)
            ? $"Partial ({optimal + feasible}/{components.Count}, частично {degraded}, без занятий {failed})"
            : feasible > 0 ? "Feasible" : "Optimal";
        return new GenerateScheduleResult(status, lessonCount, objective, wallSeconds)
        { Unplaced = shortfalls };
    }

    /// <summary>Построить модель компонента и решить её. Модель локальна — освобождается GC после возврата.</summary>
    private ScheduleSolution SolveComponent(ScheduleData subData, SolverOptions options)
    {
        var model = ScheduleModelDirector.CreatePerInstitute().Build(subData);
        return _solver.Solve(model, options);
    }

    /// <summary>Аварийный прогон компонента: разместить максимум возможного при жёстких ограничениях.</summary>
    private ScheduleSolution SolveComponentBestEffort(ScheduleData subData, SolverOptions options)
    {
        var model = ScheduleModelDirector.CreatePerInstituteBestEffort().Build(subData);
        return _solver.Solve(model, options);
    }

    /// <summary>Накопить занятые (аудитория, слот) и (преподаватель, слот) для следующих компонентов.</summary>
    private static void Occupy(
        ScheduleData subData, IReadOnlyList<Assignment> assignments,
        HashSet<OccupiedSlot> occupiedRooms, HashSet<OccupiedTeacherSlot> occupiedTeachers)
    {
        foreach (var a in assignments)
        {
            var slotId = subData.TimeSlots[a.Slot].Id;
            occupiedRooms.Add(new OccupiedSlot(subData.Classrooms[a.Room].Id, slotId));
            occupiedTeachers.Add(new OccupiedTeacherSlot(
                subData.SemesterWorkloads[a.Workload].Curriculum.TeacherId, slotId));
        }
    }

    /// <summary>
    /// Нагрузки компонента, размещённые меньше плана (включая нулевое размещение): для диагностики
    /// «почему преподаватель остался без занятий». Сравнивает фактически поставленные пары с плановыми
    /// (<c>Hours/2</c>) и возвращает дефицит по каждой недоразмещённой нагрузке.
    /// </summary>
    private static IEnumerable<WorkloadShortfall> CollectShortfalls(
        ScheduleData subData, IReadOnlyList<Assignment> assignments)
    {
        var placedByWorkload = new int[subData.SemesterWorkloads.Count];
        foreach (var a in assignments) placedByWorkload[a.Workload]++;

        for (int w = 0; w < subData.SemesterWorkloads.Count; w++)
        {
            var workload = subData.SemesterWorkloads[w];
            int planned = workload.Hours / 2;
            int placedPairs = placedByWorkload[w];
            if (placedPairs >= planned) continue;

            var curriculum = workload.Curriculum;
            yield return new WorkloadShortfall(
                curriculum.Teacher.Name, curriculum.Subject.Name,
                curriculum.LessonType, planned, placedPairs, ShortfallReason(workload, subData));
        }
    }

    /// <summary>
    /// Объяснить, почему нагрузку не удалось разместить полностью — для диагностики в интерфейсе.
    /// Если ни одна аудитория не подходит статически (вместимость/оборудование), причина в аудиториях;
    /// иначе подходящие аудитории есть, но не осталось свободных слотов (конфликты с другими занятиями
    /// или недоступность преподавателя).
    /// </summary>
    private static string ShortfallReason(SemesterWorkload workload, ScheduleData subData)
    {
        bool anyRoomFits = subData.Classrooms.Any(c => !StaticFeasibility.RoomForbidden(workload, c));
        if (anyRoomFits)
            return "не хватило свободных слотов (конфликты с другими занятиями или недоступность преподавателя)";

        int students = workload.Curriculum?.Stream?.StudentsCount ?? 0;
        if (students > 0 && !subData.Classrooms.Any(c => c.CanAccommodate(students)))
            return $"ни одна аудитория не вмещает {students} студентов";

        return "нет аудитории с требуемым оборудованием";
    }

    /// <summary>
    /// Разбить нагрузки на независимые подзадачи — компоненты связности по общим учебным группам
    /// (union-find). Нагрузки, делящие хотя бы одну группу (например, лекционный поток и его
    /// семинары), попадают в один компонент; разные курсы/группы не пересекаются по группам и
    /// решаются раздельно. Возвращает списки индексов нагрузок в исходном <paramref name="workloads"/>.
    /// </summary>
    private static List<List<int>> PartitionBySharedGroups(IReadOnlyList<SemesterWorkload> workloads)
    {
        int n = workloads.Count;
        var parent = new int[n];
        for (int i = 0; i < n; i++) parent[i] = i;

        int Find(int x)
        {
            while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
            return x;
        }
        void Union(int a, int b) => parent[Find(a)] = Find(b);

        var firstByGroup = new Dictionary<Guid, int>();
        for (int i = 0; i < n; i++)
            foreach (var sg in workloads[i].Curriculum.Stream.StreamGroups)
            {
                if (firstByGroup.TryGetValue(sg.GroupId, out int j)) Union(i, j);
                else firstByGroup[sg.GroupId] = i;
            }

        var components = new Dictionary<int, List<int>>();
        for (int i = 0; i < n; i++)
        {
            int root = Find(i);
            if (!components.TryGetValue(root, out var list)) components[root] = list = new List<int>();
            list.Add(i);
        }
        return components.Values.ToList();
    }
}
