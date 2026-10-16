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
        decimal? totalCostSnapshot,
        SalesReturnCondition? returnCondition,
        SalesReturnStockDisposition? stockDisposition)
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
        ReturnCondition = returnCondition;
        StockDisposition = stockDisposition;
        ValidateQualityPolicy();
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
    public SalesReturnCondition? ReturnCondition { get; private set; }
    public SalesReturnStockDisposition? StockDisposition { get; private set; }
    public DateTimeOffset? InspectedAtUtc { get; private set; }
    public string? InspectedBy { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public bool RequiresInventory => ProductVariantId.HasValue && WarehouseId.HasValue;
    public bool RestoresInventoryAsset => RequiresInventory && StockDisposition is SalesReturnStockDisposition.Restock or SalesReturnStockDisposition.Quarantine;

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
        decimal? totalCostSnapshot,
        SalesReturnCondition? returnCondition = null,
        SalesReturnStockDisposition? stockDisposition = null) =>
        new(id, salesReturnId, lineNumber, salesInvoiceLineId, lineType, productVariantId, warehouseId,
            productCodeSnapshot, productNameSnapshot, quantity, netAmount, taxAmount, finalAmount,
            baseNetAmount, baseTaxAmount, baseFinalAmount, unitCostSnapshot, totalCostSnapshot,
            returnCondition, stockDisposition);

    public void RecordInspection(
        SalesReturnCondition condition,
        SalesReturnStockDisposition disposition,
        DateTimeOffset inspectedAtUtc,
        string inspectedBy,
        Guid? destinationWarehouseId = null)
    {
        if (!RequiresInventory)
            throw new DomainException("Only inventory return lines can be inspected.");
        if (StockDisposition != SalesReturnStockDisposition.Quarantine)
            throw new DomainException("Only quarantined return lines can be inspected or released.");
        if (disposition is not (SalesReturnStockDisposition.Quarantine or SalesReturnStockDisposition.Restock))
            throw new DomainException("Inspection can keep stock quarantined or release it back to sellable stock only.");
        if (disposition == SalesReturnStockDisposition.Restock)
        {
            if (condition != SalesReturnCondition.Resellable)
                throw new DomainException("Only a resellable inspected item can be released from quarantine.");
            if (!destinationWarehouseId.HasValue || destinationWarehouseId == Guid.Empty)
                throw new DomainException("A destination warehouse is required when releasing quarantined stock.");
        }
        else if (destinationWarehouseId.HasValue)
        {
            throw new DomainException("A destination warehouse is only allowed when releasing stock from quarantine.");
        }

        ReturnCondition = condition;
        StockDisposition = disposition;
        if (disposition == SalesReturnStockDisposition.Restock)
            WarehouseId = destinationWarehouseId;
        InspectedAtUtc = inspectedAtUtc;
        InspectedBy = SalesDomainGuard.Required(inspectedBy, 64, "Inspector");
        ValidateQualityPolicy();
    }

    private void ValidateQualityPolicy()
    {
        if (!RequiresInventory)
        {
            if (StockDisposition.HasValue && StockDisposition != SalesReturnStockDisposition.NoStock)
                throw new DomainException("Non-inventory return lines must use NoStock disposition when a disposition is supplied.");
            return;
        }

        if (ReturnCondition.HasValue && ReturnCondition == SalesReturnCondition.Unknown)
            throw new DomainException("Inventory return condition cannot be Unknown.");
        if (StockDisposition.HasValue && StockDisposition == SalesReturnStockDisposition.Unknown)
            throw new DomainException("Inventory return stock disposition cannot be Unknown.");
        if (ReturnCondition is SalesReturnCondition.NeedsInspection or SalesReturnCondition.Damaged &&
            StockDisposition is not null and not SalesReturnStockDisposition.Quarantine)
            throw new DomainException("Items requiring inspection or marked damaged must be quarantined first.");
        if (StockDisposition == SalesReturnStockDisposition.Restock && ReturnCondition != SalesReturnCondition.Resellable)
            throw new DomainException("Only resellable returned stock can be restocked directly.");
    }

    internal void Deactivate() => IsActive = false;
}
