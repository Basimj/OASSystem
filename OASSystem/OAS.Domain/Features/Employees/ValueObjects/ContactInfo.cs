using OAS.Domain.Common.ValueObjects;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.ValueObjects;

public sealed class ContactInfo : ValueObject
{
    private ContactInfo()
    {
        Address = Address.Empty;
    }

    private ContactInfo(string? phone, string? email, Address address)
    {
        Phone = Normalize(phone, 32, "Phone");
        Email = Normalize(email, 256, "Email");
        Address = address ?? throw new DomainException("Address is required.");
    }

    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public Address Address { get; private set; }

    public static ContactInfo Empty => new(null, null, Address.Empty);

    public static ContactInfo Create(string? phone, string? email, Address address) =>
        new(phone, email, address);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Phone;
        yield return Email;
        yield return Address;
    }

    private static string? Normalize(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        return normalized;
    }
}
