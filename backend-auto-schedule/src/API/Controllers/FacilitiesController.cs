using Application.Common.DTO.Lookups;
using Application.Common.Lookups.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Справочники корпусов и аудиторий для выбора занятости аудитории.</summary>
[ApiController]
[Produces("application/json")]
public sealed class FacilitiesController(IMediator mediator) : ControllerBase
{
    /// <summary>Учебные корпуса.</summary>
    [HttpGet("api/buildings")]
    public async Task<ActionResult<IReadOnlyList<BuildingDto>>> GetBuildings(
        [FromQuery] string? search, CancellationToken ct)
        => Ok(await mediator.Send(new GetBuildingsQuery(search), ct));

    /// <summary>Аудитории — конечный выбор для занятости аудитории.</summary>
    [HttpGet("api/rooms")]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetRooms(
        [FromQuery] Guid? buildingId, [FromQuery] string? search, CancellationToken ct)
        => Ok(await mediator.Send(new GetRoomsQuery(buildingId, search), ct));
}
