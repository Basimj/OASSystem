using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("tbl_PurchaseOrderLines", "dbo", t => { t.HasCheckConstraint("CK_PurchaseOrderLines_Quantities_Positive", "[OrderedQuantity] > 0 AND [BaseQuantity] > 0 AND [UnitConversionFactor] > 0"); t.HasCheckConstraint("CK_PurchaseOrderLines_Amounts_NonNegative", "[UnitPrice] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] >= 0 AND [TaxAmount] >= 0 AND [FinalAmount] >= 0"); });
        builder.HasKey(x => x.Id); builder.ConfigurePurchasingAudit(); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property(x => x.PurchaseOrderId).IsRequired(); builder.Property(x => x.LineSequence).IsRequired(); builder.Property(x => x.ProductVariantId).IsRequired(); builder.Property(x => x.SupplierCatalogItemId); builder.Property(x => x.PurchaseUnitId).IsRequired(); builder.Property(x => x.UnitConversionFactor).HasPrecision(18,6).IsRequired(); builder.Property(x => x.ProductCodeSnapshot).HasMaxLength(64); builder.Property(x => x.ProductNameSnapshot).IsRequired().HasMaxLength(200); builder.Property(x => x.UnitNameSnapshot).HasMaxLength(100); builder.Property(x => x.OrderedQuantity).HasPrecision(18,3).IsRequired(); builder.Property(x => x.BaseQuantity).HasPrecision(18,3).IsRequired(); builder.Property(x => x.UnitPrice).HasPrecision(19,4).IsRequired(); builder.Property(x => x.DiscountAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.NetAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.TaxRate).HasPrecision(9,6).IsRequired(); builder.Property(x => x.TaxAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.FinalAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.ExpectedDeliveryDate).HasColumnType("date"); builder.Property(x => x.Notes).HasMaxLength(500); builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.PurchaseOrderId, x.LineSequence }).IsUnique().HasDatabaseName("UX_PurchaseOrderLines_Order_LineSequence"); builder.HasIndex(x => x.ProductVariantId).HasDatabaseName("IX_PurchaseOrderLines_ProductVariantId");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseOrderLines_ProductVariants_ProductVariantId"); builder.HasOne<SupplierCatalogItem>().WithMany().HasForeignKey(x => x.SupplierCatalogItemId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseOrderLines_SupplierCatalogItems_SupplierCatalogItemId"); builder.HasOne<Unit>().WithMany().HasForeignKey(x => x.PurchaseUnitId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseOrderLines_Units_PurchaseUnitId");
    }
}
