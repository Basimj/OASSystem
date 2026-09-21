using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;
using ContractPaymentMethod = OAS.Contracts.Accounting.Enums.PaymentMethod;
using ContractReceiptPartyType = OAS.Contracts.Accounting.Enums.ReceiptPartyType;
using ContractReceiptVoucherStatus = OAS.Contracts.Accounting.Enums.ReceiptVoucherStatus;

namespace OAS.Application.Accounting.ReceiptVouchers.Mapping;

public sealed class ReceiptVoucherMapper
{
    public ReceiptVoucherDto ToRead(ReceiptVoucher source)
    {
        var lines = source.Lines
            .Select(l => new ReceiptVoucherLineDto(
                l.Id,
                l.ReceiptVoucherId,
                l.LineNumber,
                l.AccountId,
                l.Amount,
                l.ReferenceType,
                l.ReferenceId,
                l.Description))
            .ToList();

        return new ReceiptVoucherDto(
            source.Id,
            source.VoucherNumber,
            source.VoucherDate,
            (ContractReceiptPartyType)(int)source.PartyType,
            source.CustomerId,
            source.ReceivedFrom,
            (ContractPaymentMethod)(int)source.PaymentMethod,
            source.CashAccountId,
            source.BankAccountId,
            source.TotalAmount,
            (ContractReceiptVoucherStatus)(int)source.Status,
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
