using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseReceiptConfiguration : IEntityTypeConfiguration<PurchaseReceipt>
{
    public void Configure(EntityTypeBuilder<PurchaseReceipt> builder)
    {
        builder.ToTable("tbl_PurchaseReceipts", "dbo"); builder.HasKey(x => x.Id); builder.ConfigurePurchasingAudit(); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property(x => x.ReceiptCode).IsRequired().HasMaxLength(40); builder.Property(x => x.PurchaseOrderId).IsRequired(); builder.Property(x => x.SupplierId).IsRequired(); builder.Property(x => x.WarehouseId).IsRequired(); builder.Property(x => x.ReceiptDate).HasColumnType("date").IsRequired(); builder.Property(x => x.PostingDate).HasColumnType("date").IsRequired(); builder.Property(x => x.SupplierDeliveryCode).HasMaxLength(100); builder.Property(x => x.Status).HasConversion<byte>().IsRequired(); builder.Property(x => x.InventoryTransactionId); builder.Property(x => x.JournalEntryId); builder.Property(x => x.Notes).HasMaxLength(1000); builder.Property(x => x.ConfirmedBy).HasMaxLength(64); builder.Property(x => x.ConfirmedAt).HasColumnType("datetimeoffset"); builder.Property(x => x.PostedBy).HasMaxLength(64); builder.Property(x => x.PostedAt).HasColumnType("datetimeoffset"); builder.Property(x => x.CancelledBy).HasMaxLength(64); builder.Property(x => x.CancelledAt).HasColumnType("datetimeoffset"); builder.Property(x => x.CancellationReason).HasMaxLength(500); builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.ReceiptCode).IsUnique().HasDatabaseName("UX_PurchaseReceipts_ReceiptCode"); builder.HasIndex(x => x.PurchaseOrderId).HasDatabaseName("IX_PurchaseReceipts_PurchaseOrderId"); builder.HasIndex(x => x.SupplierId).HasDatabaseName("IX_PurchaseReceipts_SupplierId"); builder.HasIndex(x => x.PostingDate).HasDatabaseName("IX_PurchaseReceipts_PostingDate"); builder.HasIndex(x => x.Status).HasDatabaseName("IX_PurchaseReceipts_Status");
        builder.HasOne<PurchaseOrder>().WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReceipts_Orders_PurchaseOrderId"); builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReceipts_Suppliers_SupplierId"); builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReceipts_Warehouses_WarehouseId"); builder.HasOne<InventoryTransaction>().WithMany().HasForeignKey(x => x.InventoryTransactionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReceipts_InventoryTransactions_InventoryTransactionId"); builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReceipts_JournalEntries_JournalEntryId"); builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PurchaseReceiptId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReceiptLines_Receipts_PurchaseReceiptId"); builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
