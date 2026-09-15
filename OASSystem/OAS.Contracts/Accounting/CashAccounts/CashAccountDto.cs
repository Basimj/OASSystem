namespace OAS.Contracts.Accounting.CashAccounts;

public sealed record CashAccountDto(
    Guid Id,
    string Code,
    string Name,
    Guid AccountId,
    bool IsDefault,
    bool IsActive,
    string RowVersion);