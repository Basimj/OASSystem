using OAS.Application.Common.Exceptions;

namespace OAS.Application.Identity.Users.Common;

internal static class RowVersionCodec
{
    public static byte[] Decode(string value)
    {
        try
        {
            var bytes = Convert.FromBase64String(value);
            if (bytes.Length == 0) throw new FormatException();
            return bytes;
        }
        catch (FormatException)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["RowVersion"] = ["row_version_invalid"]
            });
        }
    }

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try { return Convert.FromBase64String(value).Length > 0; }
        catch (FormatException) { return false; }
    }

    public static void EnsureMatches(byte[] actual, string incoming)
    {
        if (!Decode(incoming).SequenceEqual(actual))
            throw new ConcurrencyException("The user account was changed by another operation. Reload it and try again.");
    }
}
