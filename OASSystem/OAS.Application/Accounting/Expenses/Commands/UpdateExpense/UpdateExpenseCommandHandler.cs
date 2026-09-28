using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;

namespace OAS.Application.Accounting.Expenses.Commands.UpdateExpense;

public sealed class UpdateExpenseCommandHandler(
    IRepository<Expense, Guid> repository,
    IReadRepository<Account, Guid> accountRepository)
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
            throw new ConcurrencyException("تم تعديل المصروف بواسطة مستخدم آخر. أعد تحميله ثم حاول مرة أخرى.");
        }

        var data = request.Data;
        var account = await accountRepository.GetByIdAsync(data.ExpenseAccountId, cancellationToken);
        if (account is null)
            throw new NotFoundException(nameof(Account), data.ExpenseAccountId);
        if (!account.CanReceivePosting())
            throw new ConflictException(
                "expense_account_invalid",
                $"الحساب {account.Code} - {account.NameAr} غير صالح كحساب مصروف للترحيل.");

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
