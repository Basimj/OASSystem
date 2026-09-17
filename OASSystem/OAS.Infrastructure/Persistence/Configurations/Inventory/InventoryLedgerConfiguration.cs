using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;

public sealed class InventoryLedgerConfiguration
: IEntityTypeConfiguration<InventoryLedger>
{
    public void Configure(EntityTypeBuilder<InventoryLedger> builder)
    {
        builder.ToTable("tbl_InventoryLedger");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SequenceNumber)
            .IsRequired();

        builder.Property(x => x.TransactionId)
            .IsRequired();

        builder.Property(x => x.TransactionLineId)
            .IsRequired();

        builder.Property(x => x.WarehouseId)
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .IsRequired();

        builder.Property(x => x.MovementType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.QuantityIn)
            .HasPrecision(18, 3);

        builder.Property(x => x.QuantityOut)
            .HasPrecision(18, 3);

        builder.Property(x => x.BalanceAfter)
            .HasPrecision(18, 3);

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.AverageCostAfter)
            .HasPrecision(18, 2);

        builder.Property(x => x.InventoryValueAfter)
            .HasPrecision(18, 2);

        builder.Property(x => x.MovementDate)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(64);

        builder.HasIndex(x => x.SequenceNumber)
            .IsUnique()
            .HasDatabaseName("UX_InventoryLedger_SequenceNumber");

        builder.HasIndex(x => new
        {
            x.WarehouseId,
            x.ProductVariantId,
            x.MovementDate
        })
        .HasDatabaseName("IX_InventoryLedger_Warehouse_ProductVariant_Date");

        builder.HasIndex(x => x.TransactionId)
            .HasDatabaseName("IX_InventoryLedger_TransactionId");

        builder.HasIndex(x => x.TransactionLineId)
            .HasDatabaseName("IX_InventoryLedger_TransactionLineId");

        builder.HasIndex(x => x.ProductVariantId)
            .HasDatabaseName("IX_InventoryLedger_ProductVariantId");

        builder.HasOne<InventoryTransaction>()
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_InventoryLedger_Transactions_TransactionId");

        builder.HasOne<InventoryTransactionLine>()
            .WithMany()
            .HasForeignKey(x => x.TransactionLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_InventoryLedger_TransactionLines_TransactionLineId");

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_InventoryLedger_Warehouses_WarehouseId");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_InventoryLedger_ProductVariants_ProductVariantId");
    }

}