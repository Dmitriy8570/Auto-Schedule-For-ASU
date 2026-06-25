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
    /// Решить расписание одной недели ОДНОЙ совместной моделью на весь охват генерации (институт —
    /// когда задан <see cref="GenerateScheduleCommand.InstituteId"/>, иначе весь университет; данные
    /// уже отфильтрованы провайдером). Совместное решение принципиально для качества: преподаватели и
    /// аудитории координируются разом, поэтому штраф за окно реально работает — раньше группы решались
    /// последовательно и преподаватель «застолблялся» одной группой, вынуждая у другой окно, которое
    /// мягкий штраф пробить не мог. Благодаря понедельной оси модель невелика (≈ планы × слоты недели),
    /// так что помещается в память. Лимит времени из конфигурации (Solver:MaxTimeInSeconds) — это
    /// бюджет на одну неделю; суммарная длительность семестра регулируется им же.
    /// </summary>
    private WeekOutcome SolveWeek(ScheduleData data, SolverOptions options, CancellationToken ct)
    {
        if (data.Workloads.Count == 0)
            return new WeekOutcome(Array.Empty<Lesson>(), Array.Empty<WorkloadShortfall>(), 0, 0);

        ct.ThrowIfCancellationRequested();

        var placed = new List<Lesson>();
        var shortfalls = new List<WorkloadShortfall>();
        double objective = 0, wallSeconds = 0;

        // Двухфазно. Этап 1 — максимум размещения (мягкое, без качества): сколько пар вообще
        // влезает. Модель проста, солвер обычно завершает досрочно. Всегда разрешима.
        var phase1 = _solver.Solve(
            ScheduleModelDirector.CreatePerWeekMaxPlacement().Build(data), options);
        wallSeconds += phase1.WallTimeSeconds;

        var chosen = phase1;
        if (phase1.IsSuccess)
        {
            // Покрытие этапа 1 по нагрузкам → нижняя граница для этапа 2.
            var floors = new int[data.Workloads.Count];
            foreach (var a in phase1.Assignments) floors[a.Workload]++;

            // Этап 2 — компактность/окна при ЗАФИКСИРОВАННОМ покрытии (без награды за размещение,
            // поэтому компактность реально минимизируется, а занятия не выбрасываются). Тёплый старт:
            // решение этапа 1 допустимо и для этапа 2 (то же покрытие, те же жёсткие ограничения),
            // поэтому подаём его подсказкой — солвер сразу получает стартовый инкумбент, и весь бюджет
            // времени идёт на УЛУЧШЕНИЕ качества, а не на повторный поиск любого допустимого решения.
            // На институтской модели (~сотни нагрузок) без этого этап 2 не успевает уйти от размещения.
            var phase2Model = ScheduleModelDirector.CreatePerWeekQuality(floors).Build(data);
            HintAssignments(phase2Model, phase1.Assignments);
            var phase2 = _solver.Solve(phase2Model, options);
            wallSeconds += phase2.WallTimeSeconds;

            if (phase2.IsSuccess && phase2.Assignments.Count >= phase1.Assignments.Count)
                chosen = phase2;
        }

        if (chosen.IsSuccess)
        {
            objective += chosen.ObjectiveValue;
            placed.AddRange(_mapper.ToLessons(data, chosen.Assignments)); // Lesson.Create() => Draft.
            shortfalls.AddRange(CollectShortfalls(data, chosen.Assignments));
        }
        else
        {
            // Сюда практически не попадаем (модель всегда разрешима) — только при таймауте без
            // единого найденного решения; считаем всю неделю недоразмещённой.
            shortfalls.AddRange(CollectShortfalls(data, Array.Empty<Assignment>()));
        }

        // Возврат памяти ОС после решения недели (уплотнение LOH) — крупные буферы модели/пресолва.
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

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

    /// <summary>
    /// Тёплый старт CP-SAT: подсказать модели полное решение этапа 1 — для каждой созданной переменной
    /// (нагрузка, аудитория, слот) указать «занята/нет». Это допустимое решение этапа 2 (покрытие то же,
    /// жёсткие ограничения те же), поэтому солвер берёт его стартовым инкумбентом и тратит весь бюджет
    /// на минимизацию окон и числа учебных дней, а не на повторный поиск любого допустимого решения.
    /// </summary>
    private static void HintAssignments(ScheduleModel model, IReadOnlyList<Assignment> assignments)
    {
        var occupied = new HashSet<(int W, int R, int T)>(
            assignments.Select(a => (a.Workload, a.Room, a.Slot)));

        for (int w = 0; w < model.WorkloadCount; w++)
            for (int r = 0; r < model.ClassroomCount; r++)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    if (model.Lessons[w, r, t] is { } cell)
                        model.Model.AddHint(cell, occupied.Contains((w, r, t)) ? 1 : 0);
    }

    // ----- Вспомогательное -------------------------------------------------------------------

    /// <summary>
    /// Нагрузки недели, размещённые меньше плана недели (включая нулевое размещение): для
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
