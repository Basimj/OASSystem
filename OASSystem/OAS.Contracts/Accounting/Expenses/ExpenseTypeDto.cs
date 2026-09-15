namespace OAS.Contracts.Accounting.Expenses;

public sealed record ExpenseTypeDto(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    Guid? DefaultExpenseAccountId,
    bool IsActive,
    string RowVersion);