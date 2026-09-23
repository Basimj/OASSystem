using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainReceiptPartyType = OAS.Domain.Accounting.Enums.ReceiptPartyType;
using DomainReceiptVoucherStatus = OAS.Domain.Accounting.Enums.ReceiptVoucherStatus;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.CreateReceiptVoucher;

public sealed class CreateReceiptVoucherCommandHandler(
    IRepository<ReceiptVoucher, Guid> repository,
    IReadRepository<Customer, Guid> customers,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateReceiptVoucherCommand, Guid>
{
    public async Task<Guid> Handle(CreateReceiptVoucherCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        var voucherNumber = await ResolveNumberAsync(data.VoucherNumber, data.VoucherDate.Year, cancellationToken);
        if ((DomainReceiptPartyType)(int)data.PartyType == DomainReceiptPartyType.Customer)
        {
            if (!data.CustomerId.HasValue) throw new ConflictException("receipt_customer_required", "Customer is required for a customer receipt.");
            var customer = await customers.GetByIdAsync(data.CustomerId.Value, cancellationToken);
            if (customer is null) throw new NotFoundException(nameof(Customer), data.CustomerId.Value);
            if (!customer.IsActive) throw new ConflictException("receipt_customer_inactive", "The selected customer is inactive.");
        }

        var voucherId = Guid.NewGuid();
        var voucher = ReceiptVoucher.Create(voucherId, voucherNumber, data.VoucherDate,
            (DomainReceiptPartyType)(int)data.PartyType, data.CustomerId, data.ReceivedFrom,
            (DomainPaymentMethod)(int)data.PaymentMethod, data.CashAccountId, data.BankAccountId,
            data.TotalAmount, DomainReceiptVoucherStatus.Draft, data.Description, null);
        var lineNumber = 1;
        foreach (var lineRequest in data.Lines)
            voucher.AddLine(ReceiptVoucherLine.Create(Guid.NewGuid(), voucherId, lineNumber++, lineRequest.AccountId,
                lineRequest.Amount, lineRequest.ReferenceType, lineRequest.ReferenceId, lineRequest.Description));
        await repository.AddAsync(voucher, cancellationToken);
        return voucher.Id;
    }

    private async Task<string> ResolveNumberAsync(string? requested, int year, CancellationToken ct)
    {
        var number = requested?.Trim();
        if (!string.IsNullOrEmpty(number) && !number.StartsWith($"RV-{year:0000}-", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("receipt_voucher_number_period_mismatch", "Reserved receipt voucher number does not match the voucher year.");
        if (string.IsNullOrEmpty(number))
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var sequence = await sequenceNumberGenerator.NextAsync($"ReceiptVoucher-{year}", ct);
                number = $"RV-{year:0000}-{sequence:000000}";
                if (!await ExistsAsync(number, ct)) break;
            }
        }
        if (string.IsNullOrEmpty(number) || await ExistsAsync(number, ct))
            throw new ConflictException("receipt_voucher_number_duplicate", "Receipt voucher number is already in use.");
        return number;
    }
    private async Task<bool> ExistsAsync(string number, CancellationToken ct) =>
        await repository.CountAsync(new Specification<ReceiptVoucher>().Where(x => x.VoucherNumber == number), ct) > 0;
}
