using Application.Common.DTO.Lessons;
using Application.Common.Lessons.Commands;
using Application.Common.Lessons.Querys;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LessonsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LessonsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Получить занятие по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var lesson = await _mediator.Send(new GetLessonByIdQuery { Id = id }, ct);
            return Ok(lesson);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Получить занятия учебной группы.</summary>
    [HttpGet("by-group/{groupId:guid}")]
    public async Task<ActionResult<IReadOnlyList<LessonDTO>>> GetByGroup(Guid groupId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetLessonByGroupQuery { GroupId = groupId }, ct));

    /// <summary>Получить занятия в аудитории.</summary>
    [HttpGet("by-room/{classroomId:guid}")]
    public async Task<ActionResult<IReadOnlyList<LessonDTO>>> GetByRoom(Guid classroomId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetLessonByRoomQuery { ClassroomId = classroomId }, ct));

    /// <summary>Получить занятия преподавателя.</summary>
    [HttpGet("by-teacher/{teacherId:guid}")]
    public async Task<ActionResult<IReadOnlyList<LessonDTO>>> GetByTeacher(Guid teacherId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetLessonByTeacherQuery { TeacherId = teacherId }, ct));

    /// <summary>Создать занятие.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLessonCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Сгенерировать черновик расписания для одного института на семестр
    /// (с учётом занятости других институтов и якоря к прошлому семестру).
    /// Текущее расписание института при этом заменяется.
    /// </summary>
    [HttpPost("generate/semester/{semesterId:guid}/institute/{instituteId:guid}")]
    public async Task<ActionResult<GenerateScheduleResult>> GenerateForInstitute(
        Guid semesterId, Guid instituteId, CancellationToken ct)
        => Ok(await _mediator.Send(
            new GenerateInstituteScheduleCommand { SemesterId = semesterId, InstituteId = instituteId }, ct));
}
