using OAS.Domain.Common.ValueObjects;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.ValueObjects;

public sealed class PartyAddress : ValueObject
{
    private PartyAddress() { }

    private PartyAddress(string? country, string? governorate, string? city, string? district,
        string? street, string? building, string? postalCode, string? addressDetails)
    {
        Country = Normalize(country, 100, "Country");
        Governorate = Normalize(governorate, 100, "Governorate");
        City = Normalize(city, 100, "City");
        District = Normalize(district, 100, "District");
        Street = Normalize(street, 150, "Street");
        Building = Normalize(building, 100, "Building");
        PostalCode = Normalize(postalCode, 24, "Postal code");
        AddressDetails = Normalize(addressDetails, 300, "Address details");
    }

    public string? Country { get; private set; }
    public string? Governorate { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? Street { get; private set; }
    public string? Building { get; private set; }
    public string? PostalCode { get; private set; }
    public string? AddressDetails { get; private set; }

    public static PartyAddress Empty => new(null, null, null, null, null, null, null, null);

    public static PartyAddress Create(string? country, string? governorate, string? city, string? district,
        string? street, string? building, string? postalCode, string? addressDetails) =>
        new(country, governorate, city, district, street, building, postalCode, addressDetails);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Country; yield return Governorate; yield return City; yield return District;
        yield return Street; yield return Building; yield return PostalCode; yield return AddressDetails;
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
