namespace OAS.Contracts.Accounting.CashAccounts;

public sealed record UpdateCashAccountRequest(
    string Code,
    string Name,
    Guid AccountId,
    bool IsDefault,
    bool IsActive,
    string RowVersion);