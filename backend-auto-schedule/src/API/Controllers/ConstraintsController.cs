using Application.Common.Constraints;
using Application.Common.DTO.Constraints;
using Domain.university.groups;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Конфигурация ограничений солвера (вкладка «Ограничения»): сетки доступности преподавателей и
/// аудиторий, по-нагрузочные правила учебных планов и смена групп. Все изменения подхватываются
/// при следующей генерации расписания.
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class ConstraintsController(IMediator mediator) : ControllerBase
{
    // ----- Доступность преподавателя -----

    /// <summary>Сетка доступности преподавателя (возвращаются только не-нейтральные слоты).</summary>
    [HttpGet("api/availability/teacher/{teacherId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AvailabilityCellDto>>> GetTeacherAvailability(
        Guid teacherId, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(new GetTeacherAvailabilityQuery(teacherId), ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>Полностью заменить сетку доступности преподавателя.</summary>
    [HttpPut("api/availability/teacher/{teacherId:guid}")]
    public async Task<IActionResult> SetTeacherAvailability(
        Guid teacherId, [FromBody] IReadOnlyList<AvailabilityCellDto> cells, CancellationToken ct)
    {
        try { await mediator.Send(new SetTeacherAvailabilityCommand(teacherId, cells), ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ----- Доступность аудитории -----

    /// <summary>Сетка доступности аудитории (возвращаются только не-нейтральные слоты).</summary>
    [HttpGet("api/availability/classroom/{classroomId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AvailabilityCellDto>>> GetClassroomAvailability(
        Guid classroomId, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(new GetClassroomAvailabilityQuery(classroomId), ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>Полностью заменить сетку доступности аудитории.</summary>
    [HttpPut("api/availability/classroom/{classroomId:guid}")]
    public async Task<IActionResult> SetClassroomAvailability(
        Guid classroomId, [FromBody] IReadOnlyList<AvailabilityCellDto> cells, CancellationToken ct)
    {
        try { await mediator.Send(new SetClassroomAvailabilityCommand(classroomId, cells), ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ----- По-нагрузочные ограничения учебного плана -----

    /// <summary>По-нагрузочные ограничения учебного плана.</summary>
    [HttpGet("api/curriculums/{id:guid}/constraints")]
    public async Task<ActionResult<CurriculumConstraintsDto>> GetCurriculumConstraints(
        Guid id, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(new GetCurriculumConstraintsQuery(id), ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>Изменить по-нагрузочные ограничения учебного плана.</summary>
    [HttpPut("api/curriculums/{id:guid}/constraints")]
    public async Task<IActionResult> SetCurriculumConstraints(
        Guid id, [FromBody] CurriculumConstraintsDto body, CancellationToken ct)
    {
        try { await mediator.Send(new SetCurriculumConstraintsCommand(id, body), ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ----- Смена группы -----

    /// <summary>Изменить смену обучения группы.</summary>
    [HttpPut("api/groups/{id:guid}/shift")]
    public async Task<IActionResult> SetGroupShift(
        Guid id, [FromBody] ShiftBody body, CancellationToken ct)
    {
        try { await mediator.Send(new SetGroupShiftCommand(id, body.Shift), ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>Тело запроса смены группы (id берётся из маршрута).</summary>
    public sealed record ShiftBody(Shift Shift);
}
