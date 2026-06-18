using Application.Common.DTO.Workload;
using Application.Common.Workload.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WorkloadController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkloadController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Журнал изменений нагрузки вуза с необязательными фильтрами
    /// (преподаватель, группа, предмет, семестр, диапазон дат). Сортировка по времени по убыванию.
    /// </summary>
    [HttpGet("changes")]
    public async Task<ActionResult<IReadOnlyList<WorkloadChangeDto>>> GetChanges(
        CancellationToken ct,
        [FromQuery] Guid? teacherId = null,
        [FromQuery] Guid? groupId = null,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] Guid? semesterId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
        => Ok(await _mediator.Send(new GetWorkloadChangesQuery
        {
            TeacherId = teacherId,
            GroupId = groupId,
            SubjectId = subjectId,
            SemesterId = semesterId,
            From = from,
            To = to
        }, ct));
}
