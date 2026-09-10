using OAS.Domain.Common.ValueObjects;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.ValueObjects;

public sealed class Address : ValueObject
{
    private Address()
    {
    }

    private Address(string? country, string? governorate, string? city, string? postalCode, string? residentialAddress)
    {
        Country = Normalize(country, 100, "Country");
        Governorate = Normalize(governorate, 100, "Governorate");
        City = Normalize(city, 100, "City");
        PostalCode = Normalize(postalCode, 24, "Postal code");
        ResidentialAddress = Normalize(residentialAddress, 300, "Residential address");
    }

    public string? Country { get; private set; }
    public string? Governorate { get; private set; }
    public string? City { get; private set; }
    public string? PostalCode { get; private set; }
    public string? ResidentialAddress { get; private set; }

    public static Address Empty => new(null, null, null, null, null);

    public static Address Create(
        string? country,
        string? governorate,
        string? city,
        string? postalCode,
        string? residentialAddress) =>
        new(country, governorate, city, postalCode, residentialAddress);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Country;
        yield return Governorate;
        yield return City;
        yield return PostalCode;
        yield return ResidentialAddress;
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
