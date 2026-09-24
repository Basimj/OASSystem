namespace OAS.Contracts.Accounting.CustomerAccounts;

public sealed record CreateCustomerAccountRequest(
    Guid CustomerId,
    Guid AccountId,
    Guid ControlAccountId,
    bool IsActive = true);