using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Expenses;

public sealed record CreateExpenseRequest(
    DateOnly ExpenseDate,
    Guid ExpenseTypeId,
    Guid ExpenseAccountId,
    string? Beneficiary,
    decimal Amount,
    PaymentMethod PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    string? Description,
    string? ExpenseNumber = null);