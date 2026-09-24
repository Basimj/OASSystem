using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("tbl_Products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductCode)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.NameEn)
            .HasMaxLength(200);

        builder.Property(x => x.CategoryId)
            .IsRequired();

        builder.Property(x => x.BrandId);

        builder.Property(x => x.ProductTypeId)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.IsStockItem)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(64);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(64);

        builder.HasIndex(x => x.ProductCode)
            .IsUnique()
            .HasDatabaseName("UX_Products_ProductCode");

        builder.HasIndex(x => x.CategoryId)
            .HasDatabaseName("IX_Products_CategoryId");

        builder.HasIndex(x => x.BrandId)
            .HasDatabaseName("IX_Products_BrandId");

        builder.HasIndex(x => x.ProductTypeId)
            .HasDatabaseName("IX_Products_ProductTypeId");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_Products_IsActive");

        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Products_ProductCategories_CategoryId");

        builder.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Products_Brands_BrandId");

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(x => x.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Products_ProductTypes_ProductTypeId");
    }

}