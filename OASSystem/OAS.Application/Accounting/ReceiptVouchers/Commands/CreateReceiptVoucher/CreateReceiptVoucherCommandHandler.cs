using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainReceiptPartyType = OAS.Domain.Accounting.Enums.ReceiptPartyType;
using DomainReceiptVoucherStatus = OAS.Domain.Accounting.Enums.ReceiptVoucherStatus;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.CreateReceiptVoucher;

public sealed class CreateReceiptVoucherCommandHandler(
    IRepository<ReceiptVoucher, Guid> repository,
    ISequenceNumberGenerator sequenceNumberGenerator,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<CreateReceiptVoucherCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateReceiptVoucherCommand request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var data = request.Data;

        var sequenceName = $"ReceiptVoucher-{data.VoucherDate.Year}";
        var sequence = await sequenceNumberGenerator.NextAsync(sequenceName, cancellationToken);
        var voucherNumber = $"RV-{data.VoucherDate.Year:0000}-{sequence:000000}";

        var voucherId = Guid.NewGuid();

        var voucher = ReceiptVoucher.Create(
            voucherId,
            voucherNumber,
            data.VoucherDate,
            (DomainReceiptPartyType)(int)data.PartyType,
            data.CustomerId,
            data.ReceivedFrom,
            (DomainPaymentMethod)(int)data.PaymentMethod,
            data.CashAccountId,
            data.BankAccountId,
            data.TotalAmount,
            DomainReceiptVoucherStatus.Draft,
            data.Description,
            journalEntryId: null,
            userId,
            timeProvider.GetUtcNow().UtcDateTime);

        var lineNumber = 1;
        foreach (var lineRequest in data.Lines)
        {
            var line = ReceiptVoucherLine.Create(
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
