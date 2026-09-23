using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Accounting.Common;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.ReserveReceiptVoucherNumber;

public sealed class ReserveReceiptVoucherNumberCommandHandler(ISequenceNumberGenerator sequences, IReadRepository<ReceiptVoucher, Guid> repository)
    : IRequestHandler<ReserveReceiptVoucherNumberCommand, AccountingNumberReservationDto>
{
    public async Task<AccountingNumberReservationDto> Handle(ReserveReceiptVoucherNumberCommand request, CancellationToken cancellationToken)
    {
        var year = request.VoucherDate.Year;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var sequence = await sequences.NextAsync($"ReceiptVoucher-{year}", cancellationToken);
            var number = $"RV-{year:0000}-{sequence:000000}";
            var spec = new Specification<ReceiptVoucher>().Where(x => x.VoucherNumber == number);
            if (await repository.CountAsync(spec, cancellationToken) == 0)
                return new AccountingNumberReservationDto(number);
        }

        throw new InvalidOperationException("Unable to reserve a unique accounting number.");
    }
}
