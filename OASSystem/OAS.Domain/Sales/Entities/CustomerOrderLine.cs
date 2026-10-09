using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class CustomerOrderLine : AuditableEntity<Guid>
{
    private CustomerOrderLine() { }

    private CustomerOrderLine(
        Guid id, Guid customerOrderId, int lineNumber, Guid? groupId, SalesLineType lineType,
        Guid? productVariantId, Guid? warehouseId, string descriptionSnapshot, decimal quantity,
        decimal baseUnitPrice, decimal actualUnitPrice, SalesDiscountType discountType,
        decimal? discountValue, decimal? taxRate, Guid? prescriptionRevisionId, EyeSide? prescriptionEye,
        bool requiresProduction, string? notes, TaxCalculationMode taxCalculationMode, byte currencyDecimalPlaces)
    {
        Id = SalesDomainGuard.Required(id, "Customer order line id");
        CustomerOrderId = SalesDomainGuard.Required(customerOrderId, "Customer order id");
        if (lineNumber <= 0)
            throw new DomainException("Line number must be greater than zero.");

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
    public decimal DiscountAmount { get; private set; }
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
    public bool RequiresInventory => LineType != SalesLineType.Service && ProductVariantId.HasValue && WarehouseId.HasValue;

    public static CustomerOrderLine Create(
        Guid id, Guid customerOrderId, int lineNumber, Guid? groupId, SalesLineType lineType,
        Guid? productVariantId, Guid? warehouseId, string descriptionSnapshot, decimal quantity,
        decimal baseUnitPrice, decimal actualUnitPrice, SalesDiscountType discountType, decimal? discountValue,
        decimal? taxRate, Guid? prescriptionRevisionId, EyeSide? prescriptionEye, bool requiresProduction,
        string? notes, TaxCalculationMode taxCalculationMode, byte currencyDecimalPlaces) =>
        new(id, customerOrderId, lineNumber, groupId, lineType, productVariantId, warehouseId, descriptionSnapshot,
            quantity, baseUnitPrice, actualUnitPrice, discountType, discountValue, taxRate, prescriptionRevisionId,
            prescriptionEye, requiresProduction, notes, taxCalculationMode, currencyDecimalPlaces);

    internal void UpdatePricing(
        decimal quantity, decimal baseUnitPrice, decimal actualUnitPrice, SalesDiscountType discountType,
        decimal? discountValue, decimal? taxRate, TaxCalculationMode taxCalculationMode, byte currencyDecimalPlaces)
    {
        if (baseUnitPrice < 0)
            throw new DomainException("Base unit price cannot be negative.");

        var amounts = SalesPricingCalculator.Calculate(quantity, actualUnitPrice, discountType, discountValue, taxRate, taxCalculationMode, currencyDecimalPlaces);
        Quantity = quantity;
        BaseUnitPrice = baseUnitPrice;
        ActualUnitPrice = actualUnitPrice;
        DiscountType = discountType;
        DiscountValue = discountType == SalesDiscountType.None ? null : discountValue;
        DiscountAmount = amounts.DiscountAmount;
        TaxRate = taxRate;
        TaxAmount = amounts.TaxAmount;
        NetAmount = amounts.NetAmount;
        FinalAmount = amounts.FinalAmount;
    }

    public void EnsurePrescriptionReference(bool prescriptionRequired)
    {
        if (prescriptionRequired && (!PrescriptionRevisionId.HasValue || !PrescriptionEye.HasValue))
            throw new DomainException("A prescription revision and eye are required for this sales line.");
    }

    public void EnsureOpticalMeasurementReference(bool opticalMeasurementRequired, bool hasOpticalSnapshot)
    {
        if (!opticalMeasurementRequired)
            return;

        var hasLegacyStoredPrescription = PrescriptionRevisionId.HasValue && PrescriptionEye.HasValue;
        if (!hasLegacyStoredPrescription && !hasOpticalSnapshot)
            throw new DomainException("An optical snapshot or stored prescription reference is required for this sales line.");
    }

    internal void SetActive(bool isActive) => IsActive = isActive;

    internal void SetLineNumber(int lineNumber)
    {
        if (lineNumber <= 0)
            throw new DomainException("Line number must be greater than zero.");
        LineNumber = lineNumber;
    }

    private void SetItemReferences(Guid? productVariantId, Guid? warehouseId, Guid? prescriptionRevisionId, EyeSide? prescriptionEye)
    {
        SalesDomainGuard.Defined(LineType, "Sales line type");

        if (LineType is SalesLineType.Frame or SalesLineType.Lens or SalesLineType.Accessory)
        {
            if (!productVariantId.HasValue || productVariantId.Value == Guid.Empty)
                throw new DomainException("Product variant is required for product sales lines.");
            // Warehouse is intentionally optional at the domain level because a Lens/Frame/Accessory
            // product can be configured as non-stock (for example a made-to-order lab lens).
            // ISalesLineResolver remains authoritative and requires a warehouse for stock items.
            if (warehouseId == Guid.Empty)
                throw new DomainException("Warehouse id cannot be empty when provided.");
        }

        if (LineType == SalesLineType.Service && (productVariantId.HasValue || warehouseId.HasValue))
            throw new DomainException("Service lines cannot reference a product variant or warehouse.");

        if (prescriptionRevisionId.HasValue != prescriptionEye.HasValue)
            throw new DomainException("Prescription revision and prescription eye must be provided together.");

        if (prescriptionRevisionId == Guid.Empty)
            throw new DomainException("Prescription revision id cannot be empty.");
        if (prescriptionEye.HasValue)
            SalesDomainGuard.Defined(prescriptionEye.Value, "Prescription eye");

        ProductVariantId = productVariantId;
        WarehouseId = warehouseId;
        PrescriptionRevisionId = prescriptionRevisionId;
        PrescriptionEye = prescriptionEye;
    }
}
