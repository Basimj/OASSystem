using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class LensDetailsConfiguration : IEntityTypeConfiguration<LensDetails>
{
    public void Configure(EntityTypeBuilder<LensDetails> builder)
    {
        builder.ToTable("tbl_LensDetails");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.LensType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Material)
            .HasMaxLength(100);

        builder.Property(x => x.Coating)
            .HasMaxLength(100);

        builder.Property(x => x.RefractiveIndex)
            .HasPrecision(5, 3);

        builder.Property(x => x.SphereMin)
            .HasPrecision(6, 2);

        builder.Property(x => x.SphereMax)
            .HasPrecision(6, 2);

        builder.Property(x => x.CylinderMin)
            .HasPrecision(6, 2);

        builder.Property(x => x.CylinderMax)
            .HasPrecision(6, 2);

        builder.Property(x => x.AddMin)
            .HasPrecision(6, 2);

        builder.Property(x => x.AddMax)
            .HasPrecision(6, 2);

        builder.Property(x => x.IsPrescriptionLens)
            .IsRequired();

        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasDatabaseName("UX_LensDetails_ProductId");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_LensDetails_Products_ProductId");
    }
}