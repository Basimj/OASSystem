using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Domain.Accounting.Entities;
using ContractPaymentMethod = OAS.Contracts.Accounting.Enums.PaymentMethod;
using ContractPaymentPartyType = OAS.Contracts.Accounting.Enums.PaymentPartyType;
using ContractPaymentVoucherStatus = OAS.Contracts.Accounting.Enums.PaymentVoucherStatus;

namespace OAS.Application.Accounting.PaymentVouchers.Mapping;

public sealed class PaymentVoucherMapper
{
    public PaymentVoucherDto ToRead(PaymentVoucher source)
    {
        var lines = source.Lines
            .Select(l => new PaymentVoucherLineDto(
                l.Id,
                l.PaymentVoucherId,
                l.LineNumber,
                l.AccountId,
                l.Amount,
                l.ReferenceType,
                l.ReferenceId,
                l.Description))
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
            source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty,
            lines);
    }
}
