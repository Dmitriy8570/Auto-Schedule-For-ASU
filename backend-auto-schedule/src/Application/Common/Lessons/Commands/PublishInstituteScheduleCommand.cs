using Application.Common.Interfaces;
using Domain.schedule;
using MediatR;

namespace Application.Common.Lessons.Commands;

/// <summary>
/// Опубликовать расписание института: перевести его занятия из черновика (Draft) в текущее (Current),
/// заменив ранее опубликованное расписание этого института.
/// </summary>
public sealed class PublishInstituteScheduleCommand : IRequest<PublishInstituteScheduleResult>
{
    public Guid InstituteId { get; init; }
}

/// <summary>Итог публикации: сколько занятий стало текущими.</summary>
public sealed record PublishInstituteScheduleResult(int Published);

public sealed class PublishInstituteScheduleCommandHandler
    : IRequestHandler<PublishInstituteScheduleCommand, PublishInstituteScheduleResult>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IRealtimeNotifier _notifier;

    public PublishInstituteScheduleCommandHandler(
        ILessonRepository lessonRepository, ITransactionRunner transactionRunner, IRealtimeNotifier notifier)
    {
        _lessonRepository = lessonRepository;
        _transactionRunner = transactionRunner;
        _notifier = notifier;
    }

    public async Task<PublishInstituteScheduleResult> Handle(
        PublishInstituteScheduleCommand request, CancellationToken cancellationToken)
    {
        // Замена опубликованного расписания черновиком выполняется в одной SERIALIZABLE-транзакции:
        // удаление прежнего Current и публикация Draft видны другим пользователям атомарно, без
        // промежуточного состояния «институт без расписания».
        var result = await _transactionRunner.ExecuteSerializableAsync(async ct =>
        {
            var lessons = await _lessonRepository.GetByInstituteAsync(request.InstituteId, ct);

            var drafts = lessons.Where(l => l.Version == ScheduleVersion.Draft).ToList();
            if (drafts.Count == 0)
                throw new KeyNotFoundException("Черновик расписания института отсутствует.");

            // Старое опубликованное расписание заменяется черновиком.
            var currentlyPublished = lessons.Where(l => l.Version == ScheduleVersion.Current);
            _lessonRepository.RemoveRange(currentlyPublished);

            foreach (var lesson in drafts)
                lesson.Publish();

            await _lessonRepository.SaveChangesAsync(ct);

            return new PublishInstituteScheduleResult(drafts.Count);
        }, cancellationToken);

        // После фиксации транзакции оповещаем подключённых клиентов: опубликованное расписание
        // института обновилось — их сетки расписания должны перезагрузиться в реальном времени.
        await _notifier.NotifyScheduleChangedAsync(request.InstituteId, "published", cancellationToken);

        return result;
    }
}
