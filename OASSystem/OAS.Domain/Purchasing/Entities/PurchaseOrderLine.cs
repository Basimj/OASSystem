using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Rules;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseOrderLine : AuditableEntity<Guid>
{
    private PurchaseOrderLine() { }

    private PurchaseOrderLine(
        Guid id,
        Guid purchaseOrderId,
        int lineSequence,
        Guid productVariantId,
        Guid? supplierCatalogItemId,
        Guid purchaseUnitId,
        decimal unitConversionFactor,
        string? productCodeSnapshot,
        string productNameSnapshot,
        string? unitNameSnapshot,
        decimal orderedQuantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal taxRate,
        TaxCalculationMode taxCalculationMode,
        DateOnly? expectedDeliveryDate,
        string? notes)
    {
        Id = PurchasingDomainGuard.Required(id, "Purchase order line id");
        PurchaseOrderId = PurchasingDomainGuard.Required(purchaseOrderId, "Purchase order id");
        SetValues(lineSequence, productVariantId, supplierCatalogItemId, purchaseUnitId, unitConversionFactor,
            productCodeSnapshot, productNameSnapshot, unitNameSnapshot, orderedQuantity, unitPrice, discountAmount,
            taxRate, taxCalculationMode, expectedDeliveryDate, notes);
    }

    public Guid PurchaseOrderId { get; private set; }
    public int LineSequence { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public Guid? SupplierCatalogItemId { get; private set; }
    public Guid PurchaseUnitId { get; private set; }
    public decimal UnitConversionFactor { get; private set; }
    public string? ProductCodeSnapshot { get; private set; }
    public string ProductNameSnapshot { get; private set; } = string.Empty;
    public string? UnitNameSnapshot { get; private set; }
    public decimal OrderedQuantity { get; private set; }
    public decimal BaseQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal FinalAmount { get; private set; }
    public DateOnly? ExpectedDeliveryDate { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PurchaseOrderLine Create(
        Guid id,
        Guid purchaseOrderId,
        int lineSequence,
        Guid productVariantId,
        Guid? supplierCatalogItemId,
        Guid purchaseUnitId,
        decimal unitConversionFactor,
        string? productCodeSnapshot,
        string productNameSnapshot,
        string? unitNameSnapshot,
        decimal orderedQuantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal taxRate,
        TaxCalculationMode taxCalculationMode,
        DateOnly? expectedDeliveryDate,
        string? notes) =>
        new(id, purchaseOrderId, lineSequence, productVariantId, supplierCatalogItemId, purchaseUnitId,
            unitConversionFactor, productCodeSnapshot, productNameSnapshot, unitNameSnapshot, orderedQuantity,
            unitPrice, discountAmount, taxRate, taxCalculationMode, expectedDeliveryDate, notes);

    public void UpdateCommercials(
        decimal orderedQuantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal taxRate,
        TaxCalculationMode taxCalculationMode,
        DateOnly? expectedDeliveryDate,
        string? notes)
    {
        CalculateAmounts(orderedQuantity, unitPrice, discountAmount, taxRate, taxCalculationMode);
        ExpectedDeliveryDate = expectedDeliveryDate;
        Notes = PurchasingDomainGuard.Optional(notes, 500, "Line notes");
    }

    private void SetValues(
        int lineSequence,
        Guid productVariantId,
        Guid? supplierCatalogItemId,
        Guid purchaseUnitId,
        decimal unitConversionFactor,
        string? productCodeSnapshot,
        string productNameSnapshot,
        string? unitNameSnapshot,
        decimal orderedQuantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal taxRate,
        TaxCalculationMode taxCalculationMode,
        DateOnly? expectedDeliveryDate,
        string? notes)
    {
        if (lineSequence <= 0) throw new DomainException("Line sequence must be greater than zero.");
        if (supplierCatalogItemId == Guid.Empty) throw new DomainException("Supplier catalog item id cannot be empty.");
        LineSequence = lineSequence;
        ProductVariantId = PurchasingDomainGuard.Required(productVariantId, "Product variant id");
        SupplierCatalogItemId = supplierCatalogItemId;
        PurchaseUnitId = PurchasingDomainGuard.Required(purchaseUnitId, "Purchase unit id");
        PurchasingDomainGuard.Positive(unitConversionFactor, "Unit conversion factor");
        UnitConversionFactor = unitConversionFactor;
        ProductCodeSnapshot = PurchasingDomainGuard.Optional(productCodeSnapshot, 64, "Product code snapshot");
        ProductNameSnapshot = PurchasingDomainGuard.Required(productNameSnapshot, 200, "Product name snapshot");
        UnitNameSnapshot = PurchasingDomainGuard.Optional(unitNameSnapshot, 100, "Unit name snapshot");
        CalculateAmounts(orderedQuantity, unitPrice, discountAmount, taxRate, taxCalculationMode);
        ExpectedDeliveryDate = expectedDeliveryDate;
        Notes = PurchasingDomainGuard.Optional(notes, 500, "Line notes");
    }

    private void CalculateAmounts(
        decimal orderedQuantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal taxRate,
        TaxCalculationMode taxCalculationMode)
    {
        PurchasingDomainGuard.Positive(orderedQuantity, "Ordered quantity");
        PurchasingDomainGuard.NonNegative(unitPrice, "Unit price");
        PurchasingDomainGuard.NonNegative(discountAmount, "Discount amount");
        PurchasingDomainGuard.NonNegative(taxRate, "Tax rate");
        if (!Enum.IsDefined(taxCalculationMode)) throw new DomainException("Tax calculation mode is invalid.");

        var amounts = SalesPricingCalculator.Calculate(
            orderedQuantity,
            unitPrice,
            discountAmount == 0m ? SalesDiscountType.None : SalesDiscountType.FixedAmount,
            discountAmount == 0m ? null : discountAmount,
            taxRate,
            taxCalculationMode,
            4);

        OrderedQuantity = orderedQuantity;
        BaseQuantity = Math.Round(orderedQuantity * UnitConversionFactor, 3);
        UnitPrice = unitPrice;
        DiscountAmount = amounts.DiscountAmount;
        NetAmount = amounts.NetAmount;
        TaxRate = taxRate;
        TaxAmount = amounts.TaxAmount;
        FinalAmount = amounts.FinalAmount;
    }
}
