using Application.Common.Calendar.Queries;
using Application.Common.DTO.Calendar;
using Application.Common.DTO.Schedule;
using Application.Common.Schedule.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Календарь расписания: список семестров и недель семестра.
/// Нужен для выбора периода (семестр + неделя) в сетке расписания.
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class CalendarController(IMediator mediator) : ControllerBase
{
    /// <summary>Список семестров (свежие сверху), с пометкой текущего.</summary>
    [HttpGet("api/semesters")]
    public async Task<ActionResult<IReadOnlyList<SemesterDto>>> GetSemesters(CancellationToken ct)
        => Ok(await mediator.Send(new GetSemestersQuery(), ct));

    /// <summary>Недели семестра с порядковым номером и типом (Red/Blue).</summary>
    [HttpGet("api/weeks")]
    public async Task<ActionResult<IReadOnlyList<WeekDto>>> GetWeeks(
        [FromQuery] Guid semesterId, CancellationToken ct)
        => Ok(await mediator.Send(new GetWeeksQuery(semesterId), ct));

    /// <summary>Временные слоты недели (для сопоставления ячейки сетки с TimeSlotId при добавлении пары).</summary>
    [HttpGet("api/timeslots")]
    public async Task<ActionResult<IReadOnlyList<TimeSlotDto>>> GetTimeSlots(
        [FromQuery] Guid weekId, CancellationToken ct)
        => Ok(await mediator.Send(new GetTimeSlotsQuery(weekId), ct));

    /// <summary>Учебные планы (дисциплина/преподаватель/тип) для формы добавления пары.</summary>
    [HttpGet("api/curriculums")]
    public async Task<ActionResult<IReadOnlyList<CurriculumOptionDto>>> GetCurriculums(
        [FromQuery] Guid? groupId, [FromQuery] Guid? teacherId, [FromQuery] Guid? semesterId,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetCurriculumsQuery(groupId, teacherId, semesterId), ct));
}
