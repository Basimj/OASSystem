using System.Security.Cryptography;
using System.Text;

namespace OAS.API.Security;

/// <summary>
/// Produces a non-reversible credential version from the stored password hash.
/// The version is kept only inside the protected authentication cookie and lets
/// the API revoke existing sessions immediately after an administrative reset.
/// </summary>
public static class IdentityPasswordVersion
{
    public static string Create(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash));
        return Convert.ToHexString(bytes);
    }
}
