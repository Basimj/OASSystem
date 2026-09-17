using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Domain.Accounting.Entities;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainPaymentPartyType = OAS.Domain.Accounting.Enums.PaymentPartyType;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.UpdatePaymentVoucher;

public sealed class UpdatePaymentVoucherCommandHandler(
    IRepository<PaymentVoucher, Guid> repository)
    : IRequestHandler<UpdatePaymentVoucherCommand>
{
    public async Task Handle(
        UpdatePaymentVoucherCommand request,
        CancellationToken cancellationToken)
    {
        var voucher = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (voucher is null)
        {
            throw new NotFoundException(nameof(PaymentVoucher), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!voucher.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The payment voucher has been modified by another user.");
        }

        var data = request.Data;
        voucher.UpdateDetails(
            data.VoucherDate,
            (DomainPaymentPartyType)(int)data.PartyType,
            data.SupplierId,
            data.BeneficiaryName,
            (DomainPaymentMethod)(int)data.PaymentMethod,
            data.CashAccountId,
            data.BankAccountId,
            data.TotalAmount,
            data.Description);

        var lineNumber = 1;
        var newLines = data.Lines
            .Select(l => PaymentVoucherLine.Create(
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
