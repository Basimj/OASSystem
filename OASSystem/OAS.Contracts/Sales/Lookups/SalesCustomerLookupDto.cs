namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesCustomerLookupDto(
    Guid Id,
    string CustomerCode,
    Guid AccountId,
    string AccountCode,
    string NameAr,
    string? Mobile,
    bool IsCreditAllowed,
    decimal CreditLimit,
    int PaymentTermDays,
    bool IsActive);
