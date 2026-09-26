namespace OAS.Contracts.Accounting.BankAccounts;

public sealed record UpdateBankAccountRequest(
    string BankName,
    string AccountName,
    string AccountNumber,
    string? IBAN,
    Guid CurrencyId,
    bool IsActive,
    string RowVersion);
