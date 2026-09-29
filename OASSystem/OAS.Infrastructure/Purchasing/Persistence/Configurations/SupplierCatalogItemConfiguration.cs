using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class SupplierCatalogItemConfiguration : IEntityTypeConfiguration<SupplierCatalogItem>
{
    public void Configure(EntityTypeBuilder<SupplierCatalogItem> builder)
    {
        builder.ToTable("tbl_SupplierCatalogItems", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SupplierCatalogItems_UnitConversionFactor_Positive", "[UnitConversionFactor] > 0");
            t.HasCheckConstraint("CK_SupplierCatalogItems_MinimumOrderQuantity_NonNegative", "[MinimumOrderQuantity] >= 0");
            t.HasCheckConstraint("CK_SupplierCatalogItems_LeadTimeDays_NonNegative", "[LeadTimeDays] IS NULL OR [LeadTimeDays] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigurePurchasingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SupplierId).IsRequired();
        builder.Property(x => x.ProductVariantId).IsRequired();
        builder.Property(x => x.SupplierProductCode).HasMaxLength(64);
        builder.Property(x => x.SupplierProductName).HasMaxLength(200);
        builder.Property(x => x.PurchaseUnitId).IsRequired();
        builder.Property(x => x.UnitConversionFactor).HasPrecision(18,6).IsRequired();
        builder.Property(x => x.LeadTimeDays);
        builder.Property(x => x.MinimumOrderQuantity).HasPrecision(18,3).IsRequired();
        builder.Property(x => x.IsPreferred).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.SupplierId, x.ProductVariantId, x.PurchaseUnitId }).IsUnique().HasDatabaseName("UX_SupplierCatalogItems_Supplier_ProductVariant_PurchaseUnit");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("IX_SupplierCatalogItems_SupplierId");
        builder.HasIndex(x => x.ProductVariantId).HasDatabaseName("IX_SupplierCatalogItems_ProductVariantId");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_SupplierCatalogItems_IsActive");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SupplierCatalogItems_Suppliers_SupplierId");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SupplierCatalogItems_ProductVariants_ProductVariantId");
        builder.HasOne<Unit>().WithMany().HasForeignKey(x => x.PurchaseUnitId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SupplierCatalogItems_Units_PurchaseUnitId");
    }
}
