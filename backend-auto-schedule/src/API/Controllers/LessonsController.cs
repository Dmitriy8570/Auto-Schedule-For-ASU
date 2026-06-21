using Application.Common.DTO.Lessons;
using Application.Common.Generation;
using Application.Common.Lessons.Commands;
using Application.Common.Lessons.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LessonsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IScheduleGenerationQueue _generationQueue;

    public LessonsController(IMediator mediator, IScheduleGenerationQueue generationQueue)
    {
        _mediator = mediator;
        _generationQueue = generationQueue;
    }

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

    /// <summary>Получить занятия учебной группы (опционально — за конкретную неделю).</summary>
    [HttpGet("by-group/{groupId:guid}")]
    public async Task<ActionResult<IReadOnlyList<LessonDTO>>> GetByGroup(
        Guid groupId, CancellationToken ct, [FromQuery] Guid? weekId = null)
        => Ok(await _mediator.Send(new GetLessonByGroupQuery { GroupId = groupId, WeekId = weekId }, ct));

    /// <summary>Получить занятия в аудитории (опционально — за конкретную неделю).</summary>
    [HttpGet("by-room/{classroomId:guid}")]
    public async Task<ActionResult<IReadOnlyList<LessonDTO>>> GetByRoom(
        Guid classroomId, CancellationToken ct, [FromQuery] Guid? weekId = null)
        => Ok(await _mediator.Send(new GetLessonByRoomQuery { ClassroomId = classroomId, WeekId = weekId }, ct));

    /// <summary>Получить занятия преподавателя (опционально — за конкретную неделю).</summary>
    [HttpGet("by-teacher/{teacherId:guid}")]
    public async Task<ActionResult<IReadOnlyList<LessonDTO>>> GetByTeacher(
        Guid teacherId, CancellationToken ct, [FromQuery] Guid? weekId = null)
        => Ok(await _mediator.Send(new GetLessonByTeacherQuery { TeacherId = teacherId, WeekId = weekId }, ct));

    /// <summary>Создать занятие.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLessonCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Удалить занятие.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteLessonCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Сгенерировать черновик расписания для одного института на семестр
    /// (с учётом занятости других институтов и якоря к прошлому семестру).
    /// Текущее расписание института при этом заменяется.
    /// </summary>
    [HttpPost("generate/semester/{semesterId:guid}/institute/{instituteId:guid}")]
    public async Task<ActionResult<GenerateScheduleResult>> GenerateForInstitute(
        Guid semesterId, Guid instituteId, CancellationToken ct,
        [FromQuery] double? maxTimeInSeconds = null, [FromQuery] int? searchWorkers = null)
        => Ok(await _mediator.Send(
            new GenerateInstituteScheduleCommand
            {
                SemesterId = semesterId,
                InstituteId = instituteId,
                MaxTimeInSeconds = maxTimeInSeconds,
                SearchWorkers = searchWorkers
            }, ct));

    /// <summary>
    /// Поставить генерацию расписания института в очередь (фоновая, не блокирует HTTP-поток).
    /// Возвращает 202 Accepted и статус задачи; за прогрессом следить через GET generate/status/{jobId}.
    /// </summary>
    [HttpPost("generate/semester/{semesterId:guid}/institute/{instituteId:guid}/async")]
    public ActionResult<GenerationJobStatus> EnqueueGenerateForInstitute(Guid semesterId, Guid instituteId)
    {
        var status = _generationQueue.Enqueue(semesterId, instituteId);
        return AcceptedAtAction(nameof(GetGenerationStatus), new { jobId = status.JobId }, status);
    }

    /// <summary>Статус фоновой задачи генерации расписания.</summary>
    [HttpGet("generate/status/{jobId:guid}")]
    public ActionResult<GenerationJobStatus> GetGenerationStatus(Guid jobId)
        => _generationQueue.TryGetStatus(jobId, out var status) ? Ok(status) : NotFound();

    /// <summary>
    /// Опубликовать расписание института: перевести черновик (Draft) в текущее (Current),
    /// заменив ранее опубликованное расписание.
    /// </summary>
    [HttpPost("publish/institute/{instituteId:guid}")]
    public async Task<ActionResult<PublishInstituteScheduleResult>> PublishForInstitute(
        Guid instituteId, CancellationToken ct)
    {
        try
        {
            return Ok(await _mediator.Send(new PublishInstituteScheduleCommand { InstituteId = instituteId }, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Сбросить черновик расписания института до выгруженного: удалить занятия-черновики (Draft),
    /// оставив опубликованное расписание (Current). Идемпотентно (нет черновика → удалено 0).
    /// </summary>
    [HttpPost("discard/institute/{instituteId:guid}")]
    public async Task<ActionResult<DiscardInstituteScheduleResult>> DiscardForInstitute(
        Guid instituteId, CancellationToken ct)
        => Ok(await _mediator.Send(new DiscardInstituteScheduleCommand { InstituteId = instituteId }, ct));
}
