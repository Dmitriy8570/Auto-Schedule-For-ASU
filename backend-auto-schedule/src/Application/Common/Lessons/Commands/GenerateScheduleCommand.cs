using Application.Common.Interfaces;
using Application.Solver.Builder;
using Application.Solver.Mapping;
using Application.Solver.Solving;
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
    double WallTimeSeconds);

public sealed class GenerateScheduleCommandHandler
    : IRequestHandler<GenerateScheduleCommand, GenerateScheduleResult>
{
    private readonly IScheduleDataProvider _dataProvider;
    private readonly IScheduleSolver _solver;
    private readonly IScheduleResultMapper _mapper;
    private readonly ILessonRepository _lessonRepository;

    public GenerateScheduleCommandHandler(
        IScheduleDataProvider dataProvider,
        IScheduleSolver solver,
        IScheduleResultMapper mapper,
        ILessonRepository lessonRepository)
    {
        _dataProvider = dataProvider;
        _solver = solver;
        _mapper = mapper;
        _lessonRepository = lessonRepository;
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

        var lessons = _mapper.ToLessons(data, solution.Assignments);
        foreach (var lesson in lessons)
            await _lessonRepository.AddAsync(lesson, cancellationToken);
        await _lessonRepository.SaveChangesAsync(cancellationToken);

        return new GenerateScheduleResult(
            solution.Status.ToString(), lessons.Count, solution.ObjectiveValue, solution.WallTimeSeconds);
    }
}
