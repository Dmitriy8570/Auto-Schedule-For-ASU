using Application.Common.DTO.Generation;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Workloads.Queries;

/// <summary>
/// Нераспределённая нагрузка преподавателей за семестр по текущему расписанию
/// (план Hours/2 пар против фактически поставленных занятий). Фильтры опциональны.
/// </summary>
public sealed record GetUnplacedWorkloadQuery(
    Guid SemesterId,
    Guid? InstituteId = null,
    Guid? DepartmentId = null,
    Guid? TeacherId = null) : IRequest<IReadOnlyList<UnplacedWorkloadRow>>;

public sealed class GetUnplacedWorkloadQueryHandler(IWorkloadRepository repo)
    : IRequestHandler<GetUnplacedWorkloadQuery, IReadOnlyList<UnplacedWorkloadRow>>
{
    public Task<IReadOnlyList<UnplacedWorkloadRow>> Handle(
        GetUnplacedWorkloadQuery request, CancellationToken ct)
        => repo.GetUnplacedWorkloadAsync(
            request.SemesterId, request.InstituteId, request.DepartmentId, request.TeacherId, ct);
}
