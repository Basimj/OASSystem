namespace OAS.Contracts.Accounting.BankAccounts;

public sealed record UpdateBankAccountRequest(
    string Code,
    string BankName,
    string AccountName,
    string AccountNumber,
    string? IBAN,
    Guid AccountId,
    bool IsActive,
    string RowVersion);