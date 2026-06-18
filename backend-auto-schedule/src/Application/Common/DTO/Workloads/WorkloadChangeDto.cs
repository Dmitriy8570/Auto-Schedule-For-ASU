using Domain.workload.logs;

namespace Application.Common.DTO.Workloads;

/// <summary>
/// Запись журнала изменений нагрузки вуза (объединяет семестровые и понедельные журналы).
/// </summary>
public sealed class WorkloadChangeDto
{
    /// <summary>Тип операции: 0 Add, 1 Update, 2 Delete.</summary>
    public LogAction Action { get; init; }

    /// <summary>Часов до изменения.</summary>
    public int OldValue { get; init; }

    /// <summary>Часов после изменения.</summary>
    public int NewValue { get; init; }

    public DateTime TimeStamp { get; init; }

    /// <summary>Уровень нагрузки: "Semester" или "Week".</summary>
    public string Scope { get; init; } = string.Empty;

    public Guid CurriculumId { get; init; }
    public Guid TeacherId { get; init; }
    public Guid SubjectId { get; init; }
    public Guid StreamId { get; init; }
}
