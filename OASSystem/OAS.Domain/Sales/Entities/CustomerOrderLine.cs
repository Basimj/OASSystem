using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class CustomerOrderLine : AuditableEntity<Guid>
{
    private CustomerOrderLine() { }

    private CustomerOrderLine(Guid id, Guid customerOrderId, int lineNumber, Guid? groupId, SalesLineType lineType,
        Guid? productVariantId, Guid? warehouseId, string descriptionSnapshot, decimal quantity,
        decimal baseUnitPrice, decimal actualUnitPrice, SalesDiscountType discountType, decimal? discountValue,
        decimal? taxRate, Guid? prescriptionRevisionId, EyeSide? prescriptionEye, bool requiresProduction,
        string? notes, TaxCalculationMode taxCalculationMode, byte currencyDecimalPlaces)
    {
        Id = SalesDomainGuard.Required(id, "Customer order line id");
        CustomerOrderId = SalesDomainGuard.Required(customerOrderId, "Customer order id");
        if (lineNumber <= 0) throw new DomainException("Line number must be greater than zero.");
        LineNumber = lineNumber;
        GroupId = groupId;
        LineType = lineType;
        SetItemReferences(productVariantId, warehouseId, prescriptionRevisionId, prescriptionEye);
        DescriptionSnapshot = SalesDomainGuard.Required(descriptionSnapshot, 500, "Description snapshot");
        RequiresProduction = requiresProduction;
        Notes = SalesDomainGuard.Optional(notes, 1000, "Line notes");
        IsActive = true;
        UpdatePricing(quantity, baseUnitPrice, actualUnitPrice, discountType, discountValue, taxRate, taxCalculationMode, currencyDecimalPlaces);
    }

    public Guid CustomerOrderId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid? GroupId { get; private set; }
    public SalesLineType LineType { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public string DescriptionSnapshot { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal BaseUnitPrice { get; private set; }
    public decimal ActualUnitPrice { get; private set; }
    public SalesDiscountType DiscountType { get; private set; }
    public decimal? DiscountValue { get; private set; }
    /// <summary>Line-level discount only.</summary>
    public decimal DiscountAmount { get; private set; }
    public decimal HeaderDiscountAllocated { get; private set; }
    public decimal? TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal FinalAmount { get; private set; }
    public Guid? PrescriptionRevisionId { get; private set; }
    public EyeSide? PrescriptionEye { get; private set; }
    public bool RequiresProduction { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public decimal GrossAmount => Quantity * ActualUnitPrice;
    public decimal NetBeforeHeaderDiscount => Math.Max(0m, GrossAmount - DiscountAmount);
    public bool RequiresInventory => LineType != SalesLineType.Service && ProductVariantId.HasValue && WarehouseId.HasValue;

    public static CustomerOrderLine Create(Guid id, Guid customerOrderId, int lineNumber, Guid? groupId, SalesLineType lineType,
        Guid? productVariantId, Guid? warehouseId, string descriptionSnapshot, decimal quantity,
        decimal baseUnitPrice, decimal actualUnitPrice, SalesDiscountType discountType, decimal? discountValue,
        decimal? taxRate, Guid? prescriptionRevisionId, EyeSide? prescriptionEye, bool requiresProduction,
        string? notes, TaxCalculationMode taxCalculationMode, byte currencyDecimalPlaces) =>
        new(id, customerOrderId, lineNumber, groupId, lineType, productVariantId, warehouseId, descriptionSnapshot,
            quantity, baseUnitPrice, actualUnitPrice, discountType, discountValue, taxRate, prescriptionRevisionId,
            prescriptionEye, requiresProduction, notes, taxCalculationMode, currencyDecimalPlaces);

    internal void UpdatePricing(decimal quantity, decimal baseUnitPrice, decimal actualUnitPrice, SalesDiscountType discountType,
        decimal? discountValue, decimal? taxRate, TaxCalculationMode taxCalculationMode, byte currencyDecimalPlaces)
    {
        if (baseUnitPrice < 0) throw new DomainException("Base unit price cannot be negative.");
        var amounts = SalesPricingCalculator.Calculate(quantity, actualUnitPrice, discountType, discountValue, taxRate, taxCalculationMode, currencyDecimalPlaces);
        Quantity = quantity;
        BaseUnitPrice = baseUnitPrice;
        ActualUnitPrice = actualUnitPrice;
        DiscountType = discountType;
        DiscountValue = discountType == SalesDiscountType.None ? null : discountValue;
        DiscountAmount = amounts.DiscountAmount;
        HeaderDiscountAllocated = 0m;
        TaxRate = taxRate;
        TaxAmount = amounts.TaxAmount;
        NetAmount = amounts.NetAmount;
        FinalAmount = amounts.FinalAmount;
    }

    internal void ApplyHeaderDiscountAllocation(decimal allocation, TaxCalculationMode mode, byte decimals)
    {
        if (allocation < 0m || allocation > NetBeforeHeaderDiscount)
            throw new DomainException("Header discount allocation must be between zero and the line amount after line discount.");
        HeaderDiscountAllocated = SalesPricingCalculator.Round(allocation, decimals);
        var after = SalesPricingCalculator.Round(NetBeforeHeaderDiscount - HeaderDiscountAllocated, decimals);
        var rate = TaxRate ?? 0m;
        if (mode == TaxCalculationMode.Exclusive)
        {
            NetAmount = after;
            TaxAmount = SalesPricingCalculator.Round(NetAmount * rate / 100m, decimals);
            FinalAmount = SalesPricingCalculator.Round(NetAmount + TaxAmount, decimals);
        }
        else
        {
            FinalAmount = after;
            TaxAmount = rate == 0m ? 0m : SalesPricingCalculator.Round(FinalAmount - FinalAmount / (1m + rate / 100m), decimals);
            NetAmount = SalesPricingCalculator.Round(FinalAmount - TaxAmount, decimals);
        }
    }

    public void EnsurePrescriptionReference(bool prescriptionRequired)
    {
        if (prescriptionRequired && (!PrescriptionRevisionId.HasValue || !PrescriptionEye.HasValue))
            throw new DomainException("A prescription revision and eye are required for this sales line.");
    }
    public void EnsureOpticalMeasurementReference(bool opticalMeasurementRequired, bool hasOpticalSnapshot)
    {
        if (!opticalMeasurementRequired) return;
        if (!(PrescriptionRevisionId.HasValue && PrescriptionEye.HasValue) && !hasOpticalSnapshot)
            throw new DomainException("An optical snapshot or stored prescription reference is required for this sales line.");
    }
    internal void SetActive(bool isActive) => IsActive = isActive;
    internal void SetLineNumber(int lineNumber)
    {
        if (lineNumber <= 0) throw new DomainException("Line number must be greater than zero.");
        LineNumber = lineNumber;
    }

    private void SetItemReferences(Guid? productVariantId, Guid? warehouseId, Guid? prescriptionRevisionId, EyeSide? prescriptionEye)
    {
        SalesDomainGuard.Defined(LineType, "Sales line type");
        if (LineType is SalesLineType.Frame or SalesLineType.Lens or SalesLineType.Accessory)
        {
            if (!productVariantId.HasValue || productVariantId.Value == Guid.Empty) throw new DomainException("Product variant is required for product sales lines.");
            if (warehouseId == Guid.Empty) throw new DomainException("Warehouse id cannot be empty when provided.");
        }
        if (LineType == SalesLineType.Service && (productVariantId.HasValue || warehouseId.HasValue))
            throw new DomainException("Service lines cannot reference a product variant or warehouse.");
        if (prescriptionRevisionId.HasValue != prescriptionEye.HasValue)
            throw new DomainException("Prescription revision and prescription eye must be provided together.");
        if (prescriptionRevisionId == Guid.Empty) throw new DomainException("Prescription revision id cannot be empty.");
        if (prescriptionEye.HasValue) SalesDomainGuard.Defined(prescriptionEye.Value, "Prescription eye");
        ProductVariantId = productVariantId;
        WarehouseId = warehouseId;
        PrescriptionRevisionId = prescriptionRevisionId;
        PrescriptionEye = prescriptionEye;
    }
}
