using Application.Common.Lessons.Commands;

namespace Application.Common.Generation;

/// <summary>Состояние фоновой задачи генерации расписания.</summary>
public enum GenerationJobState
{
    /// <summary>Поставлена в очередь, ещё не выполняется.</summary>
    Queued,
    /// <summary>Солвер работает.</summary>
    Running,
    /// <summary>Завершена успешно (см. <see cref="GenerationJobStatus.Result"/>).</summary>
    Succeeded,
    /// <summary>Завершена ошибкой (см. <see cref="GenerationJobStatus.Error"/>).</summary>
    Failed
}

/// <summary>Снимок состояния фоновой задачи генерации расписания института.</summary>
public sealed record GenerationJobStatus(
    Guid JobId,
    GenerationJobState State,
    Guid SemesterId,
    Guid InstituteId,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    GenerateScheduleResult? Result,
    string? Error);
