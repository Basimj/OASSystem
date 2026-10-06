using System.Text.Json.Serialization;

namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesCustomerLookupDto
{
    public Guid Id { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string NameAr { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public string? Mobile { get; init; }
    public string? Phone { get; init; }
    public bool IsCreditAllowed { get; init; }
    public decimal CreditLimit { get; init; }
    public int PaymentTermDays { get; init; }
    public bool IsActive { get; init; }

    // Compatibility-only properties. They are deliberately excluded from API JSON because
    // AccountId/AccountCode are accounting internals, not customer-facing identifiers.
    [JsonIgnore]
    [Obsolete("Accounting AccountId is not part of the Sales customer lookup contract.")]
    public Guid AccountId { get; init; }

    [JsonIgnore]
    [Obsolete("Accounting AccountCode is not part of the Sales customer lookup contract.")]
    public string AccountCode { get; init; } = string.Empty;

    public SalesCustomerLookupDto()
    {
    }

    public SalesCustomerLookupDto(
        Guid id,
        string customerCode,
        string nameAr,
        string? nameEn,
        string? mobile,
        string? phone,
        bool isCreditAllowed,
        decimal creditLimit,
        int paymentTermDays,
        bool isActive)
    {
        Id = id;
        CustomerCode = customerCode;
        NameAr = nameAr;
        NameEn = nameEn;
        Mobile = mobile;
        Phone = phone;
        IsCreditAllowed = isCreditAllowed;
        CreditLimit = creditLimit;
        PaymentTermDays = paymentTermDays;
        IsActive = isActive;
    }

    [Obsolete("Use the constructor that does not accept AccountId/AccountCode.")]
    public SalesCustomerLookupDto(
        Guid id,
        string customerCode,
        Guid accountId,
        string accountCode,
        string nameAr,
        string? mobile,
        bool isCreditAllowed,
        decimal creditLimit,
        int paymentTermDays,
        bool isActive)
        : this(id, customerCode, nameAr, null, mobile, null, isCreditAllowed, creditLimit, paymentTermDays, isActive)
    {
        AccountId = accountId;
        AccountCode = accountCode;
    }
}
