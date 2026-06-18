using Application.Common.DTO;
using Application.Common.DTO.Workloads;

namespace Application.Common.Interfaces;

/// <summary>
/// Чтение учебной нагрузки (учебного плана) с фильтрами и пагинацией.
/// Все фильтры опциональны; результат отсортирован по ФИО преподавателя, затем по дисциплине.
/// </summary>
public interface IWorkloadRepository
{
    Task<PagedResult<WorkloadItemDto>> GetWorkloadsAsync(
        Guid? instituteId,
        Guid? departmentId,
        Guid? teacherId,
        string? subjectSearch,
        int page,
        int pageSize,
        CancellationToken ct);
}
