using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable("tbl_productTypes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.NameAr)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.NameEn)
            .HasMaxLength(100);

        builder.Property(x => x.SystemKey)
            .HasMaxLength(32);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(64);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(64);

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_productTypes_Code");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_productTypes_IsActive");

        builder.HasIndex(x => x.SystemKey)
            .IsUnique()
            .HasFilter("[SystemKey] IS NOT NULL")
            .HasDatabaseName("UX_productTypes_SystemKey");
    }
}
