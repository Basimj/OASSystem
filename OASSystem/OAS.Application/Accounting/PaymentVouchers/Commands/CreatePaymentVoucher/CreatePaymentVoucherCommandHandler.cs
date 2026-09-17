using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Domain.Accounting.Entities;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainPaymentPartyType = OAS.Domain.Accounting.Enums.PaymentPartyType;
using DomainPaymentVoucherStatus = OAS.Domain.Accounting.Enums.PaymentVoucherStatus;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.CreatePaymentVoucher;

public sealed class CreatePaymentVoucherCommandHandler(
    IRepository<PaymentVoucher, Guid> repository,
    ISequenceNumberGenerator sequenceNumberGenerator,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<CreatePaymentVoucherCommand, Guid>
{
    public async Task<Guid> Handle(
        CreatePaymentVoucherCommand request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var data = request.Data;

        var sequenceName = $"PaymentVoucher-{data.VoucherDate.Year}";
        var sequence = await sequenceNumberGenerator.NextAsync(sequenceName, cancellationToken);
        var voucherNumber = $"PV-{data.VoucherDate.Year:0000}-{sequence:000000}";

        var voucherId = Guid.NewGuid();

        var voucher = PaymentVoucher.Create(
            voucherId,
            voucherNumber,
            data.VoucherDate,
            (DomainPaymentPartyType)(int)data.PartyType,
            data.SupplierId,
            data.BeneficiaryName,
            (DomainPaymentMethod)(int)data.PaymentMethod,
            data.CashAccountId,
            data.BankAccountId,
            data.TotalAmount,
            DomainPaymentVoucherStatus.Draft,
            data.Description,
            journalEntryId: null,
            userId,
            timeProvider.GetUtcNow().UtcDateTime);

        var lineNumber = 1;
        foreach (var lineRequest in data.Lines)
        {
            var line = PaymentVoucherLine.Create(
                Guid.NewGuid(),
                voucherId,
                lineNumber++,
                lineRequest.AccountId,
                lineRequest.Amount,
                lineRequest.ReferenceType,
                lineRequest.ReferenceId,
                lineRequest.Description);

            voucher.AddLine(line);
        }

        await repository.AddAsync(voucher, cancellationToken);
        return voucher.Id;
    }
}
