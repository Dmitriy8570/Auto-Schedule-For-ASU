using Application.Common.Interfaces;
using Application.Solver.Builder;
using Application.Solver.Mapping;
using Application.Solver.Solving;
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
    private readonly SolverOptions _solverOptions;

    public GenerateInstituteScheduleCommandHandler(
        IScheduleDataProvider dataProvider,
        IScheduleSolver solver,
        IScheduleResultMapper mapper,
        ILessonRepository lessonRepository,
        SolverOptions solverOptions)
    {
        _dataProvider = dataProvider;
        _solver = solver;
        _mapper = mapper;
        _lessonRepository = lessonRepository;
        _solverOptions = solverOptions;
    }

    public async Task<GenerateScheduleResult> Handle(
        GenerateInstituteScheduleCommand request, CancellationToken cancellationToken)
    {
        var data = await _dataProvider.GetForInstituteAsync(
            request.SemesterId, request.InstituteId, cancellationToken);

        var model = ScheduleModelDirector.CreatePerInstitute().Build(data);

        // Параметры солвера из конфигурации с необязательным переопределением на запрос.
        var options = _solverOptions;
        if (request.MaxTimeInSeconds is { } maxTime) options = options with { MaxTimeInSeconds = maxTime };
        if (request.SearchWorkers is { } workers) options = options with { SearchWorkers = workers };

        var solution = _solver.Solve(model, options);

        if (!solution.IsSuccess)
            return new GenerateScheduleResult(
                solution.Status.ToString(), 0, solution.ObjectiveValue, solution.WallTimeSeconds);

        // Решение найдено — заменяем текущее расписание института новым черновиком.
        // Удаление и вставка фиксируются одним SaveChanges, чтобы не оставить институт без расписания.
        var existing = await _lessonRepository.GetByInstituteAndSemesterAsync(
            request.InstituteId, request.SemesterId, cancellationToken);
        _lessonRepository.RemoveRange(existing);

        var lessons = _mapper.ToLessons(data, solution.Assignments); // Lesson.Create() => Version = Draft.
        foreach (var lesson in lessons)
            await _lessonRepository.AddAsync(lesson, cancellationToken);

        await _lessonRepository.SaveChangesAsync(cancellationToken);

        return new GenerateScheduleResult(
            solution.Status.ToString(), lessons.Count, solution.ObjectiveValue, solution.WallTimeSeconds);
    }
}
