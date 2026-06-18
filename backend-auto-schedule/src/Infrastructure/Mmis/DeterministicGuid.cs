using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Mmis;

/// <summary>
/// Детерминированные GUID (RFC 4122, версия 5 — name-based SHA-1) для сопоставления
/// целочисленных идентификаторов MMIS с Guid-ключами Auto-Schedule. Один и тот же
/// (тип сущности, MMIS Id) всегда даёт один и тот же Guid, поэтому повторная
/// синхронизация идемпотентна: строки находятся по ключу и сравниваются, а не дублируются.
/// </summary>
public static class DeterministicGuid
{
    // Фиксированный namespace для всего проекта (любой статичный GUID).
    private static readonly Guid Namespace = new("4f3a1d2e-7c6b-4a59-9e8d-2b1c0a9f8e7d");

    /// <summary>Guid для сущности типа <paramref name="entityType"/> с исходным идентификатором <paramref name="mmisId"/>.</summary>
    public static Guid For(string entityType, int mmisId) => Create($"{entityType}:{mmisId}");

    private static Guid Create(string name)
    {
        var nsBytes = Namespace.ToByteArray();
        SwapByteOrder(nsBytes); // .NET хранит первые три группы в little-endian — приводим к big-endian (RFC).

        var nameBytes = Encoding.UTF8.GetBytes(name);

        byte[] hash;
        using (var sha1 = SHA1.Create())
        {
            sha1.TransformBlock(nsBytes, 0, nsBytes.Length, null, 0);
            sha1.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
            hash = sha1.Hash!;
        }

        var result = new byte[16];
        Array.Copy(hash, 0, result, 0, 16);

        result[6] = (byte)((result[6] & 0x0F) | 0x50); // версия 5
        result[8] = (byte)((result[8] & 0x3F) | 0x80); // вариант RFC 4122

        SwapByteOrder(result); // обратно в порядок .NET
        return new Guid(result);
    }

    private static void SwapByteOrder(byte[] guid)
    {
        (guid[0], guid[3]) = (guid[3], guid[0]);
        (guid[1], guid[2]) = (guid[2], guid[1]);
        (guid[4], guid[5]) = (guid[5], guid[4]);
        (guid[6], guid[7]) = (guid[7], guid[6]);
    }
}
