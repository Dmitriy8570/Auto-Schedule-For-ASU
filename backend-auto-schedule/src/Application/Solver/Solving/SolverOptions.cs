using System.Globalization;

namespace Application.Solver.Solving;

/// <summary>Параметры запуска CP-SAT солвера.</summary>
public sealed record SolverOptions
{
    /// <summary>Максимальное время поиска, секунды.</summary>
    public double MaxTimeInSeconds { get; init; } = 180;

    /// <summary>Число параллельных воркеров поиска.</summary>
    public int SearchWorkers { get; init; } = 8;

    /// <summary>Логировать прогресс поиска в stdout солвера.</summary>
    public bool LogSearchProgress { get; init; } = false;

    public static SolverOptions Default => new();

    /// <summary>Сериализация в формат StringParameters OR-Tools.</summary>
    public string ToStringParameters() =>
        $"max_time_in_seconds:{MaxTimeInSeconds.ToString(CultureInfo.InvariantCulture)};" +
        $"num_search_workers:{SearchWorkers};" +
        $"log_search_progress:{(LogSearchProgress ? "true" : "false")}";
}
