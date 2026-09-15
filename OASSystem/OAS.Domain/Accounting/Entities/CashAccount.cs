using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class CashAccount : Entity<Guid>
{
    private CashAccount()
    {
    }

    private CashAccount(
        Guid id,
        string code,
        string name,
        Guid accountId,
        bool isDefault,
        bool isActive)
    {
        Id = id;
        Code = code;
        Name = name;
        AccountId = accountId;
        IsDefault = isDefault;
        IsActive = isActive;
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public Guid AccountId { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public static CashAccount Create(
        Guid id,
        string code,
        string name,
        Guid accountId,
        bool isDefault,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        return new CashAccount(
            id,
            code.Trim(),
            name.Trim(),
            accountId,
            isDefault,
            isActive);
    }

    public void UpdateDetails(
        string code,
        string name,
        Guid accountId,
        bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        Code = code.Trim();
        Name = name.Trim();
        AccountId = accountId;
        IsDefault = isDefault;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}