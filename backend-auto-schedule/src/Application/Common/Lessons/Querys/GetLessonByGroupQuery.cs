using Application.Common.DTO.Lessons;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Lessons.Querys;

public class GetLessonByGroupQuery: IRequest<IReadOnlyList<LessonDTO>>
{
    public Guid GroupId { get; init; }

    /// <summary>Необязательный фильтр по учебной неделе; null — расписание за весь семестр.</summary>
    public Guid? WeekId { get; init; }
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
        var lessons = await _lessonRepository.GetLessonByGroupAsync(request.GroupId, request.WeekId, cancellationToken);
        return lessons?.Select(LessonDTO.From).ToList() ?? new List<LessonDTO>();
    }
}