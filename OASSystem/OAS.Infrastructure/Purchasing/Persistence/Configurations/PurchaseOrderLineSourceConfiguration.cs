using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseOrderLineSourceConfiguration : IEntityTypeConfiguration<PurchaseOrderLineSource>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLineSource> builder)
    {
        builder.ToTable("tbl_PurchaseOrderLineSources", "dbo", t => t.HasCheckConstraint("CK_PurchaseOrderLineSources_AllocatedQuantity_Positive", "[AllocatedQuantity] > 0")); builder.HasKey(x => x.Id); builder.ConfigurePurchasingAudit(); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property(x => x.PurchaseOrderLineId).IsRequired(); builder.Property(x => x.PurchaseRequestLineId).IsRequired(); builder.Property(x => x.AllocatedQuantity).HasPrecision(18,3).IsRequired(); builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken(); builder.HasIndex(x => new { x.PurchaseOrderLineId, x.PurchaseRequestLineId }).IsUnique().HasDatabaseName("UX_PurchaseOrderLineSources_OrderLine_RequestLine"); builder.HasIndex(x => x.PurchaseRequestLineId).HasDatabaseName("IX_PurchaseOrderLineSources_RequestLineId"); builder.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseOrderLineSources_OrderLines_PurchaseOrderLineId"); builder.HasOne<PurchaseRequestLine>().WithMany().HasForeignKey(x => x.PurchaseRequestLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseOrderLineSources_RequestLines_PurchaseRequestLineId");
    }
}
