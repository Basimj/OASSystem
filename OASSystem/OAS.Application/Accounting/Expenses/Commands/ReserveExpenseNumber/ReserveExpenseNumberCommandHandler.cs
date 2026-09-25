using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Accounting.Common;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.Commands.ReserveExpenseNumber;

public sealed class ReserveExpenseNumberCommandHandler(ISequenceNumberGenerator sequences, IReadRepository<Expense, Guid> repository)
    : IRequestHandler<ReserveExpenseNumberCommand, AccountingNumberReservationDto>
{
    public async Task<AccountingNumberReservationDto> Handle(ReserveExpenseNumberCommand request, CancellationToken cancellationToken)
    {
        var year = request.ExpenseDate.Year;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var sequence = await sequences.NextAsync($"Expense-{year}", cancellationToken);
            var number = $"EXP-{year:0000}-{sequence:000000}";
            var spec = new Specification<Expense>().Where(x => x.ExpenseNumber == number);
            if (await repository.CountAsync(spec, cancellationToken) == 0)
                return new AccountingNumberReservationDto(number);
        }

        throw new InvalidOperationException("Unable to reserve a unique accounting number.");
    }
}
