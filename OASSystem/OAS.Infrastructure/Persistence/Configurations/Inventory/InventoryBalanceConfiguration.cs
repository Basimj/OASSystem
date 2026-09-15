using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("tbl_InventoryBalances");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WarehouseId)
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .IsRequired();

        builder.Property(x => x.OnHandQuantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.ReservedQuantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.OnOrderQuantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.AverageUnitCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.InventoryValue)
            .HasPrecision(18, 2);

        builder.Property(x => x.LastMovementAtUtc);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(64);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(64);

        builder.HasIndex(x => new
        {
            x.WarehouseId,
            x.ProductVariantId
        })
        .IsUnique()
        .HasDatabaseName("UX_InventoryBalances_Warehouse_ProductVariant");

        builder.HasIndex(x => x.ProductVariantId)
            .HasDatabaseName("IX_InventoryBalances_ProductVariantId");

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_InventoryBalances_Warehouses_WarehouseId");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_InventoryBalances_ProductVariants_ProductVariantId");
    }
}