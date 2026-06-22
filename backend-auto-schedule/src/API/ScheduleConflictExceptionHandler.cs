using Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API;

/// <summary>
/// Преобразует <see cref="ScheduleConflictException"/> (ручное добавление занятия нарушает
/// жёсткие ограничения) в ответ 409 Conflict в формате ProblemDetails с перечнем коллизий.
/// </summary>
public class ScheduleConflictExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ScheduleConflictException conflictException)
            return false;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Коллизия расписания.",
            Detail = conflictException.Message
        };
        problemDetails.Extensions["conflicts"] = conflictException.Conflicts
            .Select(c => new { kind = c.Kind.ToString(), detail = c.Detail })
            .ToArray();

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
