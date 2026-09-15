namespace OAS.Contracts.Accounting.BankAccounts;

public sealed record BankAccountDto(
    Guid Id,
    string Code,
    string BankName,
    string AccountName,
    string AccountNumber,
    string? IBAN,
    Guid AccountId,
    bool IsActive,
    string RowVersion);