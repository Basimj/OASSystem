using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence.Configurations;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("tbl_StockReservations", "dbo", t =>
        {
            t.HasCheckConstraint("CK_StockReservations_Quantity_Positive", "[Quantity] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureOasAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProductVariantId).IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.Quantity).IsRequired().HasPrecision(18, 3);
        builder.Property(x => x.SourceModule).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SourceDocumentType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SourceDocumentId).IsRequired();
        builder.Property(x => x.SourceLineId).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>();
        builder.Property(x => x.ReservedAtUtc).IsRequired().HasColumnType("datetimeoffset");
        builder.Property(x => x.ReleasedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.ConsumedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.WarehouseId, x.ProductVariantId, x.Status })
            .HasDatabaseName("IX_StockReservations_Warehouse_Product_Status");
        builder.HasIndex(x => new { x.SourceModule, x.SourceDocumentType, x.SourceDocumentId })
            .HasDatabaseName("IX_StockReservations_Source");
        builder.HasIndex(x => x.SourceLineId)
            .HasDatabaseName("IX_StockReservations_SourceLineId");

        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StockReservations_ProductVariants_ProductVariantId");
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StockReservations_Warehouses_WarehouseId");
    }
}
