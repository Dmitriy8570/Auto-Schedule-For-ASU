using System.Runtime;
using Application.Common.Generation;
using Application.Common.Interfaces;
using Application.Solver.Builder;
using Application.Solver.Builder.BuildSections;
using Application.Solver.Compaction;
using Application.Solver.Mapping;
using Application.Solver.Model;
using Application.Solver.Solving;
using Domain.calendar;
using Domain.schedule;
using Domain.workload;
using MediatR;

namespace Application.Common.Lessons.Commands;

/// <summary>
/// Сгенерировать черновик расписания <em>по неделям</em>. Каждая неделя семестра решается отдельной
/// небольшой моделью (нагрузки именно этой недели × аудитории × слоты недели) и сразу записывается в
/// БД — так уже сформированные недели видны во фронтенде по мере генерации. Недели одного типа
/// (красная/синяя) якорятся к предыдущей решённой неделе того же типа (а первая — к прошлому
/// семестру), что даёт регулярное, устойчивое из недели в неделю расписание.
///
/// <see cref="InstituteId"/> == <c>null</c> — генерация по всему университету (нагрузки всех
/// институтов недели решаются совместно); иначе — только указанный институт, с учётом уже занятых
/// в этой неделе ресурсов других институтов.
/// </summary>
public sealed class GenerateScheduleCommand : IRequest<GenerateScheduleResult>
{
    public Guid SemesterId { get; init; }

    /// <summary>Институт генерации; <c>null</c> — весь университет.</summary>
    public Guid? InstituteId { get; init; }

    /// <summary>Переопределить лимит времени солвера (секунды) на неделю; иначе из конфигурации.</summary>
    public double? MaxTimeInSeconds { get; init; }

    /// <summary>Переопределить число параллельных воркеров солвера; иначе из конфигурации.</summary>
    public int? SearchWorkers { get; init; }

    /// <summary>
    /// Необязательный приёмник прогресса по неделям (в процессе, не сериализуется). Устанавливается
    /// фоновой службой, чтобы обновлять статус задачи в очереди по мере прохождения недель.
    /// </summary>
    public IProgress<GenerationProgress>? Progress { get; init; }
}

/// <summary>Итог генерации: статус, метрики, число созданных занятий и понедельная сводка.</summary>
public sealed record GenerateScheduleResult(
    string Status,
    int LessonsCreated,
    double ObjectiveValue,
    double WallTimeSeconds)
{
    /// <summary>
    /// Нагрузки, размещённые не полностью (или вовсе не размещённые): для каждой — преподаватель,
    /// дисциплина, тип занятия и сколько пар из плана недели удалось поставить. Пусто, если всё
    /// размещено целиком. Заполняется, когда компонент недели пришлось решать в аварийном
    /// (best-effort) режиме — см. диагностику «почему преподаватель остался без занятий».
    /// </summary>
    public IReadOnlyList<WorkloadShortfall> Unplaced { get; init; } = Array.Empty<WorkloadShortfall>();

    /// <summary>Всего недель в семестре.</summary>
    public int WeeksTotal { get; init; }

    /// <summary>Недель, для которых поставлено хотя бы одно занятие.</summary>
    public int WeeksGenerated { get; init; }
}

/// <summary>Недоразмещённая нагрузка: кто, что, сколько пар поставлено из плана и почему не все.</summary>
public sealed record WorkloadShortfall(
    string Teacher,
    string Subject,
    LessonType LessonType,
    int PlannedPairs,
    int PlacedPairs,
    string Reason);

