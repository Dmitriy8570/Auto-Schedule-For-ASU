using Application.Common.Interfaces;
using Domain.schedule;
using MediatR;

namespace Application.Common.Lessons.Commands;

public class CreateLessonCommand : IRequest<Guid>
{
    public Guid ClassroomId { get; init; }
    public Guid TimeSlotId { get; init; }
    public Guid StreamId { get; init; }
    public Guid SemesterId { get; init; }

    /// <summary>Учебный план (дисциплина/преподаватель/тип), который реализует занятие. Необязателен.</summary>
    public Guid? CurriculumId { get; init; }
}

public class CreateLessonCommandHandler : IRequestHandler<CreateLessonCommand, Guid>
{
    private readonly ILessonRepository _lessonRepository;

    public CreateLessonCommandHandler(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<Guid> Handle(CreateLessonCommand request, CancellationToken cancellationToken)
    {
        var lessonId = Guid.NewGuid();
        var lesson = Lesson.Create(
            lessonId, request.ClassroomId, request.TimeSlotId, request.StreamId, request.SemesterId, request.CurriculumId);
        await _lessonRepository.AddAsync(lesson, cancellationToken);
        await _lessonRepository.SaveChangesAsync(cancellationToken);
        return lessonId;
    }
}
