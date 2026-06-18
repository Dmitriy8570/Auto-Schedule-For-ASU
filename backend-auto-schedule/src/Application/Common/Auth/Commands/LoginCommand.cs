using Application.Common.DTO.Auth;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Auth.Commands;

/// <summary>Вход по доменной учётной записи (логин + пароль Active Directory).</summary>
public sealed record LoginCommand(string Username, string Password) : IRequest<LoginResponse>;

public sealed class LoginCommandHandler(
    IAuthenticationService authentication,
    ITokenService tokens) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await authentication.AuthenticateAsync(request.Username, request.Password, ct);
        var token = tokens.CreateToken(user);
        return new LoginResponse(token.Value, token.ExpiresAtUtc, user.Username, user.DisplayName, user.Groups);
    }
}
