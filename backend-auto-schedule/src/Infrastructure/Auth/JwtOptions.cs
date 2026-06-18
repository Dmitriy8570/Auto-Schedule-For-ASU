namespace Infrastructure.Auth;

/// <summary>Настройки выпуска и проверки JWT (секция конфигурации "Jwt").</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "auto-schedule";
    public string Audience { get; set; } = "auto-schedule";

    /// <summary>Секрет для подписи HS256. Должен быть длиной не менее 32 байт.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Срок жизни токена в минутах.</summary>
    public int ExpiryMinutes { get; set; } = 480;
}
