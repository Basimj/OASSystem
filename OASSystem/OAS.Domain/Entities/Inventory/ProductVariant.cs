using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class ProductVariant : AuditableEntity<Guid>
{
    public Guid ProductId { get; private set; }

    public string SKU { get; private set; } = null!;
    public string? Barcode { get; private set; }
    public string? VariantName { get; private set; }
    public string? Color { get; private set; }
    public string? Size { get; private set; }

    public Guid? UnitId { get; private set; }

    public decimal PurchasePrice { get; private set; }
    public decimal SellingPrice { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private ProductVariant()
    {
    }

    public ProductVariant(
        Guid productId,
        string sku,
        decimal purchasePrice,
        decimal sellingPrice,
        string? barcode = null,
        string? variantName = null,
        string? color = null,
        string? size = null,
        Guid? unitId = null)
    {
        Id = Guid.NewGuid();

        ProductId = productId;
        SKU = sku;
        Barcode = barcode;
        VariantName = variantName;
        Color = color;
        Size = size;
        UnitId = unitId;
        PurchasePrice = purchasePrice;
        SellingPrice = sellingPrice;
        IsActive = true;
    }
}