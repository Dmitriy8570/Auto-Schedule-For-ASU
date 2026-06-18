using Application.Common.DTO;
using Application.Common.DTO.Workloads;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Workloads.Queries;

/// <summary>Запрос учебной нагрузки с фильтрами и пагинацией.</summary>
public sealed record GetWorkloadsQuery(
    Guid? InstituteId,
    Guid? DepartmentId,
    Guid? TeacherId,
    string? SubjectSearch,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<WorkloadItemDto>>;

public sealed class GetWorkloadsQueryHandler(IWorkloadRepository repo)
    : IRequestHandler<GetWorkloadsQuery, PagedResult<WorkloadItemDto>>
{
    public Task<PagedResult<WorkloadItemDto>> Handle(GetWorkloadsQuery request, CancellationToken ct)
        => repo.GetWorkloadsAsync(
            request.InstituteId, request.DepartmentId, request.TeacherId,
            request.SubjectSearch, request.Page, request.PageSize, ct);
}
