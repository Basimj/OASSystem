namespace OAS.Contracts.Accounting.Expenses;

public sealed record SetExpenseTypeStatusRequest(
    bool IsActive,
    string RowVersion);