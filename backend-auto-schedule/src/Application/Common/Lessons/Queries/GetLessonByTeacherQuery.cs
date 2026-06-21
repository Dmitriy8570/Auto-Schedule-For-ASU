using Application.Common.DTO.Lessons;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Lessons.Queries;

public class GetLessonByTeacherQuery : IRequest<IReadOnlyList<LessonDTO>>
{
    public Guid TeacherId { get; init; }

    /// <summary>Необязательный фильтр по учебной неделе; null — расписание за весь семестр.</summary>
    public Guid? WeekId { get; init; }
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
        var lessons = await _lessonRepository.GetLessonByTeacherAsync(request.TeacherId, request.WeekId, cancellationToken);
        return lessons?.Select(LessonDTO.From).ToList() ?? new List<LessonDTO>();
    }
}

