using Application.Common.DTO.Lessons;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Lessons.Querys;

public class GetLessonByRoomQuery: IRequest<IReadOnlyList<LessonDTO>>
{
    public Guid ClassroomId { get; init; }

    /// <summary>Необязательный фильтр по учебной неделе; null — расписание за весь семестр.</summary>
    public Guid? WeekId { get; init; }
}

public class GetLessonByRoomQueryHandler : IRequestHandler<GetLessonByRoomQuery, IReadOnlyList<LessonDTO>>
{
    private readonly ILessonRepository _lessonRepository;
    public GetLessonByRoomQueryHandler(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }
    public async Task<IReadOnlyList<LessonDTO>> Handle(GetLessonByRoomQuery request, CancellationToken cancellationToken)
    {
        var lessons = await _lessonRepository.GetLessonByRoomAsync(request.ClassroomId, request.WeekId, cancellationToken);
        return lessons?.Select(lesson => new LessonDTO
        {
            Id = lesson.Id,
            ClassroomId = lesson.ClassroomId,
            TimeSlotId = lesson.TimeSlotId,
            StreamId = lesson.StreamId,
            Version = lesson.Version
        }).ToList() ?? new List<LessonDTO>();
    }
}