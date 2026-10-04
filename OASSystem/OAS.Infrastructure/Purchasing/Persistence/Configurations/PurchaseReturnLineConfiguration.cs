using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseReturnLineConfiguration : IEntityTypeConfiguration<PurchaseReturnLine>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnLine> builder)
    {
        builder.ToTable("tbl_PurchaseReturnLines", "dbo", t =>
        {
            t.HasCheckConstraint("CK_PurchaseReturnLines_Quantity_Positive", "[Quantity] > 0 AND [BaseQuantity] > 0");
            t.HasCheckConstraint("CK_PurchaseReturnLines_Amounts_NonNegative", "[ReceiptUnitCostBase] >= 0 AND [ReceiptCostBaseAmount] >= 0 AND [SupplierNetBaseAmount] >= 0 AND [SupplierTaxBaseAmount] >= 0 AND [SupplierGrossBaseAmount] >= 0 AND ([InventoryUnitCostBase] IS NULL OR [InventoryUnitCostBase] >= 0) AND ([InventoryCostBaseAmount] IS NULL OR [InventoryCostBaseAmount] >= 0)");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigurePurchasingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PurchaseReturnId).IsRequired();
        builder.Property(x => x.LineNumber).IsRequired();
        builder.Property(x => x.PurchaseReceiptLineId).IsRequired();
        builder.Property(x => x.PurchaseInvoiceLineId);
        builder.Property(x => x.ProductVariantId).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18,3).IsRequired();
        builder.Property(x => x.BaseQuantity).HasPrecision(18,3).IsRequired();
        builder.Property(x => x.ReceiptUnitCostBase).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.ReceiptCostBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.SupplierNetBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.SupplierTaxBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.SupplierGrossBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.InventoryUnitCostBase).HasPrecision(19,4);
        builder.Property(x => x.InventoryCostBaseAmount).HasPrecision(19,4);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.PurchaseReturnId, x.LineNumber }).IsUnique().HasDatabaseName("UX_PurchaseReturnLines_Return_LineNumber");
        builder.HasIndex(x => x.PurchaseReceiptLineId).HasDatabaseName("IX_PurchaseReturnLines_ReceiptLineId");
        builder.HasIndex(x => x.PurchaseInvoiceLineId).HasDatabaseName("IX_PurchaseReturnLines_InvoiceLineId");
        builder.HasOne<PurchaseReceiptLine>().WithMany().HasForeignKey(x => x.PurchaseReceiptLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturnLines_ReceiptLines_PurchaseReceiptLineId");
        builder.HasOne<PurchaseInvoiceLine>().WithMany().HasForeignKey(x => x.PurchaseInvoiceLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturnLines_InvoiceLines_PurchaseInvoiceLineId");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturnLines_ProductVariants_ProductVariantId");
    }
}
