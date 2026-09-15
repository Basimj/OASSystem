using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class FrameDetailsConfiguration : IEntityTypeConfiguration<FrameDetails>
{
    public void Configure(EntityTypeBuilder<FrameDetails> builder)
    {
        builder.ToTable("tbl_FrameDetails");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Material)
            .HasMaxLength(100);

        builder.Property(x => x.RimType)
            .HasMaxLength(50);

        builder.Property(x => x.Gender)
            .HasMaxLength(50);

        builder.Property(x => x.Shape)
            .HasMaxLength(50);

        builder.Property(x => x.TempleLength)
            .HasPrecision(6, 2);

        builder.Property(x => x.BridgeSize)
            .HasPrecision(6, 2);

        builder.Property(x => x.LensWidth)
            .HasPrecision(6, 2);

        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasDatabaseName("UX_FrameDetails_ProductId");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_FrameDetails_Products_ProductId");
    }
}