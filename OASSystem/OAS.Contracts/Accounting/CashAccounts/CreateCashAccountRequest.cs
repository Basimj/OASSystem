namespace OAS.Contracts.Accounting.CashAccounts;

public sealed record CreateCashAccountRequest(
    string Code,
    string Name,
    Guid AccountId,
    bool IsDefault = false,
    bool IsActive = true);