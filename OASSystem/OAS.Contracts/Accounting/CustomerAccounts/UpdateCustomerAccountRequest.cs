namespace OAS.Contracts.Accounting.CustomerAccounts;

public sealed record UpdateCustomerAccountRequest(
    Guid AccountId,
    Guid ControlAccountId,
    bool IsActive,
    string RowVersion);