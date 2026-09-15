using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class ExpenseType : Entity<Guid>
{
    private ExpenseType()
    {
    }

    private ExpenseType(
        Guid id,
        string code,
        string nameAr,
        string? nameEn,
        Guid? defaultExpenseAccountId,
        bool isActive)
    {
        Id = id;
        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        DefaultExpenseAccountId = defaultExpenseAccountId;
        IsActive = isActive;
    }

    public string Code { get; private set; } = null!;

    public string NameAr { get; private set; } = null!;

    public string? NameEn { get; private set; }

    public Guid? DefaultExpenseAccountId { get; private set; }

    public bool IsActive { get; private set; }

    public static ExpenseType Create(
        Guid id,
        string code,
        string nameAr,
        string? nameEn,
        Guid? defaultExpenseAccountId,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new ArgumentException("Arabic name is required.", nameof(nameAr));

        return new ExpenseType(
            id,
            code.Trim(),
            nameAr.Trim(),
            string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            defaultExpenseAccountId,
            isActive);
    }

    public void UpdateDetails(
        string code,
        string nameAr,
        string? nameEn,
        Guid? defaultExpenseAccountId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new ArgumentException("Arabic name is required.", nameof(nameAr));

        Code = code.Trim();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        DefaultExpenseAccountId = defaultExpenseAccountId;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}