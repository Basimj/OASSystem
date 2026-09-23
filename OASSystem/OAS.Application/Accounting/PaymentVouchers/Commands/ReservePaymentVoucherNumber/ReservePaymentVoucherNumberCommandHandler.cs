using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Accounting.Common;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.ReservePaymentVoucherNumber;

public sealed class ReservePaymentVoucherNumberCommandHandler(ISequenceNumberGenerator sequences, IReadRepository<PaymentVoucher, Guid> repository)
    : IRequestHandler<ReservePaymentVoucherNumberCommand, AccountingNumberReservationDto>
{
    public async Task<AccountingNumberReservationDto> Handle(ReservePaymentVoucherNumberCommand request, CancellationToken cancellationToken)
    {
        var year = request.VoucherDate.Year;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var sequence = await sequences.NextAsync($"PaymentVoucher-{year}", cancellationToken);
            var number = $"PV-{year:0000}-{sequence:000000}";
            var spec = new Specification<PaymentVoucher>().Where(x => x.VoucherNumber == number);
            if (await repository.CountAsync(spec, cancellationToken) == 0)
                return new AccountingNumberReservationDto(number);
        }

        throw new InvalidOperationException("Unable to reserve a unique accounting number.");
    }
}
