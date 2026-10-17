namespace OAS.UiLib.Core.Models.Purchasing;

public sealed class UiPurchaseInvoicePaymentSettlementModel
{
    public Guid PaymentAllocationId { get; set; }
    public Guid? PaymentVoucherId { get; set; }
    public Guid? PaymentVoucherLineId { get; set; }
    public string? SourceCurrencyCode { get; set; }
    public decimal SourceAmount { get; set; }
    public decimal? SourceExchangeRate { get; set; }
    public DateOnly? SourceExchangeRateDate { get; set; }
    public string? SourceExchangeRateTypeText { get; set; }
    public string? SourceExchangeRateSourceText { get; set; }
    public decimal SourceBaseAmount { get; set; }
    public decimal TargetDocumentAmount { get; set; }
    public decimal TargetBaseAmount { get; set; }
    public decimal RealizedExchangeDifferenceBase { get; set; }
    public Guid? RealizedExchangeJournalEntryId { get; set; }
    public DateTimeOffset AllocatedAtUtc { get; set; }
}
