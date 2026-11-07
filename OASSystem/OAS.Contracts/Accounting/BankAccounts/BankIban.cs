namespace OAS.Contracts.Accounting.BankAccounts;

/// <summary>
/// Normalizes and verifies optional IBANs using ISO 13616's modulo-97 check.
/// A country-specific length directory may be added when the supported countries are finalized.
/// </summary>
public static class BankIban
{
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return new string(raw.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
    }

    public static bool IsValid(string? raw)
    {
        var iban = Normalize(raw);
        if (iban is null) return true; // IBAN is optional in the existing domain.
        if (iban.Length is < 15 or > 34 ||
            !char.IsAsciiLetterUpper(iban[0]) || !char.IsAsciiLetterUpper(iban[1]) ||
            !char.IsAsciiDigit(iban[2]) || !char.IsAsciiDigit(iban[3]) ||
            iban.Any(c => !char.IsAsciiDigit(c) && !char.IsAsciiLetterUpper(c))) return false;

        var remainder = 0;
        foreach (var ch in iban[4..].Concat(iban[..4]))
        {
            if (char.IsAsciiDigit(ch)) remainder = (remainder * 10 + ch - '0') % 97;
            else remainder = (remainder * 100 + ch - 'A' + 10) % 97;
        }
        return remainder == 1;
    }
}
