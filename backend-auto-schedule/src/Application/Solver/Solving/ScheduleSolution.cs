namespace Application.Solver.Solving;

/// <summary>Статус решения задачи составления расписания.</summary>
public enum ScheduleSolveStatus
{
    /// <summary>Найдено оптимальное решение.</summary>
    Optimal,
    /// <summary>Найдено допустимое (но не доказанно оптимальное) решение.</summary>
    Feasible,
    /// <summary>Доказано, что решения не существует.</summary>
    Infeasible,
    /// <summary>Решение не найдено за отведённое время либо модель некорректна.</summary>
    Unknown
}

/// <summary>Результат запуска солвера: статус, метрики и извлечённые назначения.</summary>
public sealed record ScheduleSolution(
    ScheduleSolveStatus Status,
    IReadOnlyList<Assignment> Assignments,
    double ObjectiveValue,
    double WallTimeSeconds)
{
    /// <summary>Найдено ли пригодное решение (оптимальное или допустимое).</summary>
    public bool IsSuccess => Status is ScheduleSolveStatus.Optimal or ScheduleSolveStatus.Feasible;
}
