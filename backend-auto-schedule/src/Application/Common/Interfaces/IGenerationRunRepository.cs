using Domain.schedule;

namespace Application.Common.Interfaces;

/// <summary>Хранилище истории запусков автогенерации расписания.</summary>
public interface IGenerationRunRepository
{
    /// <summary>Сохранить запись о завершённом запуске.</summary>
    Task AddAsync(GenerationRun run, CancellationToken ct);

    /// <summary>
    /// Недавние запуски (по убыванию времени завершения) с необязательными фильтрами по семестру
    /// и институту. <paramref name="limit"/> ограничивает число записей.
    /// </summary>
    Task<IReadOnlyList<GenerationRun>> GetRecentAsync(
        Guid? semesterId, Guid? instituteId, int limit, CancellationToken ct);
}
