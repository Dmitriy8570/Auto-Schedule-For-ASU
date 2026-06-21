using Application.Common.DTO;
using Application.Common.DTO.Workloads;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Workloads.Queries;

/// <summary>Журнал изменений нагрузки вуза с необязательными фильтрами и пагинацией.</summary>
public sealed class GetWorkloadChangesQuery : IRequest<PagedResult<WorkloadChangeDto>>
{
    public Guid? TeacherId { get; init; }
    public Guid? GroupId { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? SemesterId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class GetWorkloadChangesQueryHandler
    : IRequestHandler<GetWorkloadChangesQuery, PagedResult<WorkloadChangeDto>>
{
    private readonly IWorkloadLogRepository _repository;

    public GetWorkloadChangesQueryHandler(IWorkloadLogRepository repository)
        => _repository = repository;

    public Task<PagedResult<WorkloadChangeDto>> Handle(
        GetWorkloadChangesQuery request, CancellationToken cancellationToken)
        => _repository.GetChangesAsync(
            new WorkloadChangeFilter(
                request.TeacherId, request.GroupId, request.SubjectId,
                request.SemesterId, request.From, request.To),
            request.Page, request.PageSize, cancellationToken);
}
