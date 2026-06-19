using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Lessons.Commands;

/// <summary>Удалить занятие из расписания.</summary>
public sealed record DeleteLessonCommand(Guid Id) : IRequest;

public sealed class DeleteLessonCommandHandler(ILessonRepository lessonRepository)
    : IRequestHandler<DeleteLessonCommand>
{
    public async Task Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
    {
        var removed = await lessonRepository.DeleteAsync(request.Id, cancellationToken);
        if (!removed)
            throw new KeyNotFoundException();
    }
}
