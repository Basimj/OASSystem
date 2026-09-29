using OAS.Domain.Common.Entities;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class SupplierCatalogItem : AuditableEntity<Guid>
{
    private SupplierCatalogItem() { }
    private SupplierCatalogItem(Guid id, Guid supplierId, Guid productVariantId, string? supplierProductCode, string? supplierProductName,
        Guid purchaseUnitId, decimal unitConversionFactor, int? leadTimeDays, decimal minimumOrderQuantity, bool isPreferred, bool isActive)
    {
        Id = PurchasingDomainGuard.Required(id, "Supplier catalog item id");
        SupplierId = PurchasingDomainGuard.Required(supplierId, "Supplier id");
        ProductVariantId = PurchasingDomainGuard.Required(productVariantId, "Product variant id");
        Update(supplierProductCode, supplierProductName, purchaseUnitId, unitConversionFactor, leadTimeDays, minimumOrderQuantity, isPreferred, isActive);
    }

    public Guid SupplierId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string? SupplierProductCode { get; private set; }
    public string? SupplierProductName { get; private set; }
    public Guid PurchaseUnitId { get; private set; }
    public decimal UnitConversionFactor { get; private set; }
    public int? LeadTimeDays { get; private set; }
    public decimal MinimumOrderQuantity { get; private set; }
    public bool IsPreferred { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static SupplierCatalogItem Create(Guid id, Guid supplierId, Guid productVariantId, string? supplierProductCode, string? supplierProductName,
        Guid purchaseUnitId, decimal unitConversionFactor, int? leadTimeDays, decimal minimumOrderQuantity, bool isPreferred, bool isActive = true) =>
        new(id, supplierId, productVariantId, supplierProductCode, supplierProductName, purchaseUnitId, unitConversionFactor, leadTimeDays, minimumOrderQuantity, isPreferred, isActive);

    public void Update(string? supplierProductCode, string? supplierProductName, Guid purchaseUnitId, decimal unitConversionFactor,
        int? leadTimeDays, decimal minimumOrderQuantity, bool isPreferred, bool isActive)
    {
        PurchaseUnitId = PurchasingDomainGuard.Required(purchaseUnitId, "Purchase unit id");
        PurchasingDomainGuard.Positive(unitConversionFactor, "Unit conversion factor");
        PurchasingDomainGuard.NonNegative(minimumOrderQuantity, "Minimum order quantity");
        if (leadTimeDays < 0) throw new OAS.Domain.Exceptions.DomainException("Lead time days cannot be negative.");
        SupplierProductCode = PurchasingDomainGuard.Optional(supplierProductCode, 64, "Supplier product code");
        SupplierProductName = PurchasingDomainGuard.Optional(supplierProductName, 200, "Supplier product name");
        UnitConversionFactor = unitConversionFactor;
        LeadTimeDays = leadTimeDays;
        MinimumOrderQuantity = minimumOrderQuantity;
        IsPreferred = isPreferred;
        IsActive = isActive;
    }
}
