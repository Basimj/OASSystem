using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseReceiptLineConfiguration : IEntityTypeConfiguration<PurchaseReceiptLine>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptLine> builder)
    {
        builder.ToTable("tbl_PurchaseReceiptLines", "dbo", t => { t.HasCheckConstraint("CK_PurchaseReceiptLines_ReceivedQuantity_Positive", "[ReceivedQuantity] > 0"); t.HasCheckConstraint("CK_PurchaseReceiptLines_Quantities_NonNegative", "[AcceptedQuantity] >= 0 AND [RejectedQuantity] >= 0 AND [BaseAcceptedQuantity] >= 0"); t.HasCheckConstraint("CK_PurchaseReceiptLines_ReturnedQuantity_Valid", "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [AcceptedQuantity]"); t.HasCheckConstraint("CK_PurchaseReceiptLines_ActualUnitCost_NonNegative", "[ActualUnitCost] >= 0"); }); builder.HasKey(x => x.Id); builder.ConfigurePurchasingAudit(); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property(x => x.PurchaseReceiptId).IsRequired(); builder.Property(x => x.PurchaseOrderLineId).IsRequired(); builder.Property(x => x.LineSequence).IsRequired(); builder.Property(x => x.ProductVariantId).IsRequired(); builder.Property(x => x.OrderedQuantitySnapshot).HasPrecision(18,3).IsRequired(); builder.Property(x => x.PreviouslyReceivedQty).HasPrecision(18,3).IsRequired(); builder.Property(x => x.ReceivedQuantity).HasPrecision(18,3).IsRequired(); builder.Property(x => x.AcceptedQuantity).HasPrecision(18,3).IsRequired(); builder.Property(x => x.RejectedQuantity).HasPrecision(18,3).IsRequired(); builder.Property(x => x.ReturnedQuantity).HasPrecision(18,3).IsRequired().HasDefaultValue(0m); builder.Property(x => x.BaseAcceptedQuantity).HasPrecision(18,3).IsRequired(); builder.Property(x => x.ActualUnitCost).HasPrecision(19,4).IsRequired(); builder.Property(x => x.TotalAcceptedCost).HasPrecision(19,4).IsRequired(); builder.Property(x => x.ExpiryDate).HasColumnType("date"); builder.Property(x => x.BatchCode).HasMaxLength(100); builder.Property(x => x.Notes).HasMaxLength(500); builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken(); builder.HasIndex(x => new { x.PurchaseReceiptId, x.LineSequence }).IsUnique().HasDatabaseName("UX_PurchaseReceiptLines_Receipt_LineSequence"); builder.HasIndex(x => x.PurchaseOrderLineId).HasDatabaseName("IX_PurchaseReceiptLines_PurchaseOrderLineId"); builder.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReceiptLines_OrderLines_PurchaseOrderLineId"); builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReceiptLines_ProductVariants_ProductVariantId");
    }
}
