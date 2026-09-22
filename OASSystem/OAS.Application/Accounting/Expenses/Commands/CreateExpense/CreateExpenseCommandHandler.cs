using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;
using DomainExpenseStatus = OAS.Domain.Accounting.Enums.ExpenseStatus;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;

namespace OAS.Application.Accounting.Expenses.Commands.CreateExpense;

public sealed class CreateExpenseCommandHandler(
    IRepository<Expense, Guid> repository,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateExpenseCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateExpenseCommand request,
        CancellationToken cancellationToken)
    {
        var data = request.Data;

        var sequenceName = $"Expense-{data.ExpenseDate.Year}";
        var sequence = await sequenceNumberGenerator.NextAsync(sequenceName, cancellationToken);
        var expenseNumber = $"EXP-{data.ExpenseDate.Year:0000}-{sequence:000000}";

        var expense = Expense.Create(
            Guid.NewGuid(),
            expenseNumber,
            data.ExpenseDate,
            data.ExpenseTypeId,
            data.ExpenseAccountId,
            data.Beneficiary,
            data.Amount,
            (DomainPaymentMethod)(int)data.PaymentMethod,
            data.CashAccountId,
            data.BankAccountId,
            data.Description,
            DomainExpenseStatus.Draft,
            journalEntryId: null);

        await repository.AddAsync(expense, cancellationToken);
        return expense.Id;
    }
}
