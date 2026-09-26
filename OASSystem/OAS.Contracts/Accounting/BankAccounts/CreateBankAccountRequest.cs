namespace OAS.Contracts.Accounting.BankAccounts;

public sealed record CreateBankAccountRequest(
    string? Code,
    string BankName,
    string AccountName,
    string AccountNumber,
    string? IBAN,
    Guid CurrencyId,
    bool IsActive = true);
