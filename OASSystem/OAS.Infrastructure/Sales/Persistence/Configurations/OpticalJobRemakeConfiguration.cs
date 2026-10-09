using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class OpticalJobRemakeConfiguration : IEntityTypeConfiguration<OpticalJobRemake>
{
    public void Configure(EntityTypeBuilder<OpticalJobRemake> builder)
    {
        builder.ToTable("tbl_OpticalJobRemakes", "dbo", t => t.HasCheckConstraint("CK_tbl_OpticalJobRemakes_Quantity_GT_0", "[Quantity] > 0"));
        builder.HasKey(x => x.Id).HasName("PK_tbl_OpticalJobRemakes");
        builder.ConfigureSalesAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OpticalJobId).IsRequired();
        builder.Property(x => x.SourceQualityCheckId);
        builder.Property(x => x.SourceBreakageId);
        builder.Property(x => x.OpticalJobLineId).IsRequired();
        builder.Property(x => x.ProductVariantId);
        builder.Property(x => x.Quantity).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ReplacementPurchaseRequestLineId);
        builder.Property(x => x.StartedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.CompletedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.OpticalJobId).HasDatabaseName("IX_tbl_OpticalJobRemakes_OpticalJobId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_tbl_OpticalJobRemakes_Status");
        builder.HasOne<OpticalJob>().WithMany().HasForeignKey(x => x.OpticalJobId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobRemakes_OpticalJobs");
        builder.HasOne<OpticalQualityCheck>().WithMany().HasForeignKey(x => x.SourceQualityCheckId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobRemakes_QualityChecks");
        builder.HasOne<OpticalJobBreakage>().WithMany().HasForeignKey(x => x.SourceBreakageId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobRemakes_Breakages");
        builder.HasOne<OpticalJobLine>().WithMany().HasForeignKey(x => x.OpticalJobLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobRemakes_OpticalJobLines");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobRemakes_ProductVariants");
        builder.HasOne<PurchaseRequestLine>().WithMany().HasForeignKey(x => x.ReplacementPurchaseRequestLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobRemakes_PurchaseRequestLines");
    }
}
