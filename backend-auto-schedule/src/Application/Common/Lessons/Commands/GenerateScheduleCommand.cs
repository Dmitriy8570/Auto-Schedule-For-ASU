using Application.Common.Interfaces;
using Application.Solver.Builder;
using Application.Solver.Mapping;
using Application.Solver.Solving;
using Domain.workload;
using MediatR;

namespace Application.Common.Lessons.Commands;

/// <summary>Сгенерировать черновик расписания на семестр: построить модель, решить, сохранить занятия.</summary>
public sealed class GenerateScheduleCommand : IRequest<GenerateScheduleResult>
{
    public Guid SemesterId { get; init; }
}

/// <summary>Итог генерации: статус солвера, метрики и число созданных занятий.</summary>
public sealed record GenerateScheduleResult(
    string Status,
    int LessonsCreated,
    double ObjectiveValue,
    double WallTimeSeconds)
{
    /// <summary>
    /// Нагрузки, размещённые не полностью (или вовсе не размещённые): для каждой — преподаватель,
    /// дисциплина, тип занятия и сколько пар из плана удалось поставить. Пусто, если расписание
    /// размещено целиком. Заполняется при покомпонентной генерации института, когда компонент
    /// пришлось решать в аварийном (best-effort) режиме — см. диагностику «почему преподаватель
    /// остался без занятий».
    /// </summary>
    public IReadOnlyList<WorkloadShortfall> Unplaced { get; init; } = Array.Empty<WorkloadShortfall>();
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

    public GenerateScheduleCommandHandler(
        IScheduleDataProvider dataProvider,
        IScheduleSolver solver,
        IScheduleResultMapper mapper,
        ILessonRepository lessonRepository,
        ITransactionRunner transactionRunner)
    {
        _dataProvider = dataProvider;
        _solver = solver;
        _mapper = mapper;
        _lessonRepository = lessonRepository;
        _transactionRunner = transactionRunner;
    }

    public async Task<GenerateScheduleResult> Handle(
        GenerateScheduleCommand request, CancellationToken cancellationToken)
    {
        var data = await _dataProvider.GetAsync(request.SemesterId, cancellationToken);

        var model = ScheduleModelDirector.CreateDefault().Build(data);
        var solution = _solver.Solve(model);

        if (!solution.IsSuccess)
            return new GenerateScheduleResult(
                solution.Status.ToString(), 0, solution.ObjectiveValue, solution.WallTimeSeconds);

        // Заменяем прежнее расписание семестра (а не добавляем поверх). Поиск солвером выполнен
        // выше, вне транзакции; короткая запись результата — в SERIALIZABLE-транзакции, где
        // удаление старого расписания и вставка нового атомарны.
        var lessonCount = await _transactionRunner.ExecuteSerializableAsync(async ct =>
        {
            var existing = await _lessonRepository.GetBySemesterAsync(request.SemesterId, ct);
            _lessonRepository.RemoveRange(existing);

            var lessons = _mapper.ToLessons(data, solution.Assignments);
            foreach (var lesson in lessons)
                await _lessonRepository.AddAsync(lesson, ct);
            await _lessonRepository.SaveChangesAsync(ct);
            return lessons.Count;
        }, cancellationToken);

        return new GenerateScheduleResult(
            solution.Status.ToString(), lessonCount, solution.ObjectiveValue, solution.WallTimeSeconds);
    }
}
