using OAS.Contracts.Accounting.Enums;
using OAS.UiLib.Core.Models.Purchasing;

namespace OAS.Client.Purchasing.Components;

public partial class PurchasingWorkspaceHost
{
    private IReadOnlyList<UiPurchaseInvoicePaymentSettlementModel> ActiveInvoicePaymentSettlements
        => ActiveInvoicePaymentSummary?.Settlements?
            .Select(MapSettlement)
            .ToArray()
            ?? Array.Empty<UiPurchaseInvoicePaymentSettlementModel>();

    private static UiPurchaseInvoicePaymentSettlementModel MapSettlement(
        OAS.Contracts.Purchasing.PurchaseInvoices.PurchaseInvoicePaymentSettlementDto source)
        => new()
        {
            PaymentAllocationId = source.PaymentAllocationId,
            PaymentVoucherId = source.PaymentVoucherId,
            PaymentVoucherLineId = source.PaymentVoucherLineId,
            SourceCurrencyCode = source.SourceCurrencyCode,
            SourceAmount = source.SourceAmount,
            SourceExchangeRate = source.SourceExchangeRate,
            SourceExchangeRateDate = source.SourceExchangeRateDate,
            SourceExchangeRateTypeText = source.SourceExchangeRateType switch
            {
                ExchangeRateType.Accounting => "محاسبي",
                _ => source.SourceExchangeRateType?.ToString()
            },
            SourceExchangeRateSourceText = source.SourceExchangeRateSource switch
            {
                ExchangeRateSource.System => "النظام",
                ExchangeRateSource.Manual => "يدوي",
                _ => source.SourceExchangeRateSource?.ToString()
            },
            SourceBaseAmount = source.SourceBaseAmount,
            TargetDocumentAmount = source.TargetDocumentAmount,
            TargetBaseAmount = source.TargetBaseAmount,
            RealizedExchangeDifferenceBase = source.RealizedExchangeDifferenceBase,
            RealizedExchangeJournalEntryId = source.RealizedExchangeJournalEntryId,
            AllocatedAtUtc = source.AllocatedAtUtc
        };

    private Task OpenInvoiceFxJournalAsync(Guid journalEntryId)
    {
        Navigation.NavigateTo($"/accounting/journals?journalId={journalEntryId:D}");
        return Task.CompletedTask;
    }
}
