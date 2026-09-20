using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("tbl_ProductVariants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.SKU)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Barcode)
            .HasMaxLength(64);

        builder.Property(x => x.VariantName)
            .HasMaxLength(100);

        builder.Property(x => x.Color)
            .HasMaxLength(50);

        builder.Property(x => x.Size)
            .HasMaxLength(50);

        builder.Property(x => x.UnitId);

        builder.Property(x => x.PurchasePrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.SellingPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(64);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(64);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.SKU)
            .IsUnique()
            .HasDatabaseName("UX_ProductVariants_SKU");

        builder.HasIndex(x => x.Barcode)
            .IsUnique()
            .HasDatabaseName("UX_ProductVariants_Barcode")
            .HasFilter("[Barcode] IS NOT NULL");

        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("IX_ProductVariants_ProductId");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_ProductVariants_IsActive");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ProductVariants_Products_ProductId");

        builder.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ProductVariants_Units_UnitId");
    }
}