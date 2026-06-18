namespace Infrastructure.Auth;

/// <summary>Настройки подключения к Active Directory (секция конфигурации "ActiveDirectory").</summary>
public sealed class AdAuthOptions
{
    public const string SectionName = "ActiveDirectory";

    /// <summary>Хост контроллера домена (например, "ad-dc" внутри сети compose).</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Порт LDAP. 636 — LDAPS, 389 — обычный LDAP.</summary>
    public int Port { get; set; } = 636;

    /// <summary>Использовать TLS (LDAPS). Samba отклоняет simple bind по незашифрованному каналу.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Не проверять серверный сертификат — для самоподписанного DC в лабораторной среде.</summary>
    public bool IgnoreCertificateErrors { get; set; }

    /// <summary>Realm (UPN-суффикс), например "TEST.MC". Логин превращается в "user@Realm".</summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>База поиска пользователей, например "DC=test,DC=mc".</summary>
    public string BaseDn { get; set; } = string.Empty;

    /// <summary>
    /// Требуемая группа доступа (имя CN/samAccountName, например "ScheduleBureau").
    /// Если пусто — проверка членства не выполняется.
    /// </summary>
    public string RequiredGroup { get; set; } = string.Empty;
}
