using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainReceiptPartyType = OAS.Domain.Accounting.Enums.ReceiptPartyType;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.UpdateReceiptVoucher;

public sealed class UpdateReceiptVoucherCommandHandler(
    IRepository<ReceiptVoucher, Guid> repository,
    IRepository<ReceiptVoucherLine, Guid> lineRepository,
    IReadRepository<Customer, Guid> customers)
    : IRequestHandler<UpdateReceiptVoucherCommand>
{
    public async Task Handle(
        UpdateReceiptVoucherCommand request,
        CancellationToken cancellationToken)
    {
        var voucher = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (voucher is null)
            throw new NotFoundException(nameof(ReceiptVoucher), request.Id);

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!voucher.RowVersion.SequenceEqual(requestedRowVersion))
            throw new ConcurrencyException("The receipt voucher has been modified by another user.");

        var data = request.Data;
        if ((DomainReceiptPartyType)(int)data.PartyType == DomainReceiptPartyType.Customer)
        {
            if (!data.CustomerId.HasValue) throw new ConflictException("receipt_customer_required", "Customer is required for a customer receipt.");
            var customer = await customers.GetByIdAsync(data.CustomerId.Value, cancellationToken);
            if (customer is null) throw new NotFoundException(nameof(Customer), data.CustomerId.Value);
            if (!customer.IsActive) throw new ConflictException("receipt_customer_inactive", "The selected customer is inactive.");
        }
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

        var existingLines = await lineRepository.ListAsync(
            new Specification<ReceiptVoucherLine>()
                .Where(x => x.ReceiptVoucherId == voucher.Id)
                .Tracking(),
            cancellationToken);

        if (existingLines.Count > 0)
            lineRepository.DeleteRange(existingLines);

        var lineNumber = 1;
        var newLines = data.Lines
            .Select(line => ReceiptVoucherLine.Create(
                Guid.NewGuid(), voucher.Id, lineNumber++, line.AccountId,
                line.Amount, line.ReferenceType, line.ReferenceId, line.Description))
            .ToList();

        voucher.ReplaceLines(newLines);
        if (newLines.Count > 0)
            await lineRepository.AddRangeAsync(newLines, cancellationToken);

        repository.Update(voucher);
    }
}
