using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;

namespace OAS.Application.Accounting.Expenses.Commands.UpdateExpense;

public sealed class UpdateExpenseCommandHandler(
    IRepository<Expense, Guid> repository)
    : IRequestHandler<UpdateExpenseCommand>
{
    public async Task Handle(
        UpdateExpenseCommand request,
        CancellationToken cancellationToken)
    {
        var expense = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (expense is null)
        {
            throw new NotFoundException(nameof(Expense), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!expense.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The expense has been modified by another user.");
        }

        var data = request.Data;
        expense.UpdateDetails(
            data.ExpenseDate,
            data.ExpenseTypeId,
            data.ExpenseAccountId,
            data.Beneficiary,
            data.Amount,
            (DomainPaymentMethod)(int)data.PaymentMethod,
            data.CashAccountId,
            data.BankAccountId,
            data.Description);

        repository.Update(expense);
    }
}
