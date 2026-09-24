namespace OAS.Contracts.Accounting.CustomerAccounts;

public sealed record SetCustomerAccountStatusRequest(
    bool IsActive,
    string RowVersion);