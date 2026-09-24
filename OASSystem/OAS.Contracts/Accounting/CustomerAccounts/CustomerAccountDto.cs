namespace OAS.Contracts.Accounting.CustomerAccounts;

public sealed record CustomerAccountDto(
    Guid Id,
    Guid CustomerId,
    Guid AccountId,
    Guid ControlAccountId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    string RowVersion);