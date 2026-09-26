using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class Currency : AuditableEntity<Guid>
{
    private Currency() { }

    private Currency(Guid id, string code, string nameAr, string? nameEn, string? symbol, byte decimalPlaces, bool isActive)
    {
        Id = id;
        Code = NormalizeCode(code);
        NameAr = Required(nameAr, nameof(nameAr));
        NameEn = Optional(nameEn);
        Symbol = Optional(symbol);
        SetDecimalPlaces(decimalPlaces);
        IsActive = isActive;
    }

    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public string? Symbol { get; private set; }
    public byte DecimalPlaces { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Currency Create(Guid id, string code, string nameAr, string? nameEn, string? symbol, byte decimalPlaces, bool isActive = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Currency id is required.", nameof(id));
        return new Currency(id, code, nameAr, nameEn, symbol, decimalPlaces, isActive);
    }

    public void Update(string nameAr, string? nameEn, string? symbol, byte decimalPlaces, bool isActive)
    {
        NameAr = Required(nameAr, nameof(nameAr));
        NameEn = Optional(nameEn);
        Symbol = Optional(symbol);
        SetDecimalPlaces(decimalPlaces);
        IsActive = isActive;
    }

    public void SetActive(bool active) => IsActive = active;

    private void SetDecimalPlaces(byte value)
    {
        if (value > 6) throw new ArgumentOutOfRangeException(nameof(value), "Currency decimal places cannot exceed 6.");
        DecimalPlaces = value;
    }

    private static string NormalizeCode(string value)
    {
        var result = Required(value, nameof(value)).ToUpperInvariant();
        if (result.Length is < 3 or > 8) throw new ArgumentOutOfRangeException(nameof(value), "Currency code length must be between 3 and 8 characters.");
        return result;
    }

    private static string Required(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
