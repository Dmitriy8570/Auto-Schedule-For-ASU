namespace Application.Common.Interfaces;

/// <summary>Выпуск JWT для аутентифицированного пользователя.</summary>
public interface ITokenService
{
    AccessToken CreateToken(AuthenticatedUser user);
}

/// <summary>Выданный токен доступа и срок его действия (UTC).</summary>
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
