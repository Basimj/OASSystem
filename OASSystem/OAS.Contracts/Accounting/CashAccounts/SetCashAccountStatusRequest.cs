namespace OAS.Contracts.Accounting.CashAccounts;

public sealed record SetCashAccountStatusRequest(
    bool IsActive,
    string RowVersion);