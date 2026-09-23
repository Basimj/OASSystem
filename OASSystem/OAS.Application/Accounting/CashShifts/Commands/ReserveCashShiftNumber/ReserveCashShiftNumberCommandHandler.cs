using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Accounting.Common;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashShifts.Commands.ReserveCashShiftNumber;

public sealed class ReserveCashShiftNumberCommandHandler(ISequenceNumberGenerator sequences, IReadRepository<CashShift, Guid> repository, TimeProvider timeProvider)
    : IRequestHandler<ReserveCashShiftNumberCommand, AccountingNumberReservationDto>
{
    public async Task<AccountingNumberReservationDto> Handle(ReserveCashShiftNumberCommand request, CancellationToken cancellationToken)
    {
        var year = timeProvider.GetUtcNow().Year;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var sequence = await sequences.NextAsync($"CashShift-{year}", cancellationToken);
            var number = $"CS-{year:0000}-{sequence:000000}";
            var spec = new Specification<CashShift>().Where(x => x.ShiftNumber == number);
            if (await repository.CountAsync(spec, cancellationToken) == 0) return new AccountingNumberReservationDto(number);
        }
        throw new InvalidOperationException("Unable to reserve a unique cash shift number.");
    }
}
