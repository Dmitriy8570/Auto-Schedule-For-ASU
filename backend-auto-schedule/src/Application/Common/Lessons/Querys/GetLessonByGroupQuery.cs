using Application.Common.DTO.Lessons;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Lessons.Querys;

public class GetLessonByGroupQuery: IRequest<IReadOnlyList<LessonDTO>>
{
    public Guid GroupId { get; init; }
}

public class GetLessonByGroupQueryHandler : IRequestHandler<GetLessonByGroupQuery, IReadOnlyList<LessonDTO>>
{
    private readonly ILessonRepository _lessonRepository;
    public GetLessonByGroupQueryHandler(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }
    public async Task<IReadOnlyList<LessonDTO>> Handle(GetLessonByGroupQuery request, CancellationToken cancellationToken)
    {
        var lessons = await _lessonRepository.GetLessonByGroupAsync(request.GroupId, cancellationToken);
        return lessons?.Select(lesson => new LessonDTO
        {
            Id = lesson.Id,
            ClassroomId = lesson.ClassroomId,
            TimeSlotId = lesson.TimeSlotId,
            StreamId = lesson.StreamId
        }).ToList() ?? new List<LessonDTO>();
    }
}