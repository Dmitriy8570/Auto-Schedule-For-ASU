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

    public PublishInstituteScheduleCommandHandler(ILessonRepository lessonRepository)
        => _lessonRepository = lessonRepository;

    public async Task<PublishInstituteScheduleResult> Handle(
        PublishInstituteScheduleCommand request, CancellationToken cancellationToken)
    {
        var lessons = await _lessonRepository.GetByInstituteAsync(request.InstituteId, cancellationToken);

        var drafts = lessons.Where(l => l.Version == ScheduleVersion.Draft).ToList();
        if (drafts.Count == 0)
            throw new KeyNotFoundException("Черновик расписания института отсутствует.");

        // Старое опубликованное расписание заменяется черновиком.
        var currentlyPublished = lessons.Where(l => l.Version == ScheduleVersion.Current);
        _lessonRepository.RemoveRange(currentlyPublished);

        foreach (var lesson in drafts)
            lesson.Publish();

        await _lessonRepository.SaveChangesAsync(cancellationToken);

        return new PublishInstituteScheduleResult(drafts.Count);
    }
}
