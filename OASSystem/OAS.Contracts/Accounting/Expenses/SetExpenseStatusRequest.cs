using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Expenses;

public sealed record SetExpenseStatusRequest(
    ExpenseStatus Status,
    string RowVersion);