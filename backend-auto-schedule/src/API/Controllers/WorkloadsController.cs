using Application.Common.DTO;
using Application.Common.DTO.Workloads;
using Application.Common.Workloads.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Учебная нагрузка (учебный план): кто ведёт какую дисциплину какой группе, в каком формате,
/// с часами за семестр и по неделям. Фильтры опциональны, вывод постраничный.
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class WorkloadsController(IMediator mediator) : ControllerBase
{
    /// <summary>Учебная нагрузка с фильтрами и пагинацией (сортировка по преподавателю, затем по дисциплине).</summary>
    [HttpGet("api/workloads")]
    public async Task<ActionResult<PagedResult<WorkloadItemDto>>> GetWorkloads(
        [FromQuery] Guid? instituteId, [FromQuery] Guid? departmentId, [FromQuery] Guid? teacherId,
        [FromQuery] string? subjectSearch, CancellationToken ct,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(
            new GetWorkloadsQuery(instituteId, departmentId, teacherId, subjectSearch, page, pageSize), ct));
}
