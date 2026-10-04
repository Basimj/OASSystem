using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class OpticalProductionMaterialConfiguration : IEntityTypeConfiguration<OpticalProductionMaterial>
{
    public void Configure(EntityTypeBuilder<OpticalProductionMaterial> builder)
    {
        builder.ToTable("tbl_OpticalProductionMaterials", "dbo", t =>
            t.HasCheckConstraint("CK_OpticalProductionMaterials_Qty", "[Quantity] > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Quantity).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.UnitCostSnapshot).HasPrecision(19, 4);
        builder.Property(x => x.TotalCostSnapshot).HasPrecision(19, 4);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.OpticalProductionJobId, x.ProductVariantId })
            .IsUnique().HasDatabaseName("UX_OpticalProductionMaterials_Job_Variant");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_OpticalProductionMaterials_ProductVariants_ProductVariantId");
    }
}
