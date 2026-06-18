namespace Application.Common.Interfaces;

/// <summary>
/// Проверка учётных данных пользователя в Active Directory (LDAP-bind) и контроль
/// членства в требуемой группе доступа. Бросает
/// <see cref="Application.Common.Exceptions.AuthenticationFailedException"/>,
/// если логин/пароль неверны либо пользователь не состоит в нужной группе.
/// </summary>
public interface IAuthenticationService
{
    Task<AuthenticatedUser> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
}

/// <summary>Аутентифицированный пользователь домена.</summary>
public sealed record AuthenticatedUser(
    string Username,
    string DisplayName,
    string? Email,
    IReadOnlyCollection<string> Groups);
