using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.ValueObjects;
using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.Entities;

public sealed class Customer : AuditableEntity<Guid>
{
    private Customer() => ContactInfo = PartyContactInfo.Empty;

    private Customer(Guid id, string customerCode, Guid accountId, PartyEntityType entityType, string nameAr,
        string? nameEn, string? tradeName, string? nationalId, string? commercialRegistrationNo, string? taxNumber,
        DateOnly? dateOfBirth, Gender gender, PartyContactInfo contactInfo, bool isCreditAllowed, decimal creditLimit,
        int paymentTermDays, DateOnly? customerSince, bool isActive, string? notes)
    {
        if (id == Guid.Empty) throw new DomainException("Customer id is required.");
        Id = id;
        if (accountId == Guid.Empty) throw new DomainException("Customer account id is required.");
        CustomerCode = NormalizeRequired(customerCode, 32, "Customer code");
        AccountId = accountId;
        UpdateDetails(entityType, nameAr, nameEn, tradeName, nationalId, commercialRegistrationNo, taxNumber,
            dateOfBirth, gender, contactInfo, isCreditAllowed, creditLimit, paymentTermDays, customerSince, notes);
        IsActive = isActive;
    }

    public string CustomerCode { get; private set; } = string.Empty;
    public Guid AccountId { get; private set; }
    public PartyEntityType EntityType { get; private set; }
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public string? TradeName { get; private set; }
    public string? NationalId { get; private set; }
    public string? CommercialRegistrationNo { get; private set; }
    public string? TaxNumber { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public PartyContactInfo ContactInfo { get; private set; }
    public bool IsCreditAllowed { get; private set; }
    public decimal CreditLimit { get; private set; }
    public int PaymentTermDays { get; private set; }
    public DateOnly? CustomerSince { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Customer Create(Guid id, string customerCode, Guid accountId, PartyEntityType entityType, string nameAr,
        string? nameEn, string? tradeName, string? nationalId, string? commercialRegistrationNo, string? taxNumber,
        DateOnly? dateOfBirth, Gender gender, PartyContactInfo contactInfo, bool isCreditAllowed, decimal creditLimit,
        int paymentTermDays, DateOnly? customerSince, bool isActive, string? notes) =>
        new(id, customerCode, accountId, entityType, nameAr, nameEn, tradeName, nationalId, commercialRegistrationNo,
            taxNumber, dateOfBirth, gender, contactInfo, isCreditAllowed, creditLimit, paymentTermDays, customerSince, isActive, notes);

    public void UpdateDetails(PartyEntityType entityType, string nameAr, string? nameEn, string? tradeName,
        string? nationalId, string? commercialRegistrationNo, string? taxNumber, DateOnly? dateOfBirth, Gender gender,
        PartyContactInfo contactInfo, bool isCreditAllowed, decimal creditLimit, int paymentTermDays,
        DateOnly? customerSince, string? notes)
    {
        if (entityType is PartyEntityType.Unknown || !Enum.IsDefined(entityType)) throw new DomainException("Customer entity type is required.");
        if (!Enum.IsDefined(gender)) throw new DomainException("Gender is invalid.");
        if (dateOfBirth.HasValue && dateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow)) throw new DomainException("Date of birth cannot be in the future.");
        if (creditLimit < 0) throw new DomainException("Credit limit cannot be negative.");
        if (paymentTermDays < 0) throw new DomainException("Payment term days cannot be negative.");
        EntityType = entityType;
        NameAr = NormalizeRequired(nameAr, 150, "Arabic name");
        NameEn = NormalizeOptional(nameEn, 150, "English name");
        TradeName = NormalizeOptional(tradeName, 150, "Trade name");
        NationalId = NormalizeOptional(nationalId, 50, "National id");
        CommercialRegistrationNo = NormalizeOptional(commercialRegistrationNo, 50, "Commercial registration number");
        TaxNumber = NormalizeOptional(taxNumber, 50, "Tax number");
        DateOfBirth = dateOfBirth;
        Gender = gender;
        ContactInfo = contactInfo ?? throw new DomainException("Contact information is required.");
        IsCreditAllowed = isCreditAllowed;
        CreditLimit = isCreditAllowed ? creditLimit : 0m;
        PaymentTermDays = isCreditAllowed ? paymentTermDays : 0;
        CustomerSince = customerSince;
        Notes = NormalizeOptional(notes, 1000, "Notes");
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static string NormalizeRequired(string value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"{name} is required.");
        var n=value.Trim(); if(n.Length>max) throw new DomainException($"{name} cannot exceed {max} characters."); return n;
    }
    private static string? NormalizeOptional(string? value, int max, string name)
    {
        if(string.IsNullOrWhiteSpace(value)) return null; var n=value.Trim(); if(n.Length>max) throw new DomainException($"{name} cannot exceed {max} characters."); return n;
    }
}
