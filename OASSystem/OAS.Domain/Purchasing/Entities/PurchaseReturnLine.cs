using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class PurchaseReturnLine : AuditableEntity<Guid>
{
    private PurchaseReturnLine() { }

    private PurchaseReturnLine(
        Guid id,
        Guid purchaseReturnId,
        int lineNumber,
        Guid purchaseReceiptLineId,
        Guid? purchaseInvoiceLineId,
        Guid productVariantId,
        decimal quantity,
        decimal baseQuantity,
        decimal receiptUnitCostBase,
        decimal supplierNetBaseAmount,
        decimal supplierTaxBaseAmount)
    {
        Id = PurchasingDomainGuard.Required(id, "Purchase return line id");
        PurchaseReturnId = PurchasingDomainGuard.Required(purchaseReturnId, "Purchase return id");
        PurchaseReceiptLineId = PurchasingDomainGuard.Required(purchaseReceiptLineId, "Purchase receipt line id");
        if (purchaseInvoiceLineId == Guid.Empty) throw new DomainException("Purchase invoice line id cannot be empty.");
        PurchaseInvoiceLineId = purchaseInvoiceLineId;
        ProductVariantId = PurchasingDomainGuard.Required(productVariantId, "Product variant id");
        if (lineNumber <= 0) throw new DomainException("Line number must be greater than zero.");
        PurchasingDomainGuard.Positive(quantity, "Return quantity");
        PurchasingDomainGuard.Positive(baseQuantity, "Base return quantity");
        PurchasingDomainGuard.NonNegative(receiptUnitCostBase, "Receipt unit cost");
        PurchasingDomainGuard.NonNegative(supplierNetBaseAmount, "Supplier net amount");
        PurchasingDomainGuard.NonNegative(supplierTaxBaseAmount, "Supplier tax amount");

        LineNumber = lineNumber;
        Quantity = quantity;
        BaseQuantity = baseQuantity;
        ReceiptUnitCostBase = receiptUnitCostBase;
        ReceiptCostBaseAmount = Math.Round(baseQuantity * receiptUnitCostBase, 4);
        SupplierNetBaseAmount = Math.Round(supplierNetBaseAmount, 4);
        SupplierTaxBaseAmount = Math.Round(supplierTaxBaseAmount, 4);
        SupplierGrossBaseAmount = Math.Round(SupplierNetBaseAmount + SupplierTaxBaseAmount, 4);
        IsActive = true;
    }

    public Guid PurchaseReturnId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid PurchaseReceiptLineId { get; private set; }
    public Guid? PurchaseInvoiceLineId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal BaseQuantity { get; private set; }
    public decimal ReceiptUnitCostBase { get; private set; }
    public decimal ReceiptCostBaseAmount { get; private set; }
    public decimal SupplierNetBaseAmount { get; private set; }
    public decimal SupplierTaxBaseAmount { get; private set; }
    public decimal SupplierGrossBaseAmount { get; private set; }
    public decimal? InventoryUnitCostBase { get; private set; }
    public decimal? InventoryCostBaseAmount { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PurchaseReturnLine Create(
        Guid id, Guid purchaseReturnId, int lineNumber, Guid purchaseReceiptLineId, Guid? purchaseInvoiceLineId,
        Guid productVariantId, decimal quantity, decimal baseQuantity, decimal receiptUnitCostBase,
        decimal supplierNetBaseAmount, decimal supplierTaxBaseAmount) =>
        new(id, purchaseReturnId, lineNumber, purchaseReceiptLineId, purchaseInvoiceLineId, productVariantId,
            quantity, baseQuantity, receiptUnitCostBase, supplierNetBaseAmount, supplierTaxBaseAmount);

    public void SetInventoryCostSnapshot(decimal unitCostBase)
    {
        PurchasingDomainGuard.NonNegative(unitCostBase, "Inventory unit cost");
        InventoryUnitCostBase = unitCostBase;
        InventoryCostBaseAmount = Math.Round(BaseQuantity * unitCostBase, 4);
    }

    internal void Deactivate() => IsActive = false;
}
