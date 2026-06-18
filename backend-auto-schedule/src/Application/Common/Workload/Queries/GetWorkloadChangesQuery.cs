using Application.Common.DTO.Workload;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Workload.Queries;

/// <summary>Журнал изменений нагрузки вуза с необязательными фильтрами.</summary>
public sealed class GetWorkloadChangesQuery : IRequest<IReadOnlyList<WorkloadChangeDto>>
{
    public Guid? TeacherId { get; init; }
    public Guid? GroupId { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? SemesterId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public sealed class GetWorkloadChangesQueryHandler
    : IRequestHandler<GetWorkloadChangesQuery, IReadOnlyList<WorkloadChangeDto>>
{
    private readonly IWorkloadLogRepository _repository;

    public GetWorkloadChangesQueryHandler(IWorkloadLogRepository repository)
        => _repository = repository;

    public Task<IReadOnlyList<WorkloadChangeDto>> Handle(
        GetWorkloadChangesQuery request, CancellationToken cancellationToken)
        => _repository.GetChangesAsync(
            new WorkloadChangeFilter(
                request.TeacherId, request.GroupId, request.SubjectId,
                request.SemesterId, request.From, request.To),
            cancellationToken);
}
