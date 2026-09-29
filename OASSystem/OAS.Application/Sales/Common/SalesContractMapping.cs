using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Contracts.Sales.PriceOverrides;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Common;

internal static class SalesContractMapping
{
    public static PrescriptionEyeDetailDto Eye(PrescriptionEyeDetail x) => new(
        x.Id, x.PrescriptionRevisionId,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.EyeSide>(x.Eye), x.SPH, x.CYL, x.Axis, x.ADD, x.Prism,
        x.PrismBase.HasValue ? SalesEnumMap.To<OAS.Contracts.Sales.Enums.PrismBaseDirection>(x.PrismBase.Value) : null,
        x.PD, x.MonocularPD, x.VA, x.FittingHeight, x.Notes, x.IsActive,
        Convert.ToBase64String(x.RowVersion), x.CreatedAtUtc, x.CreatedBy, x.LastModifiedAtUtc, x.LastModifiedBy);

    public static PrescriptionRevisionDto Revision(PrescriptionRevision x) => new(
        x.Id, x.PrescriptionId, x.RevisionNumber, x.EffectiveDate, x.Reason, x.IsCurrent, x.IsActive,
        Convert.ToBase64String(x.RowVersion), x.CreatedAtUtc, x.CreatedBy, x.LastModifiedAtUtc, x.LastModifiedBy,
        x.EyeDetails.OrderBy(e => (byte)e.Eye).Select(Eye).ToArray());

    public static PrescriptionDto Prescription(Prescription x, string? customerCode = null, string? customerName = null) => new(
        x.Id, x.PrescriptionCode, x.CustomerId, customerCode, customerName, x.PrescriptionDate,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.PrescriptionStatus>(x.Status), x.PrescribedBy, x.ClinicName,
        x.Notes, x.IsActive, Convert.ToBase64String(x.RowVersion), x.CreatedAtUtc, x.CreatedBy,
        x.LastModifiedAtUtc, x.LastModifiedBy, x.Revisions.OrderByDescending(r => r.RevisionNumber).Select(Revision).ToArray());

    public static CustomerOrderLineDto OrderLine(CustomerOrderLine x) => new(
        x.Id, x.CustomerOrderId, x.LineNumber, x.GroupId,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.SalesLineType>(x.LineType), x.ProductVariantId, x.WarehouseId,
        x.DescriptionSnapshot, x.Quantity, x.BaseUnitPrice, x.ActualUnitPrice,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.SalesDiscountType>(x.DiscountType), x.DiscountValue, x.DiscountAmount,
        x.TaxRate, x.TaxAmount, x.NetAmount, x.FinalAmount, x.PrescriptionRevisionId,
        x.PrescriptionEye.HasValue ? SalesEnumMap.To<OAS.Contracts.Sales.Enums.EyeSide>(x.PrescriptionEye.Value) : null,
        x.RequiresProduction, x.Notes, x.IsActive, Convert.ToBase64String(x.RowVersion));

    public static CustomerOrderDto Order(CustomerOrder x, string? customerCode = null, string? customerName = null) => new(
        x.Id, x.OrderCode, x.CustomerId, customerCode, customerName, x.PrescriptionRevisionId, x.OrderDate, x.RequiredDate,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.CustomerOrderStatus>(x.Status), x.CurrencyId, x.CurrencyCodeSnapshot,
        x.CurrencySymbolSnapshot, x.CurrencyDecimalPlacesSnapshot, x.ExchangeRate, x.ExchangeRateDate,
        (OAS.Contracts.Accounting.Enums.ExchangeRateType)(byte)x.ExchangeRateType,
        (OAS.Contracts.Accounting.Enums.ExchangeRateSource)(byte)x.ExchangeRateSource,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.TaxCalculationMode>(x.TaxCalculationMode),
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.SalesPaymentTermType>(x.PaymentTermType), x.PaymentTermDaysSnapshot,
        x.Subtotal, x.DiscountAmount, x.TaxAmount, x.TotalAmount, x.Notes, x.IsActive, x.ConfirmedAtUtc, x.ConfirmedBy,
        x.CancelledAtUtc, x.CancelledBy, Convert.ToBase64String(x.RowVersion), x.CreatedAtUtc, x.CreatedBy,
        x.LastModifiedAtUtc, x.LastModifiedBy, x.Lines.OrderBy(l => l.LineNumber).Select(OrderLine).ToArray());

