using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.ValueObjects;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.ValueObjects;

public sealed class PartyContactInfo : ValueObject
{
    private PartyContactInfo() => Address = PartyAddress.Empty;

    private PartyContactInfo(string? contactPersonName, string? contactPersonTitle, string? phone, string? mobile,
        string? alternatePhone, string? whatsAppNumber, string? email, string? website,
        ContactMethod preferredContactMethod, PartyAddress address)
    {
        ContactPersonName = Normalize(contactPersonName, 150, "Contact person name");
        ContactPersonTitle = Normalize(contactPersonTitle, 100, "Contact person title");
        Phone = Normalize(phone, 32, "Phone");
        Mobile = Normalize(mobile, 32, "Mobile");
        AlternatePhone = Normalize(alternatePhone, 32, "Alternate phone");
        WhatsAppNumber = Normalize(whatsAppNumber, 32, "WhatsApp number");
        Email = Normalize(email, 256, "Email");
        Website = Normalize(website, 300, "Website");
        if (!Enum.IsDefined(preferredContactMethod)) throw new DomainException("Preferred contact method is invalid.");
        PreferredContactMethod = preferredContactMethod;
        Address = address ?? throw new DomainException("Address is required.");
    }

    public string? ContactPersonName { get; private set; }
    public string? ContactPersonTitle { get; private set; }
    public string? Phone { get; private set; }
    public string? Mobile { get; private set; }
    public string? AlternatePhone { get; private set; }
    public string? WhatsAppNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }
    public ContactMethod PreferredContactMethod { get; private set; }
    public PartyAddress Address { get; private set; }

    public static PartyContactInfo Empty => new(null, null, null, null, null, null, null, null, ContactMethod.Unspecified, PartyAddress.Empty);

    public static PartyContactInfo Create(string? contactPersonName, string? contactPersonTitle, string? phone, string? mobile,
        string? alternatePhone, string? whatsAppNumber, string? email, string? website,
        ContactMethod preferredContactMethod, PartyAddress address) =>
        new(contactPersonName, contactPersonTitle, phone, mobile, alternatePhone, whatsAppNumber, email, website, preferredContactMethod, address);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ContactPersonName; yield return ContactPersonTitle; yield return Phone; yield return Mobile;
        yield return AlternatePhone; yield return WhatsAppNumber; yield return Email; yield return Website;
        yield return PreferredContactMethod; yield return Address;
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
