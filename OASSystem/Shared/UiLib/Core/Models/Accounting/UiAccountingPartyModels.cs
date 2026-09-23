namespace OAS.UiLib.Core.Models.Accounting;

public sealed record UiAccountingPartyListItem(
    Guid Id,
    string Code,
    string AccountCode,
    string Name,
    string? SecondaryName,
    string? Mobile,
    string? Location,
    string? CommercialInfo,
    string EntityLabel,
    string CreditOrTerms,
    bool IsActive);

public sealed record UiAccountingPartyParentOption(Guid Id, string Code, string Name);

public sealed class UiAccountingPartyFormModel
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string ParentAccountId { get; set; } = string.Empty;
    public string EntityType { get; set; } = "1";
    public string SupplierScope { get; set; } = "1";
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? TradeName { get; set; }
    public string? NationalId { get; set; }
    public string? CommercialRegistrationNo { get; set; }
    public string? TaxNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string Gender { get; set; } = "0";
    public string? ContactPersonName { get; set; }
    public string? ContactPersonTitle { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? AlternatePhone { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string PreferredContactMethod { get; set; } = "2";
    public string? Country { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Street { get; set; }
    public string? Building { get; set; }
    public string? PostalCode { get; set; }
    public string? AddressDetails { get; set; }
    public bool IsCreditAllowed { get; set; }
    public decimal CreditLimit { get; set; }
    public int PaymentTermDays { get; set; }
    public int? DefaultLeadTimeDays { get; set; }
    public DateOnly? Since { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
