using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence.Configurations;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class LensVariantDetailConfiguration : IEntityTypeConfiguration<LensVariantDetail>
{
    public void Configure(EntityTypeBuilder<LensVariantDetail> builder)
    {
        builder.ToTable("tbl_LensVariantDetails", "dbo", table =>
        {
            table.HasCheckConstraint("CK_LensVariantDetails_ADD", "[ADD] IS NULL OR [ADD] >= 0");
            table.HasCheckConstraint("CK_LensVariantDetails_BaseCurve", "[BaseCurve] IS NULL OR [BaseCurve] > 0");
            table.HasCheckConstraint("CK_LensVariantDetails_Diameter", "[Diameter] IS NULL OR [Diameter] > 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_tbl_LensVariantDetails");
        builder.ConfigureOasAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProductVariantId).IsRequired();
        builder.Property(x => x.SPH).HasPrecision(6, 2);
        builder.Property(x => x.CYL).HasPrecision(6, 2);
        builder.Property(x => x.ADD).HasPrecision(6, 2);
        builder.Property(x => x.BaseCurve).HasPrecision(6, 2);
        builder.Property(x => x.Diameter).HasPrecision(6, 2);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.ProductVariantId)
            .IsUnique()
            .HasDatabaseName("UX_LensVariantDetails_ProductVariantId");
        builder.HasIndex(x => new { x.SPH, x.CYL, x.ADD })
            .HasDatabaseName("IX_LensVariantDetails_SPH_CYL_ADD");

        builder.HasOne<ProductVariant>()
            .WithOne()
            .HasForeignKey<LensVariantDetail>(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LensVariantDetails_ProductVariants_ProductVariantId");
    }
}
