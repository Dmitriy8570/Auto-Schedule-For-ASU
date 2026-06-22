using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Lessons.Queries;

/// <summary>
/// Неблокирующие предупреждения для занятия (переходы между корпусами в соседних парах).
/// Запрашивается после ручного добавления/изменения пары; на саму операцию не влияет.
/// </summary>
public sealed record GetLessonWarningsQuery(Guid LessonId) : IRequest<IReadOnlyList<string>>;

public sealed class GetLessonWarningsQueryHandler(ILessonRepository repo)
    : IRequestHandler<GetLessonWarningsQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(GetLessonWarningsQuery request, CancellationToken ct)
        => repo.GetBuildingTravelWarningsAsync(request.LessonId, ct);
}
