using Application.Common.DTO.Workload;

namespace Application.Common.Interfaces;

/// <summary>Набор фильтров журнала изменений нагрузки; все поля необязательны и комбинируются по «И».</summary>
public sealed record WorkloadChangeFilter(
    Guid? TeacherId = null,
    Guid? GroupId = null,
    Guid? SubjectId = null,
    Guid? SemesterId = null,
    DateTime? From = null,
    DateTime? To = null);

/// <summary>Доступ к журналам изменений семестровой и понедельной нагрузки.</summary>
public interface IWorkloadLogRepository
{
    /// <summary>Изменения нагрузки, удовлетворяющие фильтру, по убыванию времени.</summary>
    Task<IReadOnlyList<WorkloadChangeDto>> GetChangesAsync(
        WorkloadChangeFilter filter, CancellationToken cancellationToken);
}
