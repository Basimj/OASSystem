using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class LensDetailsConfiguration : IEntityTypeConfiguration<LensDetails>
{
    public void Configure(EntityTypeBuilder<LensDetails> builder)
    {
        builder.ToTable("tbl_LensDetails", "dbo", table =>
        {
            table.HasCheckConstraint("CK_LensDetails_ADD", "[ADD] IS NULL OR [ADD] >= 0");
            table.HasCheckConstraint("CK_LensDetails_BaseCurve", "[BaseCurve] IS NULL OR [BaseCurve] > 0");
            table.HasCheckConstraint("CK_LensDetails_Diameter", "[Diameter] IS NULL OR [Diameter] > 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_tbl_LensDetails");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProductVariantId).IsRequired();
        builder.Property(x => x.LensType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Material).HasMaxLength(100);
        builder.Property(x => x.Coating).HasMaxLength(100);
        builder.Property(x => x.RefractiveIndex).HasPrecision(5, 3);
        builder.Property(x => x.SPH).HasPrecision(6, 2);
        builder.Property(x => x.CYL).HasPrecision(6, 2);
        builder.Property(x => x.ADD).HasPrecision(6, 2);
        builder.Property(x => x.BaseCurve).HasPrecision(6, 2);
        builder.Property(x => x.Diameter).HasPrecision(6, 2);
        builder.Property(x => x.IsPrescriptionLens).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.LastModifiedBy).HasMaxLength(64);

        builder.HasIndex(x => x.ProductVariantId).IsUnique().HasDatabaseName("UX_LensDetails_ProductVariantId");
        builder.HasIndex(x => new { x.SPH, x.CYL, x.ADD }).HasDatabaseName("IX_LensDetails_SPH_CYL_ADD");

        builder.HasOne<ProductVariant>()
            .WithOne()
            .HasForeignKey<LensDetails>(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_LensDetails_ProductVariants_ProductVariantId");
    }
}
