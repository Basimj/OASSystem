namespace OAS.Contracts.Accounting.Expenses;

public sealed record UpdateExpenseTypeRequest(
    string Code,
    string NameAr,
    string? NameEn,
    Guid? DefaultExpenseAccountId,
    bool IsActive,
    string RowVersion);