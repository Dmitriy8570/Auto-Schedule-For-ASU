using System.Text.Json;
using Application.Common.DTO.Generation;
using Application.Common.Interfaces;
using Application.Common.Lessons.Commands;
using MediatR;

namespace Application.Common.Generation.Queries;

/// <summary>История запусков автогенерации (по убыванию времени завершения), с фильтрами.</summary>
public sealed record GetGenerationHistoryQuery(
    Guid? SemesterId = null,
    Guid? InstituteId = null,
    int Limit = 100) : IRequest<IReadOnlyList<GenerationRunDto>>;

public sealed class GetGenerationHistoryQueryHandler(IGenerationRunRepository repo)
    : IRequestHandler<GetGenerationHistoryQuery, IReadOnlyList<GenerationRunDto>>
{
    public async Task<IReadOnlyList<GenerationRunDto>> Handle(
        GetGenerationHistoryQuery request, CancellationToken ct)
    {
        var limit = request.Limit is < 1 or > 500 ? 100 : request.Limit;
        var runs = await repo.GetRecentAsync(request.SemesterId, request.InstituteId, limit, ct);

        return runs.Select(r => new GenerationRunDto(
            r.Id, r.SemesterId, r.SemesterName, r.InstituteId, r.InstituteName,
            r.Succeeded, r.Status, r.LessonsCreated, r.ObjectiveValue, r.WallTimeSeconds,
            r.UnplacedCount, Deserialize(r.UnplacedJson), r.Error,
            r.CreatedAt, r.CompletedAt)).ToList();
    }

    private static IReadOnlyList<WorkloadShortfall> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<WorkloadShortfall>();
        try
        {
            return JsonSerializer.Deserialize<List<WorkloadShortfall>>(json)
                ?? (IReadOnlyList<WorkloadShortfall>)Array.Empty<WorkloadShortfall>();
        }
        catch (JsonException)
        {
            return Array.Empty<WorkloadShortfall>();
        }
    }
}
