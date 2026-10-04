using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseRequestLineConfiguration : IEntityTypeConfiguration<PurchaseRequestLine>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestLine> builder)
    {
        builder.ToTable("tbl_PurchaseRequestLines", "dbo", t => { t.HasCheckConstraint("CK_PurchaseRequestLines_RequestedQuantity_Positive", "[RequestedQuantity] > 0"); t.HasCheckConstraint("CK_PurchaseRequestLines_LineSequence_Positive", "[LineSequence] > 0"); });
        builder.HasKey(x => x.Id); builder.ConfigurePurchasingAudit(); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PurchaseRequestId).IsRequired(); builder.Property(x => x.LineSequence).IsRequired(); builder.Property(x => x.ProductVariantId).IsRequired(); builder.Property(x => x.RequestedQuantity).HasPrecision(18,3).IsRequired();
        builder.Property(x => x.RequiredDate).HasColumnType("date"); builder.Property(x => x.CustomerOrderLineId); builder.Property(x => x.ScheduledOrderAtUtc).HasColumnType("datetimeoffset"); builder.Property(x => x.PreferredSupplierId); builder.Property(x => x.Notes).HasMaxLength(500); builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.PurchaseRequestId, x.LineSequence }).IsUnique().HasDatabaseName("UX_PurchaseRequestLines_Request_LineSequence"); builder.HasIndex(x => x.ProductVariantId).HasDatabaseName("IX_PurchaseRequestLines_ProductVariantId"); builder.HasIndex(x => x.ScheduledOrderAtUtc).HasDatabaseName("IX_PurchaseRequestLines_ScheduledOrderAtUtc"); builder.HasIndex(x => new { x.CustomerOrderLineId, x.ProductVariantId }).HasDatabaseName("IX_PurchaseRequestLines_CustomerOrderLine_ProductVariant");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseRequestLines_ProductVariants_ProductVariantId");
        builder.HasOne<CustomerOrderLine>().WithMany().HasForeignKey(x => x.CustomerOrderLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseRequestLines_CustomerOrderLines_CustomerOrderLineId");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.PreferredSupplierId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseRequestLines_Suppliers_PreferredSupplierId");
    }
}
