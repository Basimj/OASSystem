using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Expenses;

public sealed record ExpenseDto(
    Guid Id,
    string ExpenseNumber,
    DateOnly ExpenseDate,
    Guid ExpenseTypeId,
    Guid ExpenseAccountId,
    string? Beneficiary,
    decimal Amount,
    PaymentMethod PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    string? Description,
    ExpenseStatus Status,
    Guid? JournalEntryId,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PostedAtUtc,
    string RowVersion);