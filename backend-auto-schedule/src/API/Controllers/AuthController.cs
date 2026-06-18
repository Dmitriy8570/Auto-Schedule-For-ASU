using System.Security.Claims;
using Application.Common.Auth.Commands;
using Application.Common.DTO.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Аутентификация: вход по доменной учётной записи Active Directory.</summary>
[ApiController]
[Produces("application/json")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Вход. Возвращает JWT при верных учётных данных и членстве в требуемой группе доступа;
    /// иначе — 401.
    /// </summary>
    [HttpPost("api/auth/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>Сведения о текущем пользователе, извлечённые из токена.</summary>
    [HttpGet("api/auth/me")]
    [Authorize]
    public ActionResult Me() => Ok(new
    {
        Username = User.Identity?.Name,
        DisplayName = User.FindFirstValue("displayName"),
        Email = User.FindFirstValue(ClaimTypes.Email),
        Groups = User.FindAll(ClaimTypes.Role).Select(c => c.Value),
    });
}
