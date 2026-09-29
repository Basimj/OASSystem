using OAS.Application.Common.Exceptions;

namespace OAS.Application.Purchasing.Common;

internal static class PurchasingRowVersion
{
    public static void EnsureMatches(byte[] current, string supplied, string resourceName)
    {
        byte[] incoming;
        try
        {
            incoming = Convert.FromBase64String(supplied);
        }
        catch (FormatException ex)
        {
            throw new ConcurrencyException($"{resourceName} row version is invalid.", ex);
        }

        if (incoming.Length == 0 || !incoming.SequenceEqual(current))
            throw new ConcurrencyException($"{resourceName} was modified by another user. Reload and try again.");
    }

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try { return Convert.FromBase64String(value).Length > 0; }
        catch { return false; }
    }

    public static string Encode(byte[] rowVersion) => rowVersion is { Length: > 0 } ? Convert.ToBase64String(rowVersion) : string.Empty;
}
