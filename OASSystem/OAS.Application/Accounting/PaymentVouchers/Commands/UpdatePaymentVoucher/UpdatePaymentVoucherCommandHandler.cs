using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainPaymentPartyType = OAS.Domain.Accounting.Enums.PaymentPartyType;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.UpdatePaymentVoucher;

public sealed class UpdatePaymentVoucherCommandHandler(
    IRepository<PaymentVoucher, Guid> repository,
    IRepository<PaymentVoucherLine, Guid> lineRepository)
    : IRequestHandler<UpdatePaymentVoucherCommand>
{
    public async Task Handle(
        UpdatePaymentVoucherCommand request,
        CancellationToken cancellationToken)
    {
        var voucher = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (voucher is null)
            throw new NotFoundException(nameof(PaymentVoucher), request.Id);

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!voucher.RowVersion.SequenceEqual(requestedRowVersion))
            throw new ConcurrencyException("The payment voucher has been modified by another user.");

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

        var existingLines = await lineRepository.ListAsync(
            new Specification<PaymentVoucherLine>()
                .Where(x => x.PaymentVoucherId == voucher.Id)
                .Tracking(),
            cancellationToken);

        if (existingLines.Count > 0)
            lineRepository.DeleteRange(existingLines);

        var lineNumber = 1;
        var newLines = data.Lines
            .Select(line => PaymentVoucherLine.Create(
                Guid.NewGuid(), voucher.Id, lineNumber++, line.AccountId,
                line.Amount, line.ReferenceType, line.ReferenceId, line.Description))
            .ToList();

        voucher.ReplaceLines(newLines);
        if (newLines.Count > 0)
            await lineRepository.AddRangeAsync(newLines, cancellationToken);

        repository.Update(voucher);
    }
}