public sealed class GenerateScheduleCommandHandler
    : IRequestHandler<GenerateScheduleCommand, GenerateScheduleResult>
{
    private readonly IScheduleDataProvider _dataProvider;
    private readonly IScheduleSolver _solver;
    private readonly IScheduleResultMapper _mapper;
    private readonly ILessonRepository _lessonRepository;
    private readonly ITransactionRunner _transactionRunner;
    private readonly SolverOptions _solverOptions;

    public GenerateScheduleCommandHandler(
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
        GenerateScheduleCommand request, CancellationToken cancellationToken)
    {
        // Параметры солвера из конфигурации с необязательным переопределением на запрос (на неделю).
        var options = _solverOptions;
        if (request.MaxTimeInSeconds is { } maxTime) options = options with { MaxTimeInSeconds = maxTime };
        if (request.SearchWorkers is { } workers) options = options with { SearchWorkers = workers };

        var weeks = await _dataProvider.GetWeeksAsync(request.SemesterId, cancellationToken);

        var lastSolvedByType = new Dictionary<WeekType, SolvedWeek>();
        var shortfalls = new List<WorkloadShortfall>();
        int totalLessons = 0, weeksGenerated = 0, weekIndex = 0;
        double objective = 0, wallSeconds = 0;

        foreach (var week in weeks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            weekIndex++;

            request.Progress?.Report(new GenerationProgress(
                weekIndex, weeks.Count, WeekLabel(week), week.Type));

            var data = await _dataProvider.GetForWeekAsync(
                request.SemesterId, week.Id, request.InstituteId, cancellationToken);

            // Якорь к уже решённой неделе того же типа (сильнее перекрывает якорь прошлого семестра).
            if (lastSolvedByType.TryGetValue(week.Type, out var previous))
                data = data with { Anchors = BuildWeekToWeekAnchors(previous, data) };

            var outcome = SolveWeek(data, options, cancellationToken);

            // Записываем неделю сразу (даже пустую — это очищает устаревшие занятия недели в этом
            // охвате), чтобы фронтенд видел готовые недели по мере генерации.
            await WriteWeekAsync(week.Id, request.InstituteId, outcome.Placed, cancellationToken);

            if (outcome.Placed.Count > 0)
            {
                weeksGenerated++;
                lastSolvedByType[week.Type] = new SolvedWeek(data, outcome.Placed);
            }

            totalLessons += outcome.Placed.Count;
            objective += outcome.Objective;
            wallSeconds += outcome.WallSeconds;
            shortfalls.AddRange(outcome.Shortfalls);
        }

        var status = shortfalls.Count > 0
            ? $"Partial (недель с занятиями {weeksGenerated}/{weeks.Count}, недоразмещено нагрузок {shortfalls.Count})"
            : weeksGenerated == 0 ? "Empty" : "Optimal";

        return new GenerateScheduleResult(status, totalLessons, objective, wallSeconds)
        {
            Unplaced = shortfalls,
            WeeksTotal = weeks.Count,
            WeeksGenerated = weeksGenerated,
        };
    }

    // ----- Решение одной недели (по институтам совместно → best-effort → уплотнение) -----------

    /// <summary>
    /// Решить расписание одной недели. Декомпозиция — <em>по институту</em>: нагрузки института за
    /// неделю решаются ОДНОЙ совместной моделью (а не по группам по очереди). Это принципиально для
    /// качества: преподаватели и аудитории координируются разом, поэтому штраф за окно реально
    /// работает — раньше группы решались последовательно и преподаватель «застолблялся» одной группой,
    /// вынуждая у другой окно, которое мягкий штраф пробить не мог. Благодаря понедельной оси модель
    /// института за неделю невелика (≈ планы института × слоты недели), так что в память помещается.
    /// Если отдельный институт всё же слишком велик — он дробится на компоненты связности по общим
    /// группам (страховка по памяти). Институты между собой делят аудитории, но не преподавателей,
    /// поэтому последовательная блокировка аудиторий между ними окон не создаёт.
    /// </summary>
    private WeekOutcome SolveWeek(ScheduleData data, SolverOptions options, CancellationToken ct)
    {
        // Подзадачи: по институту; крупный институт дробим по группам как страховку по памяти.
        var subproblems = new List<List<int>>();
        foreach (var institute in PartitionByInstitute(data.Workloads))
        {
            if (institute.Count <= JointSolveMaxWorkloads) subproblems.Add(institute);
            else subproblems.AddRange(PartitionBySharedGroups(data.Workloads, institute));
        }
        if (subproblems.Count == 0)
            return new WeekOutcome(Array.Empty<Lesson>(), Array.Empty<WorkloadShortfall>(), 0, 0);

        // Лимит времени конфигурации трактуем как бюджет на ОДНУ подзадачу (институт за неделю):
        // единая модель — оптимизация и выбирает весь лимит, поэтому суммарная длительность семестра
        // регулируется значением в конфигурации (Solver:MaxTimeInSeconds), а не делением здесь.
        var perComponentOptions = options;

        var occupiedRooms = new HashSet<OccupiedSlot>(data.OccupiedClassroomSlots ?? Array.Empty<OccupiedSlot>());
        var occupiedTeachers = new HashSet<OccupiedTeacherSlot>(data.OccupiedTeacherSlots ?? Array.Empty<OccupiedTeacherSlot>());

        var placed = new List<Lesson>();
        var shortfalls = new List<WorkloadShortfall>();
        double objective = 0, wallSeconds = 0;

        foreach (var indices in subproblems)
        {
            ct.ThrowIfCancellationRequested();

            var subset = indices.Select(i => data.Workloads[i]).ToList();
            var subData = data with
            {
                Workloads = subset,
                OccupiedClassroomSlots = occupiedRooms.ToList(),
                OccupiedTeacherSlots = occupiedTeachers.ToList(),
            };

            // Двухфазно. Этап 1 — максимум размещения (мягкое, без качества): сколько пар вообще
            // влезает. Модель проста, солвер обычно завершает досрочно. Всегда разрешима.
            var phase1 = _solver.Solve(
                ScheduleModelDirector.CreatePerWeekMaxPlacement().Build(subData), perComponentOptions);
            wallSeconds += phase1.WallTimeSeconds;

            var chosen = phase1;
            if (phase1.IsSuccess)
            {
                // Покрытие этапа 1 по нагрузкам → нижняя граница для этапа 2.
                var floors = new int[subData.Workloads.Count];
                foreach (var a in phase1.Assignments) floors[a.Workload]++;

                // Этап 2 — компактность/окна при ЗАФИКСИРОВАННОМ покрытии (без награды за размещение,
                // поэтому компактность реально минимизируется, а занятия не выбрасываются).
                var phase2 = _solver.Solve(
                    ScheduleModelDirector.CreatePerWeekQuality(floors).Build(subData), perComponentOptions);
                wallSeconds += phase2.WallTimeSeconds;

                if (phase2.IsSuccess && phase2.Assignments.Count >= phase1.Assignments.Count)
                    chosen = phase2;
            }

            if (chosen.IsSuccess)
            {
                objective += chosen.ObjectiveValue;
                placed.AddRange(_mapper.ToLessons(subData, chosen.Assignments)); // Lesson.Create() => Draft.
                Occupy(subData, chosen.Assignments, occupiedRooms, occupiedTeachers);
                shortfalls.AddRange(CollectShortfalls(subData, chosen.Assignments));
            }
            else
            {
                // Сюда практически не попадаем (модель всегда разрешима) — только при таймауте без
                // единого найденного решения; считаем всю подзадачу недоразмещённой.
                shortfalls.AddRange(CollectShortfalls(subData, Array.Empty<Assignment>()));
            }

            // Возврат памяти ОС между подзадачами (уплотнение LOH) — крупные буферы модели/пресолва.
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }

        // Пост-уплотнение окон в пределах недели (ресурсы других институтов — из исходных занятых).
        if (placed.Count > 0)
            ScheduleCompactor.Compact(
                placed, data,
                data.OccupiedClassroomSlots ?? Array.Empty<OccupiedSlot>(),
                data.OccupiedTeacherSlots ?? Array.Empty<OccupiedTeacherSlot>());

        return new WeekOutcome(placed, shortfalls, objective, wallSeconds);
    }

    /// <summary>Заменить занятия недели (в охвате института или всей недели) новым черновиком в одной транзакции.</summary>
    private Task<int> WriteWeekAsync(
        Guid weekId, Guid? instituteId, IReadOnlyList<Lesson> placed, CancellationToken ct) =>
        _transactionRunner.ExecuteSerializableAsync(async c =>
        {
            var existing = await _lessonRepository.GetByWeekAsync(weekId, instituteId, c);
            _lessonRepository.RemoveRange(existing);

            foreach (var lesson in placed)
                await _lessonRepository.AddAsync(lesson, c);

            await _lessonRepository.SaveChangesAsync(c);
            return placed.Count;
        }, ct);

    // ----- Якорь к предыдущей неделе того же типа --------------------------------------------

    /// <summary>
    /// Построить мягкий якорь к уже решённой неделе того же типа: каждую нагрузку текущей недели
    /// тянет к корпусу и (день недели, номер пары), в которых аналогичное занятие шло в прошлой
    /// неделе того же типа. Так красные недели похожи на красные, синие — на синие (числитель/
    /// знаменатель), а различия в нагрузке переживаются (несовпавшее просто не якорится).
    /// </summary>
    private static IReadOnlyList<WorkloadAnchor> BuildWeekToWeekAnchors(SolvedWeek previous, ScheduleData current)
    {
        var prevBuildingByRoom = previous.Data.Classrooms.ToDictionary(c => c.Id, c => c.BuildingId);
        var prevSlotById = previous.Data.TimeSlots.ToDictionary(s => s.Id);
        var prevCurriculumById = previous.Data.Workloads
            .Where(wl => wl.Curriculum is not null)
            .GroupBy(wl => wl.Curriculum.Id)
            .ToDictionary(g => g.Key, g => g.First().Curriculum);

        // Ключ предмета → корпуса и паттерны слотов из прошлой недели того же типа.
        var history = new Dictionary<CurriculumKey, HistoryEntry>();
        foreach (var lesson in previous.Placed)
        {
            if (lesson.CurriculumId is not { } cid || !prevCurriculumById.TryGetValue(cid, out var cur)) continue;
            if (!prevSlotById.TryGetValue(lesson.TimeSlotId, out var slot)) continue;

            var key = CurriculumKey.Of(cur);
            if (!history.TryGetValue(key, out var entry)) history[key] = entry = new HistoryEntry();

            if (prevBuildingByRoom.TryGetValue(lesson.ClassroomId, out var building))
                entry.Buildings.Add(building);
            entry.SlotKeys.Add((slot.WeekDay.DayOfWeek, slot.Number));
        }

        if (history.Count == 0) return Array.Empty<WorkloadAnchor>();

        var slotsByPattern = current.TimeSlots
            .GroupBy(t => (t.WeekDay.DayOfWeek, t.Number))
            .ToDictionary(g => g.Key, g => g.Select(t => t.Id).ToList());

        var anchors = new List<WorkloadAnchor>();
        for (int w = 0; w < current.Workloads.Count; w++)
        {
            if (current.Workloads[w].Curriculum is not { } curriculum) continue;
            if (!history.TryGetValue(CurriculumKey.Of(curriculum), out var entry)) continue;

            var preferredSlots = entry.SlotKeys
                .SelectMany(p => slotsByPattern.TryGetValue(p, out var ids) ? ids : Enumerable.Empty<Guid>())
                .Distinct()
                .ToList();

            Guid? preferredBuilding = entry.Buildings.Count > 0 ? entry.Buildings.First() : null;
            if (preferredBuilding is null && preferredSlots.Count == 0) continue;

            anchors.Add(new WorkloadAnchor(w, preferredBuilding, preferredSlots));
        }

        return anchors;
    }

    // ----- Вспомогательное -------------------------------------------------------------------

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
                subData.Workloads[a.Workload].Curriculum.TeacherId, slotId));
        }
    }

    /// <summary>
    /// Нагрузки компонента, размещённые меньше плана недели (включая нулевое размещение): для
    /// диагностики «почему преподаватель остался без занятий».
    /// </summary>
    private static IEnumerable<WorkloadShortfall> CollectShortfalls(
        ScheduleData subData, IReadOnlyList<Assignment> assignments)
    {
        var placedByWorkload = new int[subData.Workloads.Count];
        foreach (var a in assignments) placedByWorkload[a.Workload]++;

        for (int w = 0; w < subData.Workloads.Count; w++)
        {
            var workload = subData.Workloads[w];
            int planned = workload.Hours / 2;
            int placedPairs = placedByWorkload[w];
            if (placedPairs >= planned) continue;

            var curriculum = workload.Curriculum;
            yield return new WorkloadShortfall(
                curriculum.Teacher.Name, curriculum.Subject.Name,
                curriculum.LessonType, planned, placedPairs, ShortfallReason(workload, subData));
        }
    }

    /// <summary>Объяснить, почему нагрузку не удалось разместить полностью — для диагностики в интерфейсе.</summary>
    private static string ShortfallReason(WorkloadItem workload, ScheduleData subData)
    {
        bool anyRoomFits = subData.Classrooms.Any(c => !StaticFeasibility.RoomForbidden(workload, c));
        if (anyRoomFits)
            return "не хватило свободных слотов (конфликты с другими занятиями или недоступность преподавателя)";

        int students = workload.Curriculum?.Stream?.StudentsCount ?? 0;
        if (students > 0 && !subData.Classrooms.Any(c => c.CanAccommodate(students)))
            return $"ни одна аудитория не вмещает {students} студентов";

        return "нет аудитории с требуемым оборудованием";
    }

    /// <summary>Порог: институт крупнее этого числа нагрузок дробим на компоненты (страховка по памяти).</summary>
    private const int JointSolveMaxWorkloads = 600;

    /// <summary>
    /// Институт нагрузки — по первой группе её потока (Group → Course → Degree → Institute).
    /// Null-устойчиво: при отсутствии навигаций (минимальные модели тестов) вернёт <see cref="Guid.Empty"/>.
    /// </summary>
    private static Guid InstituteOf(WorkloadItem w) =>
        w.Curriculum.Stream.StreamGroups
            .Select(sg => sg.Group?.Course?.Degree?.InstituteId ?? Guid.Empty)
            .FirstOrDefault();

    /// <summary>
    /// Разбить нагрузки недели по институтам: нагрузки одного института решаются ОДНОЙ совместной
    /// моделью. Преподаватели не пересекают институты, поэтому совместное решение института убирает
    /// «жадные» окна между группами (раньше группы решались по очереди с блокировкой преподавателя).
    /// </summary>
    private static List<List<int>> PartitionByInstitute(IReadOnlyList<WorkloadItem> workloads)
    {
        var byInstitute = new Dictionary<Guid, List<int>>();
        for (int i = 0; i < workloads.Count; i++)
        {
            var inst = InstituteOf(workloads[i]);
            if (!byInstitute.TryGetValue(inst, out var list)) byInstitute[inst] = list = new List<int>();
            list.Add(i);
        }
        return byInstitute.Values.ToList();
    }

    /// <summary>
    /// Страховка по памяти: разбить подмножество нагрузок <paramref name="indices"/> на компоненты
    /// связности по общим учебным группам (union-find). Применяется, только если институт слишком велик
    /// для совместной модели. Возвращает списки исходных индексов в <paramref name="workloads"/>.
    /// </summary>
    private static List<List<int>> PartitionBySharedGroups(
        IReadOnlyList<WorkloadItem> workloads, IReadOnlyList<int> indices)
    {
        int n = indices.Count;
        var parent = new int[n];
        for (int p = 0; p < n; p++) parent[p] = p;

        int Find(int x)
        {
            while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
            return x;
        }
        void Union(int a, int b) => parent[Find(a)] = Find(b);

        var firstByGroup = new Dictionary<Guid, int>();
        for (int p = 0; p < n; p++)
            foreach (var sg in workloads[indices[p]].Curriculum.Stream.StreamGroups)
            {
                if (firstByGroup.TryGetValue(sg.GroupId, out int q)) Union(p, q);
                else firstByGroup[sg.GroupId] = p;
            }

        var components = new Dictionary<int, List<int>>();
        for (int p = 0; p < n; p++)
        {
            int root = Find(p);
            if (!components.TryGetValue(root, out var list)) components[root] = list = new List<int>();
            list.Add(indices[p]); // исходный индекс нагрузки
        }
        return components.Values.ToList();
    }

    private static string WeekLabel(ScheduleWeek week) =>
        $"{week.StartDate:dd.MM} – {week.EndDate:dd.MM}";

    // ----- Локальные типы --------------------------------------------------------------------

    /// <summary>Уже решённая неделя (данные + размещённые занятия) — источник якоря для следующей недели её типа.</summary>
    private sealed record SolvedWeek(ScheduleData Data, IReadOnlyList<Lesson> Placed);

    /// <summary>Итог решения одной недели.</summary>
    private sealed record WeekOutcome(
        IReadOnlyList<Lesson> Placed,
        IReadOnlyList<WorkloadShortfall> Shortfalls,
        double Objective,
        double WallSeconds);

    private readonly record struct CurriculumKey(Guid TeacherId, Guid SubjectId, LessonType LessonType)
    {
        public static CurriculumKey Of(Curriculum c) => new(c.TeacherId, c.SubjectId, c.LessonType);
    }

    private sealed class HistoryEntry
    {
        public HashSet<Guid> Buildings { get; } = new();
        public HashSet<(WeekDayType DayOfWeek, int Number)> SlotKeys { get; } = new();
    }
}
