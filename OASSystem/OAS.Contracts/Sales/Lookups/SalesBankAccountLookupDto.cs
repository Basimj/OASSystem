namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesBankAccountLookupDto(
    Guid Id,
    string Code,
    string BankName,
    string AccountName,
    string AccountNumber,
    Guid CurrencyId,
    bool IsActive);
