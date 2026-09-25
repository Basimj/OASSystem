using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;
using DomainPaymentPartyType = OAS.Domain.Accounting.Enums.PaymentPartyType;
using DomainPaymentVoucherStatus = OAS.Domain.Accounting.Enums.PaymentVoucherStatus;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.CreatePaymentVoucher;

public sealed class CreatePaymentVoucherCommandHandler(
    IRepository<PaymentVoucher, Guid> repository,
    IReadRepository<Supplier, Guid> suppliers,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreatePaymentVoucherCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaymentVoucherCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        var voucherNumber = await ResolveNumberAsync(data.VoucherNumber, data.VoucherDate.Year, cancellationToken);
        if ((DomainPaymentPartyType)(int)data.PartyType == DomainPaymentPartyType.Supplier)
        {
            if (!data.SupplierId.HasValue) throw new ConflictException("payment_supplier_required", "Supplier is required for a supplier payment.");
            var supplier = await suppliers.GetByIdAsync(data.SupplierId.Value, cancellationToken);
            if (supplier is null) throw new NotFoundException(nameof(Supplier), data.SupplierId.Value);
            if (!supplier.IsActive) throw new ConflictException("payment_supplier_inactive", "The selected supplier is inactive.");
        }

        var voucherId = Guid.NewGuid();
        var voucher = PaymentVoucher.Create(voucherId, voucherNumber, data.VoucherDate,
            (DomainPaymentPartyType)(int)data.PartyType, data.SupplierId, data.BeneficiaryName,
            (DomainPaymentMethod)(int)data.PaymentMethod, data.CashAccountId, data.BankAccountId,
            data.TotalAmount, DomainPaymentVoucherStatus.Draft, data.Description, null);
        var lineNumber = 1;
        foreach (var lineRequest in data.Lines)
            voucher.AddLine(PaymentVoucherLine.Create(Guid.NewGuid(), voucherId, lineNumber++, lineRequest.AccountId,
                lineRequest.Amount, lineRequest.ReferenceType, lineRequest.ReferenceId, lineRequest.Description));
        await repository.AddAsync(voucher, cancellationToken);
        return voucher.Id;
    }

    private async Task<string> ResolveNumberAsync(string? requested, int year, CancellationToken ct)
    {
        var number = requested?.Trim();
        if (!string.IsNullOrEmpty(number) && !number.StartsWith($"PV-{year:0000}-", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("payment_voucher_number_period_mismatch", "Reserved payment voucher number does not match the voucher year.");
        if (string.IsNullOrEmpty(number))
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var sequence = await sequenceNumberGenerator.NextAsync($"PaymentVoucher-{year}", ct);
                number = $"PV-{year:0000}-{sequence:000000}";
                if (!await ExistsAsync(number, ct)) break;
            }
        }
        if (string.IsNullOrEmpty(number) || await ExistsAsync(number, ct))
            throw new ConflictException("payment_voucher_number_duplicate", "Payment voucher number is already in use.");
        return number;
    }
    private async Task<bool> ExistsAsync(string number, CancellationToken ct) =>
        await repository.CountAsync(new Specification<PaymentVoucher>().Where(x => x.VoucherNumber == number), ct) > 0;
}