    public static SalesInvoiceLinePrescriptionSnapshotDto PrescriptionSnapshot(SalesInvoiceLinePrescriptionSnapshot x) => new(
        x.Id, x.SalesInvoiceLineId, x.PrescriptionRevisionId,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.EyeSide>(x.Eye), x.SPH, x.CYL, x.Axis, x.ADD, x.Prism,
        x.PrismBase.HasValue ? SalesEnumMap.To<OAS.Contracts.Sales.Enums.PrismBaseDirection>(x.PrismBase.Value) : null,
        x.PD, x.MonocularPD, x.VA, x.FittingHeight, x.IsActive, Convert.ToBase64String(x.RowVersion));

    public static SalesInvoiceLineDto InvoiceLine(SalesInvoiceLine x) => new(
        x.Id, x.SalesInvoiceId, x.LineNumber, x.CustomerOrderLineId, x.GroupId,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.SalesLineType>(x.LineType), x.ProductVariantId, x.WarehouseId,
        x.ProductCodeSnapshot, x.ProductNameSnapshot, x.DescriptionSnapshot, x.UnitSnapshot, x.Quantity,
        x.BaseUnitPrice, x.ActualUnitPrice, SalesEnumMap.To<OAS.Contracts.Sales.Enums.SalesDiscountType>(x.DiscountType),
        x.DiscountValue, x.DiscountAmount, x.TaxRate, x.TaxAmount, x.NetAmount, x.FinalAmount,
        x.BaseNetAmount, x.BaseTaxAmount, x.BaseFinalAmount, x.UnitCostSnapshot, x.TotalCostSnapshot,
        x.PrescriptionRevisionId,
        x.PrescriptionEye.HasValue ? SalesEnumMap.To<OAS.Contracts.Sales.Enums.EyeSide>(x.PrescriptionEye.Value) : null,
        x.RequiresProduction, x.Notes, x.IsActive, Convert.ToBase64String(x.RowVersion),
        x.PrescriptionSnapshot is null ? null : PrescriptionSnapshot(x.PrescriptionSnapshot));

    public static SalesInvoiceDto Invoice(
        SalesInvoice x,
        SalesInvoicePaymentSummaryDto payment,
        string? customerCode = null,
        string? customerName = null,
        string? orderCode = null) => new(
        x.Id, x.InvoiceCode, x.CustomerId, customerCode, customerName, x.CustomerOrderId, orderCode,
        x.PrescriptionRevisionId, x.InvoiceDate, x.PostingDate,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.SalesInvoiceStatus>(x.Status), x.CurrencyId, x.CurrencyCodeSnapshot,
        x.CurrencySymbolSnapshot, x.CurrencyDecimalPlacesSnapshot, x.ExchangeRate, x.ExchangeRateDate,
        (OAS.Contracts.Accounting.Enums.ExchangeRateType)(byte)x.ExchangeRateType,
        (OAS.Contracts.Accounting.Enums.ExchangeRateSource)(byte)x.ExchangeRateSource,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.TaxCalculationMode>(x.TaxCalculationMode),
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.SalesPaymentTermType>(x.PaymentTermType), x.PaymentTermDaysSnapshot,
        x.DueDate, x.BaseCurrencyId, x.BaseCurrencyCodeSnapshot, x.BaseCurrencyDecimalPlacesSnapshot,
        x.Subtotal, x.DiscountAmount, x.TaxAmount, x.TotalAmount, x.BaseSubtotal, x.BaseDiscountAmount,
        x.BaseTaxAmount, x.BaseTotalAmount, x.Description, x.JournalEntryId, x.ConfirmedAtUtc, x.ConfirmedBy,
        x.PostedAtUtc, x.PostedBy, x.CancelledAtUtc, x.CancelledBy, x.IsActive, Convert.ToBase64String(x.RowVersion),
        x.CreatedAtUtc, x.CreatedBy, x.LastModifiedAtUtc, x.LastModifiedBy, payment,
        x.Lines.OrderBy(l => l.LineNumber).Select(InvoiceLine).ToArray());

    public static SalesPriceOverrideDto PriceOverride(SalesPriceOverride x) => new(
        x.Id, x.SalesInvoiceId, x.SalesInvoiceLineId, x.OriginalPrice, x.OverridePrice, x.Reason,
        SalesEnumMap.To<OAS.Contracts.Sales.Enums.SalesPriceOverrideStatus>(x.Status), x.RequestedBy, x.RequestedAtUtc,
        x.ApprovedBy, x.ApprovedAtUtc, x.IsActive, Convert.ToBase64String(x.RowVersion), x.CreatedAtUtc, x.CreatedBy,
        x.LastModifiedAtUtc, x.LastModifiedBy);
}
