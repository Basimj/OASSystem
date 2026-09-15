namespace OAS.Contracts.Accounting.Expenses;

public sealed record CreateExpenseTypeRequest(
    string Code,
    string NameAr,
    string? NameEn,
    Guid? DefaultExpenseAccountId,
    bool IsActive = true);