using OAS.Contracts.Accounting.PaymentVouchers;
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
    OAS.Contracts.Accounting.Enums.PaymentVoucherStatus;

using DomainPaymentMethod =
    OAS.Domain.Accounting.Enums.PaymentMethod;

namespace OAS.Application.Accounting.PaymentVouchers.Mapping;

public sealed class PaymentVoucherMapper
{
    public PaymentVoucherDto ToRead(
        PaymentVoucher source)
    {
        return ToRead(
            source,
            source.Lines);
    }


    public PaymentVoucherDto ToRead(
        PaymentVoucher source,
        IEnumerable<PaymentVoucherLine> sourceLines)
    {
        return new PaymentVoucherDto(
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


    public static PaymentVoucherLineDto ToLineDto(
        PaymentVoucherLine line)
    {
        /*
         * SettlementAccountId „Õ›ÊŸ ›Ì ﬁ«⁄œ… «·»Ì«‰« 
         * ﬂÕ”«» GL «·‰Â«∆Ì «·„” Œœ„ ›Ì «· ”ÊÌ….
         *
         * ⁄‰œ Cash √Ê Bank ÂÊ ﬁÌ„… „Õ”Ê»… „‰ «·”Ì—›—
         * Ê·Ì” «Œ Ì«—« „” ﬁ·« ··„” Œœ„.
         *
         * ·–·ﬂ ·« ‰⁄ÌœÂ ··Ê«ÃÂ… ≈·« ⁄‰œ„«  ﬂÊ‰
         * ÿ—Ìﬁ… «·œ›⁄ Other.
         */
        var editableSettlementAccountId =
            line.PaymentMethod ==
            DomainPaymentMethod.Other
                ? line.SettlementAccountId
                : null;


        return new PaymentVoucherLineDto(
            line.Id,

            line.PaymentVoucherId,

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