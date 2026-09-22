using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class Account : AuditableEntity<Guid>
{
    private Account()
    {
    }

    private Account(
        Guid id,
        string code,
        string nameAr,
        string? nameEn,
        Guid? parentAccountId,
        byte level,
        AccountClass accountClass,
        AccountType accountType,
        NormalBalance normalBalance,
        bool isPostingAccount,
        bool isControlAccount,
        bool allowManualPosting,
        bool isSystemAccount,
        bool isActive,
        DateOnly? effectiveDate)
    {
        Id = id;
        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        ParentAccountId = parentAccountId;
        Level = level;
        AccountClass = accountClass;
        AccountType = accountType;
        NormalBalance = normalBalance;
        IsPostingAccount = isPostingAccount;
        IsControlAccount = isControlAccount;
        AllowManualPosting = allowManualPosting;
        IsSystemAccount = isSystemAccount;
        IsActive = isActive;
        EffectiveDate = effectiveDate;
    }

    public string Code { get; private set; } = null!;

    public string NameAr { get; private set; } = null!;

    public string? NameEn { get; private set; }

    public Guid? ParentAccountId { get; private set; }

    public byte Level { get; private set; }

    public AccountClass AccountClass { get; private set; }

    public AccountType AccountType { get; private set; }

    public NormalBalance NormalBalance { get; private set; }

    public bool IsPostingAccount { get; private set; }

    public bool IsControlAccount { get; private set; }

    public bool AllowManualPosting { get; private set; }

    public bool IsSystemAccount { get; private set; }

    public bool IsActive { get; private set; }

    public DateOnly? EffectiveDate { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Account Create(
        Guid id,
        string code,
        string nameAr,
        string? nameEn,
        Guid? parentAccountId,
        byte level,
        AccountClass accountClass,
        AccountType accountType,
        NormalBalance normalBalance,
        bool isPostingAccount,
        bool isControlAccount,
        bool allowManualPosting,
        bool isSystemAccount,
        bool isActive,
        DateOnly? effectiveDate)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Account code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new ArgumentException("Arabic account name is required.", nameof(nameAr));

        if (level == 0)
            throw new ArgumentOutOfRangeException(nameof(level));

        return new Account(
            id,
            code.Trim(),
            nameAr.Trim(),
            string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            parentAccountId,
            level,
            accountClass,
            accountType,
            normalBalance,
            isPostingAccount,
            isControlAccount,
            allowManualPosting,
            isSystemAccount,
            isActive,
            effectiveDate);
    }

    public void UpdateDetails(
        string code,
        string nameAr,
        string? nameEn,
        Guid? parentAccountId,
        byte level,
        AccountClass accountClass,
        AccountType accountType,
        NormalBalance normalBalance,
        bool isPostingAccount,
        bool isControlAccount,
        bool allowManualPosting,
        DateOnly? effectiveDate)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Account code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new ArgumentException("Arabic account name is required.", nameof(nameAr));

        if (level == 0)
            throw new ArgumentOutOfRangeException(nameof(level));

        Code = code.Trim();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        ParentAccountId = parentAccountId;
        Level = level;
        AccountClass = accountClass;
        AccountType = accountType;
        NormalBalance = normalBalance;
        IsPostingAccount = isPostingAccount;
        IsControlAccount = isControlAccount;
        AllowManualPosting = allowManualPosting;
        EffectiveDate = effectiveDate;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public bool CanReceiveManualPosting()
    {
        return IsActive
            && IsPostingAccount
            && AccountType != AccountType.Header
            && AllowManualPosting;
    }

    public bool CanReceivePosting(bool isManual = false)
    {
        if (!IsActive || !IsPostingAccount || AccountType == AccountType.Header)
            return false;

        if (isManual && !AllowManualPosting)
            return false;

        return true;
    }
}
