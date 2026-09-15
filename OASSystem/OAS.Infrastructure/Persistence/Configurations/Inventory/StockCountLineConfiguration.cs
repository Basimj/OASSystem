using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class StockCountLineConfiguration
    : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> builder)
    {
        builder.ToTable("tbl_StockCountLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.StockCountId)
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .IsRequired();

        builder.Property(x => x.SystemQuantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.CountedQuantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.DifferenceQuantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.AverageCostSnapshot)
            .HasPrecision(18, 2);

        builder.Property(x => x.VarianceValue)
            .HasPrecision(18, 2);

        builder.Property(x => x.CountedBy)
            .HasMaxLength(64);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => new
        {
            x.StockCountId,
            x.ProductVariantId
        })
        .IsUnique()
        .HasDatabaseName("UX_StockCountLines_StockCount_ProductVariant");

        builder.HasIndex(x => x.ProductVariantId)
            .HasDatabaseName("IX_StockCountLines_ProductVariantId");

        builder.HasOne<StockCount>()
            .WithMany()
            .HasForeignKey(x => x.StockCountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_StockCountLines_StockCounts_StockCountId");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_StockCountLines_ProductVariants_ProductVariantId");
    }
}