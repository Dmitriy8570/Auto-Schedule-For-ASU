using Application.Common.DTO;
using Application.Common.DTO.Generation;
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

    /// <summary>
    /// Нераспределённая нагрузка за семестр по текущему расписанию: для каждой нагрузки сравнивает
    /// план (Hours/2 пар) с фактически поставленными занятиями и возвращает только те, где есть
    /// дефицит. Фильтры по институту/кафедре/преподавателю опциональны. Сортировка по преподавателю,
    /// затем по дисциплине.
    /// </summary>
    Task<IReadOnlyList<UnplacedWorkloadRow>> GetUnplacedWorkloadAsync(
        Guid semesterId,
        Guid? instituteId,
        Guid? departmentId,
        Guid? teacherId,
        CancellationToken ct);
}
