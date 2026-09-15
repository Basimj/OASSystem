namespace OAS.Contracts.Accounting.BankAccounts;

public sealed record SetBankAccountStatusRequest(
    bool IsActive,
    string RowVersion);