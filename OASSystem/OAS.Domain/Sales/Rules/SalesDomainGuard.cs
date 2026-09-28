using OAS.Domain.Exceptions;

namespace OAS.Domain.Sales.Rules;

internal static class SalesDomainGuard
{
    public static Guid Required(Guid value, string name)
    {
        if (value == Guid.Empty)
            throw new DomainException($"{name} is required.");
        return value;
    }

    public static string Required(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} is required.");

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");

        return normalized;
    }

    public static string? Optional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");

        return normalized;
    }

    public static void Defined<TEnum>(TEnum value, string name) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new DomainException($"{name} is invalid.");
    }
}
