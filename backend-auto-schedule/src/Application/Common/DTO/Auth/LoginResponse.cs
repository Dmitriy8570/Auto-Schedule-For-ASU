namespace Application.Common.DTO.Auth;

/// <summary>Результат успешного входа: токен доступа и сведения о пользователе.</summary>
public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    string Username,
    string DisplayName,
    IReadOnlyCollection<string> Groups);
