using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class InventoryTransactionConfiguration
    : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("tbl_InventoryTransactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TransactionNumber)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.TransactionType)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.SourceWarehouseId);

        builder.Property(x => x.DestinationWarehouseId);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.TransactionDate)
            .IsRequired();

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(64);

        builder.Property(x => x.ReferenceId);

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.PostedBy)
            .HasMaxLength(64);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(64);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(64);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.TransactionNumber)
            .IsUnique()
            .HasDatabaseName("UX_InventoryTransactions_TransactionNumber");

        builder.HasIndex(x => x.TransactionDate)
            .HasDatabaseName("IX_InventoryTransactions_TransactionDate");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_InventoryTransactions_Status");

        builder.HasIndex(x => x.SourceWarehouseId)
            .HasDatabaseName("IX_InventoryTransactions_SourceWarehouseId");

        builder.HasIndex(x => x.DestinationWarehouseId)
            .HasDatabaseName("IX_InventoryTransactions_DestinationWarehouseId");

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => x.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_InventoryTransactions_SourceWarehouse");

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => x.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_InventoryTransactions_DestinationWarehouse");
    }
}