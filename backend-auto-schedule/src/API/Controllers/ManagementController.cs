using Application.Common.DTO.Lookups;
using Application.Common.DTO.Management;
using Application.Common.Management;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Управление объектами системы (вкладка «Ограничения»): CRUD аудиторий и оборудования,
/// настройка весов мягких ограничений.
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class ManagementController(IMediator mediator) : ControllerBase
{
    // ----- Аудитории -----

    /// <summary>Список аудиторий (опционально по корпусу).</summary>
    [HttpGet("api/classrooms")]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetClassrooms(
        [FromQuery] Guid? buildingId, CancellationToken ct)
        => Ok(await mediator.Send(new GetClassroomsQuery(buildingId), ct));

    /// <summary>Создать аудиторию.</summary>
    [HttpPost("api/classrooms")]
    public async Task<ActionResult<RoomDto>> CreateClassroom(
        [FromBody] CreateClassroomCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>Изменить аудиторию.</summary>
    [HttpPut("api/classrooms/{id:guid}")]
    public async Task<ActionResult<RoomDto>> UpdateClassroom(
        Guid id, [FromBody] UpdateClassroomBody body, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(
                new UpdateClassroomCommand(id, body.Name, body.Capacity, body.BuildingId), ct));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>Удалить аудиторию.</summary>
    [HttpDelete("api/classrooms/{id:guid}")]
    public async Task<IActionResult> DeleteClassroom(Guid id, CancellationToken ct)
    {
        try { await mediator.Send(new DeleteClassroomCommand(id), ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ----- Оборудование -----

    /// <summary>Список типов оборудования.</summary>
    [HttpGet("api/equipments")]
    public async Task<ActionResult<IReadOnlyList<EquipmentDto>>> GetEquipments(CancellationToken ct)
        => Ok(await mediator.Send(new GetEquipmentsQuery(), ct));

    /// <summary>Создать тип оборудования.</summary>
    [HttpPost("api/equipments")]
    public async Task<ActionResult<EquipmentDto>> CreateEquipment(
        [FromBody] CreateEquipmentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>Переименовать тип оборудования.</summary>
    [HttpPut("api/equipments/{id:guid}")]
    public async Task<ActionResult<EquipmentDto>> UpdateEquipment(
        Guid id, [FromBody] NameBody body, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(new UpdateEquipmentCommand(id, body.Name), ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>Удалить тип оборудования.</summary>
    [HttpDelete("api/equipments/{id:guid}")]
    public async Task<IActionResult> DeleteEquipment(Guid id, CancellationToken ct)
    {
        try { await mediator.Send(new DeleteEquipmentCommand(id), ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ----- Веса мягких ограничений -----

    /// <summary>Текущие веса мягких ограничений.</summary>
    [HttpGet("api/constraints")]
    public async Task<ActionResult<IReadOnlyList<ConstraintConfigDto>>> GetConstraints(CancellationToken ct)
        => Ok(await mediator.Send(new GetConstraintsQuery(), ct));

    /// <summary>Изменить вес мягкого ограничения.</summary>
    [HttpPut("api/constraints/{id:guid}")]
    public async Task<ActionResult<ConstraintConfigDto>> UpdateConstraint(
        Guid id, [FromBody] PenaltyBody body, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(new UpdateConstraintCommand(id, body.Penalty), ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ----- Тела запросов (id берётся из маршрута) -----
    public sealed record UpdateClassroomBody(string Name, int Capacity, Guid BuildingId);
    public sealed record NameBody(string Name);
    public sealed record PenaltyBody(int Penalty);
}
