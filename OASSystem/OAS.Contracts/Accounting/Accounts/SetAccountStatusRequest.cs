namespace OAS.Contracts.Accounting.Accounts;

public sealed record SetAccountStatusRequest(
    bool IsActive,
    string RowVersion);