using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Domain.Accounting.Entities;
using ContractPaymentMethod = OAS.Contracts.Accounting.Enums.PaymentMethod;
using ContractPaymentPartyType = OAS.Contracts.Accounting.Enums.PaymentPartyType;
using ContractPaymentVoucherStatus = OAS.Contracts.Accounting.Enums.PaymentVoucherStatus;

namespace OAS.Application.Accounting.PaymentVouchers.Mapping;

public sealed class PaymentVoucherMapper
{
    public PaymentVoucherDto ToRead(PaymentVoucher source)
        => ToRead(source, source.Lines);

    public PaymentVoucherDto ToRead(
        PaymentVoucher source,
        IEnumerable<PaymentVoucherLine> sourceLines)
    {
        var lines = sourceLines
            .OrderBy(l => l.LineNumber)
            .Select(ToLineDto)
            .ToList();

        return new PaymentVoucherDto(
            source.Id,
            source.VoucherNumber,
            source.VoucherDate,
            (ContractPaymentPartyType)(int)source.PartyType,
            source.SupplierId,
            source.BeneficiaryName,
            (ContractPaymentMethod)(int)source.PaymentMethod,
            source.CashAccountId,
            source.BankAccountId,
            source.TotalAmount,
            (ContractPaymentVoucherStatus)(int)source.Status,
            source.Description,
            source.JournalEntryId,
            source.CreatedBy,
            source.CreatedAtUtc,
            source.PostedBy,
            source.PostedAtUtc,
            Convert.ToBase64String(source.RowVersion),
            lines);
    }

    public static PaymentVoucherLineDto ToLineDto(PaymentVoucherLine line)
        => new(
            line.Id,
            line.PaymentVoucherId,
            line.LineNumber,
            line.AccountId,
            line.Amount,
            line.ReferenceType,
            line.ReferenceId,
            line.Description);
}
