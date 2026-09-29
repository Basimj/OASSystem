using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Rules;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseInvoiceLine : AuditableEntity<Guid>
{
    private PurchaseInvoiceLine() { }

    private PurchaseInvoiceLine(
        Guid id,
        Guid purchaseInvoiceId,
        int lineSequence,
        Guid? purchaseOrderLineId,
        Guid productVariantId,
        string? productCodeSnapshot,
        string descriptionSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal taxRate,
        TaxCalculationMode taxCalculationMode,
        decimal exchangeRate)
    {
        Id = PurchasingDomainGuard.Required(id, "Purchase invoice line id");
        PurchaseInvoiceId = PurchasingDomainGuard.Required(purchaseInvoiceId, "Purchase invoice id");
        SetValues(lineSequence, purchaseOrderLineId, productVariantId, productCodeSnapshot, descriptionSnapshot,
            quantity, unitPrice, discountAmount, taxRate, taxCalculationMode, exchangeRate);
    }

    public Guid PurchaseInvoiceId { get; private set; }
    public int LineSequence { get; private set; }
    public Guid? PurchaseOrderLineId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string? ProductCodeSnapshot { get; private set; }
    public string DescriptionSnapshot { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal FinalAmount { get; private set; }
    public decimal BaseNetAmount { get; private set; }
    public decimal BaseTaxAmount { get; private set; }
    public decimal BaseFinalAmount { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PurchaseInvoiceLine Create(
        Guid id,
        Guid purchaseInvoiceId,
        int lineSequence,
        Guid? purchaseOrderLineId,
        Guid productVariantId,
        string? productCodeSnapshot,
        string descriptionSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal taxRate,
        TaxCalculationMode taxCalculationMode,
        decimal exchangeRate) =>
        new(id, purchaseInvoiceId, lineSequence, purchaseOrderLineId, productVariantId, productCodeSnapshot,
            descriptionSnapshot, quantity, unitPrice, discountAmount, taxRate, taxCalculationMode, exchangeRate);

    public void Update(
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal taxRate,
        TaxCalculationMode taxCalculationMode,
        decimal exchangeRate) =>
        Calculate(quantity, unitPrice, discountAmount, taxRate, taxCalculationMode, exchangeRate);

    private void SetValues(
        int lineSequence,
        Guid? purchaseOrderLineId,
        Guid productVariantId,
        string? productCodeSnapshot,
        string descriptionSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal taxRate,
        TaxCalculationMode taxCalculationMode,
        decimal exchangeRate)
    {
        if (lineSequence <= 0) throw new DomainException("Line sequence must be greater than zero.");
        if (purchaseOrderLineId == Guid.Empty) throw new DomainException("Purchase order line id cannot be empty.");
        LineSequence = lineSequence;
        PurchaseOrderLineId = purchaseOrderLineId;
        ProductVariantId = PurchasingDomainGuard.Required(productVariantId, "Product variant id");
        ProductCodeSnapshot = PurchasingDomainGuard.Optional(productCodeSnapshot, 64, "Product code snapshot");
        DescriptionSnapshot = PurchasingDomainGuard.Required(descriptionSnapshot, 250, "Description snapshot");
        Calculate(quantity, unitPrice, discountAmount, taxRate, taxCalculationMode, exchangeRate);
    }

    private void Calculate(
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal taxRate,
        TaxCalculationMode taxCalculationMode,
        decimal exchangeRate)
    {
        PurchasingDomainGuard.Positive(quantity, "Quantity");
        PurchasingDomainGuard.NonNegative(unitPrice, "Unit price");
        PurchasingDomainGuard.NonNegative(discountAmount, "Discount amount");
        PurchasingDomainGuard.NonNegative(taxRate, "Tax rate");
        PurchasingDomainGuard.Positive(exchangeRate, "Exchange rate");
        if (!Enum.IsDefined(taxCalculationMode)) throw new DomainException("Tax calculation mode is invalid.");

        var amounts = SalesPricingCalculator.Calculate(
            quantity,
            unitPrice,
            discountAmount == 0m ? SalesDiscountType.None : SalesDiscountType.FixedAmount,
            discountAmount == 0m ? null : discountAmount,
            taxRate,
            taxCalculationMode,
            4);

        Quantity = quantity;
        UnitPrice = unitPrice;
        GrossAmount = amounts.GrossAmount;
        DiscountAmount = amounts.DiscountAmount;
        NetAmount = amounts.NetAmount;
        TaxRate = taxRate;
        TaxAmount = amounts.TaxAmount;
        FinalAmount = amounts.FinalAmount;
        BaseNetAmount = SalesPricingCalculator.ConvertToBase(NetAmount, exchangeRate, 4);
        BaseTaxAmount = SalesPricingCalculator.ConvertToBase(TaxAmount, exchangeRate, 4);
        BaseFinalAmount = SalesPricingCalculator.ConvertToBase(FinalAmount, exchangeRate, 4);
    }
}
