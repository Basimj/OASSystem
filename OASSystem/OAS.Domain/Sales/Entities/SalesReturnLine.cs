using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class SalesReturnLine : AuditableEntity<Guid>
{
    private SalesReturnLine() { }

    private SalesReturnLine(
        Guid id,
        Guid salesReturnId,
        int lineNumber,
        Guid salesInvoiceLineId,
        SalesLineType lineType,
        Guid? productVariantId,
        Guid? warehouseId,
        string? productCodeSnapshot,
        string productNameSnapshot,
        decimal quantity,
        decimal netAmount,
        decimal taxAmount,
        decimal finalAmount,
        decimal baseNetAmount,
        decimal baseTaxAmount,
        decimal baseFinalAmount,
        decimal? unitCostSnapshot,
        decimal? totalCostSnapshot)
    {
        Id = SalesDomainGuard.Required(id, "Sales return line id");
        SalesReturnId = SalesDomainGuard.Required(salesReturnId, "Sales return id");
        SalesInvoiceLineId = SalesDomainGuard.Required(salesInvoiceLineId, "Sales invoice line id");
        if (lineNumber <= 0) throw new DomainException("Line number must be greater than zero.");
        if (quantity <= 0m) throw new DomainException("Return quantity must be greater than zero.");
        if (netAmount < 0m || taxAmount < 0m || finalAmount < 0m ||
            baseNetAmount < 0m || baseTaxAmount < 0m || baseFinalAmount < 0m)
            throw new DomainException("Sales return amounts cannot be negative.");
        if (finalAmount != netAmount + taxAmount)
            throw new DomainException("Sales return line total must equal net plus tax.");
        if (baseFinalAmount != baseNetAmount + baseTaxAmount)
            throw new DomainException("Sales return base total must equal base net plus base tax.");
        if (productVariantId == Guid.Empty || warehouseId == Guid.Empty)
            throw new DomainException("Optional inventory identifiers cannot be empty GUID values.");
        if ((productVariantId.HasValue || warehouseId.HasValue) && (!productVariantId.HasValue || !warehouseId.HasValue))
            throw new DomainException("Inventory return lines require both product variant and warehouse.");
        if (unitCostSnapshot < 0m || totalCostSnapshot < 0m)
            throw new DomainException("Inventory cost snapshot cannot be negative.");
        if (productVariantId.HasValue && (!unitCostSnapshot.HasValue || !totalCostSnapshot.HasValue))
            throw new DomainException("Inventory return lines require a cost snapshot.");

        LineNumber = lineNumber;
        LineType = lineType;
        ProductVariantId = productVariantId;
        WarehouseId = warehouseId;
        ProductCodeSnapshot = SalesDomainGuard.Optional(productCodeSnapshot, 64, "Product code snapshot");
        ProductNameSnapshot = SalesDomainGuard.Required(productNameSnapshot, 250, "Product name snapshot");
        Quantity = quantity;
        NetAmount = netAmount;
        TaxAmount = taxAmount;
        FinalAmount = finalAmount;
        BaseNetAmount = baseNetAmount;
        BaseTaxAmount = baseTaxAmount;
        BaseFinalAmount = baseFinalAmount;
        UnitCostSnapshot = unitCostSnapshot;
        TotalCostSnapshot = totalCostSnapshot;
        IsActive = true;
    }

    public Guid SalesReturnId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid SalesInvoiceLineId { get; private set; }
    public SalesLineType LineType { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public string? ProductCodeSnapshot { get; private set; }
    public string ProductNameSnapshot { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal FinalAmount { get; private set; }
    public decimal BaseNetAmount { get; private set; }
    public decimal BaseTaxAmount { get; private set; }
    public decimal BaseFinalAmount { get; private set; }
    public decimal? UnitCostSnapshot { get; private set; }
    public decimal? TotalCostSnapshot { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public bool RequiresInventory => ProductVariantId.HasValue && WarehouseId.HasValue;

    public static SalesReturnLine Create(
        Guid id,
        Guid salesReturnId,
        int lineNumber,
        Guid salesInvoiceLineId,
        SalesLineType lineType,
        Guid? productVariantId,
        Guid? warehouseId,
        string? productCodeSnapshot,
        string productNameSnapshot,
        decimal quantity,
        decimal netAmount,
        decimal taxAmount,
        decimal finalAmount,
        decimal baseNetAmount,
        decimal baseTaxAmount,
        decimal baseFinalAmount,
        decimal? unitCostSnapshot,
        decimal? totalCostSnapshot) =>
        new(id, salesReturnId, lineNumber, salesInvoiceLineId, lineType, productVariantId, warehouseId,
            productCodeSnapshot, productNameSnapshot, quantity, netAmount, taxAmount, finalAmount,
            baseNetAmount, baseTaxAmount, baseFinalAmount, unitCostSnapshot, totalCostSnapshot);

    internal void Deactivate() => IsActive = false;
}
