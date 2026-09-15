using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("tbl_StockCounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.CountNumber)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.WarehouseId)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.CountDate)
            .IsRequired();

        builder.Property(x => x.ApprovedBy)
            .HasMaxLength(64);

        builder.Property(x => x.PostedBy)
            .HasMaxLength(64);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(64);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(64);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.CountNumber)
            .IsUnique()
            .HasDatabaseName("UX_StockCounts_CountNumber");

        builder.HasIndex(x => x.WarehouseId)
            .HasDatabaseName("IX_StockCounts_WarehouseId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_StockCounts_Status");

        builder.HasIndex(x => x.CountDate)
            .HasDatabaseName("IX_StockCounts_CountDate");

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_StockCounts_Warehouses_WarehouseId");
    }
}