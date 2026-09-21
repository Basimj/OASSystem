using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;
using ContractPaymentMethod = OAS.Contracts.Accounting.Enums.PaymentMethod;
using ContractReceiptPartyType = OAS.Contracts.Accounting.Enums.ReceiptPartyType;
using ContractReceiptVoucherStatus = OAS.Contracts.Accounting.Enums.ReceiptVoucherStatus;

namespace OAS.Application.Accounting.ReceiptVouchers.Mapping;

public sealed class ReceiptVoucherMapper
{
    public ReceiptVoucherDto ToRead(ReceiptVoucher source)
        => ToRead(source, source.Lines);

    public ReceiptVoucherDto ToRead(
        ReceiptVoucher source,
        IEnumerable<ReceiptVoucherLine> sourceLines)
    {
        var lines = sourceLines
            .OrderBy(l => l.LineNumber)
            .Select(ToLineDto)
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
            Convert.ToBase64String(source.RowVersion),
            lines);
    }

    public static ReceiptVoucherLineDto ToLineDto(ReceiptVoucherLine line)
        => new(
            line.Id,
            line.ReceiptVoucherId,
            line.LineNumber,
            line.AccountId,
            line.Amount,
            line.ReferenceType,
            line.ReferenceId,
            line.Description);
}
