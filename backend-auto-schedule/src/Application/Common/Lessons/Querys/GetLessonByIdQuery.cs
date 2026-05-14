using Application.Common.DTO.Lessons;
using Application.Common.Interfaces;
using Domain.schedule;
using MediatR;

namespace Application.Common.Lessons.Querys;

public class GetLessonByIdQuery: IRequest<LessonDTO>
{
    public Guid Id { get; init; }
}

public class GetLessonByIdQueryHandler : IRequestHandler<GetLessonByIdQuery, LessonDTO>
{
    private readonly ILessonRepository _lessonRepository;
    public GetLessonByIdQueryHandler(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }
    public async Task<LessonDTO> Handle(GetLessonByIdQuery request, CancellationToken cancellationToken)
    {
        var lesson = await _lessonRepository.GetLessonByIdAsync(request.Id, cancellationToken)
        ?? throw new KeyNotFoundException();

        return new LessonDTO
        {
            Id = lesson.Id,
            ClassroomId = lesson.ClassroomId,
            TimeSlotId = lesson.TimeSlotId,
            StreamId = lesson.StreamId,
            Version = lesson.Version
        };
    }
}