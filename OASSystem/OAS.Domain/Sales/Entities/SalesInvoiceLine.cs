using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class SalesInvoiceLine : AuditableEntity<Guid>
{
    private SalesInvoiceLine() { }

    private SalesInvoiceLine(
        Guid id, Guid salesInvoiceId, int lineNumber, Guid? customerOrderLineId, Guid? groupId,
        SalesLineType lineType, Guid? productVariantId, Guid? warehouseId, string? productCodeSnapshot,
        string productNameSnapshot, string descriptionSnapshot, string? unitSnapshot, decimal quantity,
        decimal baseUnitPrice, decimal actualUnitPrice, SalesDiscountType discountType, decimal? discountValue,
        decimal? taxRate, Guid? prescriptionRevisionId, EyeSide? prescriptionEye, bool requiresProduction,
        string? notes, TaxCalculationMode taxCalculationMode, byte currencyDecimalPlaces,
        decimal exchangeRate, byte baseCurrencyDecimalPlaces)
    {
        Id = SalesDomainGuard.Required(id, "Sales invoice line id");
        SalesInvoiceId = SalesDomainGuard.Required(salesInvoiceId, "Sales invoice id");
        if (lineNumber <= 0)
            throw new DomainException("Line number must be greater than zero.");
        if (customerOrderLineId == Guid.Empty)
            throw new DomainException("Customer order line id cannot be empty.");

        LineNumber = lineNumber;
        CustomerOrderLineId = customerOrderLineId;
        GroupId = groupId;
        LineType = lineType;
        SetItemReferences(productVariantId, warehouseId, prescriptionRevisionId, prescriptionEye);
        ProductCodeSnapshot = SalesDomainGuard.Optional(productCodeSnapshot, 100, "Product code snapshot");
        ProductNameSnapshot = SalesDomainGuard.Required(productNameSnapshot, 250, "Product name snapshot");
        DescriptionSnapshot = SalesDomainGuard.Required(descriptionSnapshot, 500, "Description snapshot");
        UnitSnapshot = SalesDomainGuard.Optional(unitSnapshot, 100, "Unit snapshot");
        RequiresProduction = requiresProduction;
        Notes = SalesDomainGuard.Optional(notes, 1000, "Line notes");
        IsActive = true;
        UpdatePricing(quantity, baseUnitPrice, actualUnitPrice, discountType, discountValue, taxRate,
            taxCalculationMode, currencyDecimalPlaces, exchangeRate, baseCurrencyDecimalPlaces);
    }

    public Guid SalesInvoiceId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid? CustomerOrderLineId { get; private set; }
    public Guid? GroupId { get; private set; }
    public SalesLineType LineType { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public string? ProductCodeSnapshot { get; private set; }
    public string ProductNameSnapshot { get; private set; } = string.Empty;
    public string DescriptionSnapshot { get; private set; } = string.Empty;
    public string? UnitSnapshot { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal ReturnedQuantity { get; private set; }
    public decimal RemainingReturnableQuantity => Quantity - ReturnedQuantity;
    public decimal BaseUnitPrice { get; private set; }
    public decimal ActualUnitPrice { get; private set; }
    public SalesDiscountType DiscountType { get; private set; }
    public decimal? DiscountValue { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal? TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal FinalAmount { get; private set; }
    public decimal BaseNetAmount { get; private set; }
    public decimal BaseTaxAmount { get; private set; }
    public decimal BaseFinalAmount { get; private set; }
    public decimal? UnitCostSnapshot { get; private set; }
    public decimal? TotalCostSnapshot { get; private set; }
    public Guid? PrescriptionRevisionId { get; private set; }
    public EyeSide? PrescriptionEye { get; private set; }
    public bool RequiresProduction { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public SalesInvoiceLinePrescriptionSnapshot? PrescriptionSnapshot { get; private set; }

    public decimal GrossAmount => Quantity * ActualUnitPrice;
    public bool RequiresInventory => LineType != SalesLineType.Service && ProductVariantId.HasValue && WarehouseId.HasValue;

    public static SalesInvoiceLine Create(
        Guid id, Guid salesInvoiceId, int lineNumber, Guid? customerOrderLineId, Guid? groupId,
        SalesLineType lineType, Guid? productVariantId, Guid? warehouseId, string? productCodeSnapshot,
        string productNameSnapshot, string descriptionSnapshot, string? unitSnapshot, decimal quantity,
        decimal baseUnitPrice, decimal actualUnitPrice, SalesDiscountType discountType, decimal? discountValue,
        decimal? taxRate, Guid? prescriptionRevisionId, EyeSide? prescriptionEye, bool requiresProduction,
        string? notes, TaxCalculationMode taxCalculationMode, byte currencyDecimalPlaces,
        decimal exchangeRate, byte baseCurrencyDecimalPlaces) =>
        new(id, salesInvoiceId, lineNumber, customerOrderLineId, groupId, lineType, productVariantId, warehouseId,
            productCodeSnapshot, productNameSnapshot, descriptionSnapshot, unitSnapshot, quantity, baseUnitPrice,
            actualUnitPrice, discountType, discountValue, taxRate, prescriptionRevisionId, prescriptionEye,
            requiresProduction, notes, taxCalculationMode, currencyDecimalPlaces, exchangeRate, baseCurrencyDecimalPlaces);

    internal void UpdatePricing(
        decimal quantity, decimal baseUnitPrice, decimal actualUnitPrice, SalesDiscountType discountType,
        decimal? discountValue, decimal? taxRate, TaxCalculationMode taxCalculationMode, byte currencyDecimalPlaces,
        decimal exchangeRate, byte baseCurrencyDecimalPlaces)
    {
        if (baseUnitPrice < 0)
            throw new DomainException("Base unit price cannot be negative.");

        var amounts = SalesPricingCalculator.Calculate(quantity, actualUnitPrice, discountType, discountValue, taxRate,
            taxCalculationMode, currencyDecimalPlaces);

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
        BaseNetAmount = SalesPricingCalculator.ConvertToBase(NetAmount, exchangeRate, baseCurrencyDecimalPlaces);
        BaseTaxAmount = SalesPricingCalculator.ConvertToBase(TaxAmount, exchangeRate, baseCurrencyDecimalPlaces);
        BaseFinalAmount = SalesPricingCalculator.ConvertToBase(FinalAmount, exchangeRate, baseCurrencyDecimalPlaces);
    }

    internal void SetCostSnapshot(decimal unitCost, decimal totalCost)
    {
        if (!RequiresInventory)
            throw new DomainException("Service or non-inventory lines cannot receive inventory cost snapshots.");
        if (unitCost < 0 || totalCost < 0)
            throw new DomainException("Inventory cost snapshot cannot be negative.");

        UnitCostSnapshot = unitCost;
        TotalCostSnapshot = totalCost;
    }

    internal void SetPrescriptionSnapshot(SalesInvoiceLinePrescriptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (LineType != SalesLineType.Lens)
            throw new DomainException("Prescription snapshots can only be attached to lens lines.");
        if (!PrescriptionRevisionId.HasValue || !PrescriptionEye.HasValue)
            throw new DomainException("The invoice line must reference a prescription revision and eye first.");
        if (snapshot.SalesInvoiceLineId != Id || snapshot.PrescriptionRevisionId != PrescriptionRevisionId.Value || snapshot.Eye != PrescriptionEye.Value)
            throw new DomainException("Prescription snapshot does not match the sales invoice line.");

        PrescriptionSnapshot = snapshot;
    }

    public void ReserveReturnQuantity(decimal quantity)
    {
        if (quantity <= 0m) throw new DomainException("Return quantity must be greater than zero.");
        if (ReturnedQuantity + quantity > Quantity)
            throw new DomainException("Return quantity exceeds the remaining returnable quantity.");
        ReturnedQuantity += quantity;
    }

    public void ReleaseReturnQuantity(decimal quantity)
    {
        if (quantity <= 0m) throw new DomainException("Return quantity must be greater than zero.");
        if (quantity > ReturnedQuantity)
            throw new DomainException("Cannot release more returned quantity than currently reserved.");
        ReturnedQuantity -= quantity;
    }

    public void EnsurePrescriptionReference(bool prescriptionRequired)
    {
        if (prescriptionRequired && (!PrescriptionRevisionId.HasValue || !PrescriptionEye.HasValue))
            throw new DomainException("A prescription revision and eye are required for this sales line.");
    }

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
                throw new DomainException("Product variant is required for inventory sales lines.");
            if (!warehouseId.HasValue || warehouseId.Value == Guid.Empty)
                throw new DomainException("Warehouse is required for inventory sales lines.");
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
