using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class BankAccount : Entity<Guid>
{
    private BankAccount()
    {
    }

    private BankAccount(
        Guid id,
        string code,
        string bankName,
        string accountName,
        string accountNumber,
        string? iban,
        Guid accountId,
        bool isActive)
    {
        Id = id;
        Code = code;
        BankName = bankName;
        AccountName = accountName;
        AccountNumber = accountNumber;
        IBAN = iban;
        AccountId = accountId;
        IsActive = isActive;
    }

    public string Code { get; private set; } = null!;

    public string BankName { get; private set; } = null!;

    public string AccountName { get; private set; } = null!;

    public string AccountNumber { get; private set; } = null!;

    public string? IBAN { get; private set; }

    public Guid AccountId { get; private set; }

    public bool IsActive { get; private set; }

    public static BankAccount Create(
        Guid id,
        string code,
        string bankName,
        string accountName,
        string accountNumber,
        string? iban,
        Guid accountId,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("Bank name is required.", nameof(bankName));

        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException(
                "Account name is required.",
                nameof(accountName));

        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException(
                "Account number is required.",
                nameof(accountNumber));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        return new BankAccount(
            id,
            code.Trim(),
            bankName.Trim(),
            accountName.Trim(),
            accountNumber.Trim(),
            string.IsNullOrWhiteSpace(iban) ? null : iban.Trim(),
            accountId,
            isActive);
    }

    public void UpdateDetails(
        string code,
        string bankName,
        string accountName,
        string accountNumber,
        string? iban,
        Guid accountId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("Bank name is required.", nameof(bankName));

        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException(
                "Account name is required.",
                nameof(accountName));

        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException(
                "Account number is required.",
                nameof(accountNumber));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        Code = code.Trim();
        BankName = bankName.Trim();
        AccountName = accountName.Trim();
        AccountNumber = accountNumber.Trim();
        IBAN = string.IsNullOrWhiteSpace(iban) ? null : iban.Trim();
        AccountId = accountId;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}