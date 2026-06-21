using Application.Common.DTO;
using Application.Common.DTO.Workloads;

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
    /// <summary>
    /// Страница изменений нагрузки, удовлетворяющих фильтру, по убыванию времени.
    /// Семестровый и понедельный журналы объединяются и пагинируются на стороне БД.
    /// </summary>
    Task<PagedResult<WorkloadChangeDto>> GetChangesAsync(
        WorkloadChangeFilter filter, int page, int pageSize, CancellationToken cancellationToken);
}
