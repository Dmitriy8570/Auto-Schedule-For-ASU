using Application.Common.DTO.Lessons;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Lessons.Querys;

public class GetLessonByTeacherQuery : IRequest<IReadOnlyList<LessonDTO>>
{
    public Guid TeacherId { get; init; }
}

public class GetLessonByTeacherQueryHandler : IRequestHandler<GetLessonByTeacherQuery, IReadOnlyList<LessonDTO>>
{
    private readonly ILessonRepository _lessonRepository;
    public GetLessonByTeacherQueryHandler(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }
    public async Task<IReadOnlyList<LessonDTO>> Handle(GetLessonByTeacherQuery request, CancellationToken cancellationToken)
    {
        var lessons = await _lessonRepository.GetLessonByTeacherAsync(request.TeacherId, cancellationToken);
        return lessons?.Select(lesson => new LessonDTO
        {
            Id = lesson.Id,
            ClassroomId = lesson.ClassroomId,
            TimeSlotId = lesson.TimeSlotId,
            StreamId = lesson.StreamId
        }).ToList() ?? new List<LessonDTO>();
    }
}

