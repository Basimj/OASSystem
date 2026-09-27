using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;

using ContractPartyType =
    OAS.Contracts.Accounting.Enums.SettlementPartyType;

using ContractPaymentMethod =
    OAS.Contracts.Accounting.Enums.PaymentMethod;

using ContractRateSource =
    OAS.Contracts.Accounting.Enums.ExchangeRateSource;

using ContractRateType =
    OAS.Contracts.Accounting.Enums.ExchangeRateType;

using ContractStatus =
    OAS.Contracts.Accounting.Enums.ReceiptVoucherStatus;

using DomainPaymentMethod =
    OAS.Domain.Accounting.Enums.PaymentMethod;

namespace OAS.Application.Accounting.ReceiptVouchers.Mapping;

public sealed class ReceiptVoucherMapper
{
    public ReceiptVoucherDto ToRead(
        ReceiptVoucher source)
    {
        return ToRead(
            source,
            source.Lines);
    }


    public ReceiptVoucherDto ToRead(
        ReceiptVoucher source,
        IEnumerable<ReceiptVoucherLine> sourceLines)
    {
        return new ReceiptVoucherDto(
            source.Id,
            source.VoucherNumber,
            source.VoucherDate,

            source.BaseCurrencyId,
            source.BaseCurrencyCodeSnapshot,
            source.BaseCurrencyDecimalPlacesSnapshot,
            source.BaseTotalAmount,

            (ContractStatus)(byte)source.Status,

            source.Description,
            source.JournalEntryId,

            source.CreatedBy,
            source.CreatedAtUtc,

            source.PostedBy,
            source.PostedAtUtc,

            Convert.ToBase64String(
                source.RowVersion),

            sourceLines
                .OrderBy(
                    x => x.LineNumber)
                .Select(
                    ToLineDto)
                .ToArray());
    }


    public static ReceiptVoucherLineDto ToLineDto(
        ReceiptVoucherLine line)
    {
        /*
         * SettlementAccountId ›Ì ﬁ«⁄œ… «·»Ì«‰«  ÂÊ «·Õ”«» «·„Õ«”»Ì
         * «·‰Â«∆Ì «·‰« Ã „‰ ⁄„·Ì… Resolution.
         *
         * ›Ì Õ«·… Cash √Ê Bank ·« ‰—Ìœ ≈—”«·Â ≈·Ï Ê«ÃÂ… «· ⁄œÌ·
         * ﬂ√‰Â «Œ Ì«— „” ﬁ· ··„” Œœ„.
         *
         * Ê≈·« ⁄‰œ ≈⁄«œ… Õ›Ÿ «·”‰œ ” —”· «·Ê«ÃÂ…:
         *
         * CashAccountId + SettlementAccountId
         *
         * √Ê:
         *
         * BankAccountId + SettlementAccountId
         *
         * ÊÂ–« ÂÊ ”»» «·Œÿ√ «·”«»ﬁ.
         *
         * SettlementAccountId Ì⁄ »— ≈œŒ«· „” Œœ„ ›ﬁÿ
         * ⁄‰œ„«  ﬂÊ‰ ÿ—Ìﬁ… «·œ›⁄ Other.
         */
        var editableSettlementAccountId =
            line.PaymentMethod ==
            DomainPaymentMethod.Other
                ? line.SettlementAccountId
                : null;


        return new ReceiptVoucherLineDto(
            line.Id,

            line.ReceiptVoucherId,

            line.LineNumber,

            line.AccountId,


            line.PartyType.HasValue
                ? (ContractPartyType?)
                    (byte)line.PartyType.Value
                : null,

            line.CustomerId,

            line.SupplierId,

            line.EmployeeId,

            line.PartyNameSnapshot,

            line.CounterpartyAccountId,


            line.PaymentMethod.HasValue
                ? (ContractPaymentMethod?)
                    (byte)line.PaymentMethod.Value
                : null,

            line.CashAccountId,

            line.BankAccountId,

            editableSettlementAccountId,


            line.CurrencyId,

            line.CurrencyCodeSnapshot,

            line.CurrencySymbolSnapshot,

            line.CurrencyDecimalPlacesSnapshot,

            line.Amount,

            line.ExchangeRate,

            line.ExchangeRateDate,


            line.ExchangeRateType.HasValue
                ? (ContractRateType?)
                    (byte)line.ExchangeRateType.Value
                : null,


            line.ExchangeRateSource.HasValue
                ? (ContractRateSource?)
                    (byte)line.ExchangeRateSource.Value
                : null,


            line.BaseAmount,


            line.ReferenceNumber,

            line.ReferenceDate,

            line.ReferenceType,

            line.ReferenceId,

            line.Description);
    }
}