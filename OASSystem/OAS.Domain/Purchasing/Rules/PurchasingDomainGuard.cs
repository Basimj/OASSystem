using OAS.Domain.Exceptions;

namespace OAS.Domain.Purchasing.Rules;

internal static class PurchasingDomainGuard
{
    public static Guid Required(Guid value, string name)
    {
        if (value == Guid.Empty) throw new DomainException($"{name} is required.");
        return value;
    }

    public static string Required(string? value, int maxLength, string name)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new DomainException($"{name} is required.");
        if (normalized.Length > maxLength) throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        return normalized;
    }

    public static string? Optional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        return normalized;
    }

    public static void Positive(decimal value, string name)
    {
        if (value <= 0) throw new DomainException($"{name} must be greater than zero.");
    }

    public static void NonNegative(decimal value, string name)
    {
        if (value < 0) throw new DomainException($"{name} cannot be negative.");
    }

    public static void Defined<TEnum>(TEnum value, string name) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value)) throw new DomainException($"{name} is invalid.");
    }

    public static string? User(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
