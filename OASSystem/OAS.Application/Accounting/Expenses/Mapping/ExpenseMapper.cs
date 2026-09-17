using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;
using ContractExpenseStatus = OAS.Contracts.Accounting.Enums.ExpenseStatus;
using ContractPaymentMethod = OAS.Contracts.Accounting.Enums.PaymentMethod;

namespace OAS.Application.Accounting.Expenses.Mapping;

public sealed class ExpenseMapper
{
    public ExpenseDto ToRead(Expense source)
    {
        return new ExpenseDto(
            source.Id,
            source.ExpenseNumber,
            source.ExpenseDate,
            source.ExpenseTypeId,
            source.ExpenseAccountId,
            source.Beneficiary,
            source.Amount,
            (ContractPaymentMethod)(int)source.PaymentMethod,
            source.CashAccountId,
            source.BankAccountId,
            source.Description,
            (ContractExpenseStatus)(int)source.Status,
            source.JournalEntryId,
            source.CreatedBy,
            source.CreatedAtUtc,
            source.PostedAtUtc,
            source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty);
    }
}
