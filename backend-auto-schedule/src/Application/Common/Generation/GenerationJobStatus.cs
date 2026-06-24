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
    Failed,
    /// <summary>Отменена пользователем (уже сформированные недели сохранены).</summary>
    Cancelled
}

/// <summary>
/// Снимок состояния фоновой задачи понедельной генерации расписания.
/// <see cref="InstituteId"/> == <c>null</c> — генерация по всему университету.
/// <see cref="Progress"/> заполняется по мере прохождения недель.
/// </summary>
public sealed record GenerationJobStatus(
    Guid JobId,
    GenerationJobState State,
    Guid SemesterId,
    Guid? InstituteId,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    GenerateScheduleResult? Result,
    string? Error,
    GenerationProgress? Progress = null);
