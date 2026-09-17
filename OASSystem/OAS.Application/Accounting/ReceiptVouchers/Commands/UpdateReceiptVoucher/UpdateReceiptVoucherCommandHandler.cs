using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainReceiptPartyType = OAS.Domain.Accounting.Enums.ReceiptPartyType;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.UpdateReceiptVoucher;

public sealed class UpdateReceiptVoucherCommandHandler(
    IRepository<ReceiptVoucher, Guid> repository)
    : IRequestHandler<UpdateReceiptVoucherCommand>
{
    public async Task Handle(
        UpdateReceiptVoucherCommand request,
        CancellationToken cancellationToken)
    {
        var voucher = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (voucher is null)
        {
            throw new NotFoundException(nameof(ReceiptVoucher), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!voucher.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The receipt voucher has been modified by another user.");
        }

        var data = request.Data;
        voucher.UpdateDetails(
            data.VoucherDate,
            (DomainReceiptPartyType)(int)data.PartyType,
            data.CustomerId,
            data.ReceivedFrom,
            (DomainPaymentMethod)(int)data.PaymentMethod,
            data.CashAccountId,
            data.BankAccountId,
            data.TotalAmount,
            data.Description);

        var lineNumber = 1;
        var newLines = data.Lines
            .Select(l => ReceiptVoucherLine.Create(
                Guid.NewGuid(),
                voucher.Id,
                lineNumber++,
                l.AccountId,
                l.Amount,
                l.ReferenceType,
                l.ReferenceId,
                l.Description))
            .ToList();

        voucher.ReplaceLines(newLines);
        repository.Update(voucher);
    }
}
