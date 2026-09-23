using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainExpenseStatus = OAS.Domain.Accounting.Enums.ExpenseStatus;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;

namespace OAS.Application.Accounting.Expenses.Commands.CreateExpense;

public sealed class CreateExpenseCommandHandler(
    IRepository<Expense, Guid> repository,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateExpenseCommand, Guid>
{
    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        var expenseNumber = await ResolveNumberAsync(data.ExpenseNumber, data.ExpenseDate.Year, cancellationToken);
        var expense = Expense.Create(Guid.NewGuid(), expenseNumber, data.ExpenseDate, data.ExpenseTypeId,
            data.ExpenseAccountId, data.Beneficiary, data.Amount, (DomainPaymentMethod)(int)data.PaymentMethod,
            data.CashAccountId, data.BankAccountId, data.Description, DomainExpenseStatus.Draft, null);
        await repository.AddAsync(expense, cancellationToken);
        return expense.Id;
    }

    private async Task<string> ResolveNumberAsync(string? requested, int year, CancellationToken ct)
    {
        var number=requested?.Trim();
        if(!string.IsNullOrEmpty(number) && !number.StartsWith($"EXP-{year:0000}-",StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("expense_number_period_mismatch","Reserved expense number does not match the expense year.");
        if(string.IsNullOrEmpty(number))
        {
            for(var attempt=0;attempt<100;attempt++)
            {
                var sequence=await sequenceNumberGenerator.NextAsync($"Expense-{year}",ct);
                number=$"EXP-{year:0000}-{sequence:000000}";
                if(!await ExistsAsync(number,ct)) break;
            }
        }
        if(string.IsNullOrEmpty(number)||await ExistsAsync(number,ct)) throw new ConflictException("expense_number_duplicate","Expense number is already in use.");
        return number;
    }
    private async Task<bool> ExistsAsync(string number,CancellationToken ct)=>await repository.CountAsync(new Specification<Expense>().Where(x=>x.ExpenseNumber==number),ct)>0;
}
