using Application.Common.Interfaces;
using Domain.schedule;
using MediatR;

namespace Application.Common.Lessons.Commands;

/// <summary>
/// Сбросить черновик расписания института до выгруженного: удалить его занятия-черновики (Draft),
/// оставив ранее опубликованное расписание (Current) без изменений.
/// Операция идемпотентна — если черновика нет, удалять нечего.
/// </summary>
public sealed class DiscardInstituteScheduleCommand : IRequest<DiscardInstituteScheduleResult>
{
    public Guid InstituteId { get; init; }
}

/// <summary>Итог сброса: сколько занятий-черновиков удалено.</summary>
public sealed record DiscardInstituteScheduleResult(int Discarded);

public sealed class DiscardInstituteScheduleCommandHandler
    : IRequestHandler<DiscardInstituteScheduleCommand, DiscardInstituteScheduleResult>
{
    private readonly ILessonRepository _lessonRepository;

    public DiscardInstituteScheduleCommandHandler(ILessonRepository lessonRepository)
        => _lessonRepository = lessonRepository;

    public async Task<DiscardInstituteScheduleResult> Handle(
        DiscardInstituteScheduleCommand request, CancellationToken cancellationToken)
    {
        var lessons = await _lessonRepository.GetByInstituteAsync(request.InstituteId, cancellationToken);

        var drafts = lessons.Where(l => l.Version == ScheduleVersion.Draft).ToList();
        if (drafts.Count == 0)
            return new DiscardInstituteScheduleResult(0);

        _lessonRepository.RemoveRange(drafts);
        await _lessonRepository.SaveChangesAsync(cancellationToken);

        return new DiscardInstituteScheduleResult(drafts.Count);
    }
}
