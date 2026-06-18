using Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API;

/// <summary>
/// Преобразует <see cref="AuthenticationFailedException"/> (неверные учётные данные или
/// отсутствие членства в группе) в ответ 401 Unauthorized в формате ProblemDetails.
/// </summary>
public sealed class AuthenticationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not AuthenticationFailedException authException)
            return false;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Не удалось выполнить вход.",
            Detail = authException.Message,
        };

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
