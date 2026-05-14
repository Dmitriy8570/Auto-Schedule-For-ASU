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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLessonsById(Guid id, CancellationToken ct)
    {
        try
        {
            var query = new GetLessonByIdQuery { Id = id };
            var lesson = await _mediator.Send(query, ct);
            return Ok(lesson);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}