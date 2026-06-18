using System.DirectoryServices.Protocols;
using System.Net;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth;

/// <summary>
/// Аутентификация через LDAP-bind в Active Directory. Хост приложения в домен не введён,
/// поэтому используется прямой bind по UPN (<see cref="LdapConnection"/> из
/// System.DirectoryServices.Protocols), а не System.DirectoryServices.AccountManagement.
/// После успешного bind читается memberOf и проверяется членство в требуемой группе
/// (<see cref="AdAuthOptions.RequiredGroup"/>).
/// </summary>
public sealed class LdapAuthenticationService(
    IOptions<AdAuthOptions> options,
    ILogger<LdapAuthenticationService> logger) : IAuthenticationService
{
    private readonly AdAuthOptions _options = options.Value;

    public Task<AuthenticatedUser> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
        // Bind и поиск в S.DS.Protocols синхронны — уводим их в пул потоков, не блокируя запрос.
        => Task.Run(() => Authenticate(username, password), cancellationToken);

    private AuthenticatedUser Authenticate(string username, string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new AuthenticationFailedException("Неверный логин или пароль.");

        var account = NormalizeUsername(username);            // "TEST\u" или "u@dom" -> "u"
        var userPrincipalName = $"{account}@{_options.Realm}";

        using var connection = CreateConnection();
        try
        {
            connection.Bind(new NetworkCredential(userPrincipalName, password));
        }
        catch (LdapException ex)
        {
            logger.LogWarning("LDAP bind не удался для {User}: {Message}", account, ex.Message);
            throw new AuthenticationFailedException("Неверный логин или пароль.");
        }

        var entry = FindUser(connection, account)
            ?? throw new AuthenticationFailedException("Учётная запись не найдена в каталоге.");

        var groups = ExtractGroups(entry);
        if (!string.IsNullOrWhiteSpace(_options.RequiredGroup) &&
            !groups.Contains(_options.RequiredGroup, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning("Пользователь {User} не состоит в группе {Group}.", account, _options.RequiredGroup);
            throw new AuthenticationFailedException(
                $"Доступ запрещён: требуется членство в группе «{_options.RequiredGroup}».");
        }

        return new AuthenticatedUser(
            account,
            GetAttribute(entry, "displayName") ?? account,
            GetAttribute(entry, "mail"),
            groups);
    }

    private LdapConnection CreateConnection()
    {
        var connection = new LdapConnection(new LdapDirectoryIdentifier(_options.Host, _options.Port))
        {
            AuthType = AuthType.Basic,
        };
        connection.SessionOptions.ProtocolVersion = 3;

        if (_options.UseSsl)
        {
            connection.SessionOptions.SecureSocketLayer = true;
            if (_options.IgnoreCertificateErrors)
                IgnoreServerCertificate(connection);
        }

        return connection;
    }

    // Принять самоподписанный сертификат DC (только лабораторная среда). На Windows работает
    // управляемый callback; на Linux S.DS.Protocols его не поддерживает (бросает «LDAP server
    // is unavailable»), поэтому отключаем проверку через libldap (LDAPTLS_REQCERT=never).
    private static void IgnoreServerCertificate(LdapConnection connection)
    {
        if (OperatingSystem.IsWindows())
            connection.SessionOptions.VerifyServerCertificate = (_, _) => true;
        else
            Environment.SetEnvironmentVariable("LDAPTLS_REQCERT", "never");
    }

    private SearchResultEntry? FindUser(LdapConnection connection, string account)
    {
        var escaped = EscapeFilter(account);
        var filter = $"(&(objectClass=user)(|(sAMAccountName={escaped})(userPrincipalName={escaped}@{_options.Realm})))";
        var request = new SearchRequest(
            _options.BaseDn, filter, SearchScope.Subtree,
            "displayName", "mail", "memberOf", "sAMAccountName");

        var response = (SearchResponse)connection.SendRequest(request);
        return response.Entries.Count > 0 ? response.Entries[0] : null;
    }

    private static IReadOnlyCollection<string> ExtractGroups(SearchResultEntry entry)
    {
        var attribute = entry.Attributes["memberOf"];
        if (attribute is null)
            return [];

        return attribute.GetValues(typeof(string))
            .Cast<string>()
            .Select(GroupNameFromDn)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();
    }

    // "CN=ScheduleBureau,CN=Users,DC=test,DC=mc" -> "ScheduleBureau".
    private static string GroupNameFromDn(string distinguishedName)
    {
        var first = distinguishedName.Split(',', 2)[0];
        return first.StartsWith("CN=", StringComparison.OrdinalIgnoreCase) ? first[3..] : first;
    }

    private static string? GetAttribute(SearchResultEntry entry, string name)
    {
        var attribute = entry.Attributes[name];
        return attribute is { Count: > 0 } ? attribute[0]?.ToString() : null;
    }

    private static string NormalizeUsername(string username)
    {
        var value = username.Trim();
        var slash = value.IndexOf('\\');
        if (slash >= 0) value = value[(slash + 1)..];   // DOMAIN\user
        var at = value.IndexOf('@');
        if (at >= 0) value = value[..at];               // user@domain
        return value;
    }

    private static string EscapeFilter(string value) => value
        .Replace("\\", "\\5c").Replace("*", "\\2a")
        .Replace("(", "\\28").Replace(")", "\\29").Replace("\0", "\\00");
}
