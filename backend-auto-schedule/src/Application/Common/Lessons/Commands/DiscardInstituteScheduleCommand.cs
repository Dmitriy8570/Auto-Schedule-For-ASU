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
    private readonly ITransactionRunner _transactionRunner;

    public DiscardInstituteScheduleCommandHandler(
        ILessonRepository lessonRepository, ITransactionRunner transactionRunner)
    {
        _lessonRepository = lessonRepository;
        _transactionRunner = transactionRunner;
    }

    public async Task<DiscardInstituteScheduleResult> Handle(
        DiscardInstituteScheduleCommand request, CancellationToken cancellationToken)
    {
        // Сброс черновика выполняется в SERIALIZABLE-транзакции, согласованно с публикацией и
        // перегенерацией расписания того же института.
        return await _transactionRunner.ExecuteSerializableAsync(async ct =>
        {
            var lessons = await _lessonRepository.GetByInstituteAsync(request.InstituteId, ct);

            var drafts = lessons.Where(l => l.Version == ScheduleVersion.Draft).ToList();
            if (drafts.Count == 0)
                return new DiscardInstituteScheduleResult(0);

            _lessonRepository.RemoveRange(drafts);
            await _lessonRepository.SaveChangesAsync(ct);

            return new DiscardInstituteScheduleResult(drafts.Count);
        }, cancellationToken);
    }
}
