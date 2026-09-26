namespace OAS.Contracts.Accounting.CashAccounts;

public sealed record UpdateCashAccountRequest(
    string Name,
    Guid CurrencyId,
    bool IsDefault,
    bool IsActive,
    string RowVersion);
