using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ToTable("tbl_PurchaseReturns", "dbo", t =>
        {
            t.HasCheckConstraint("CK_PurchaseReturns_Amounts_NonNegative", "[ReceiptCostBaseAmount] >= 0 AND [SupplierNetBaseAmount] >= 0 AND [SupplierTaxBaseAmount] >= 0 AND [SupplierGrossBaseAmount] >= 0 AND [InventoryCostBaseAmount] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigurePurchasingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ReturnCode).IsRequired().HasMaxLength(40);
        builder.Property(x => x.PurchaseReceiptId).IsRequired();
        builder.Property(x => x.PurchaseInvoiceId);
        builder.Property(x => x.SupplierId).IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.ReturnDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.PostingDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.ReceiptCostBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.SupplierNetBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.SupplierTaxBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.SupplierGrossBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.InventoryCostBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.PurchasePriceVarianceBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.JournalEntryId);
        builder.Property(x => x.ConfirmedAt).HasColumnType("datetimeoffset");
        builder.Property(x => x.ConfirmedBy).HasMaxLength(64);
        builder.Property(x => x.PostedAt).HasColumnType("datetimeoffset");
        builder.Property(x => x.PostedBy).HasMaxLength(64);
        builder.Property(x => x.CancelledAt).HasColumnType("datetimeoffset");
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.ReturnCode).IsUnique().HasDatabaseName("UX_PurchaseReturns_ReturnCode");
        builder.HasIndex(x => x.PurchaseReceiptId).HasDatabaseName("IX_PurchaseReturns_PurchaseReceiptId");
        builder.HasIndex(x => x.PurchaseInvoiceId).HasDatabaseName("IX_PurchaseReturns_PurchaseInvoiceId");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("IX_PurchaseReturns_SupplierId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_PurchaseReturns_Status");
        builder.HasIndex(x => x.PostingDate).HasDatabaseName("IX_PurchaseReturns_PostingDate");
        builder.HasOne<PurchaseReceipt>().WithMany().HasForeignKey(x => x.PurchaseReceiptId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturns_Receipts_PurchaseReceiptId");
        builder.HasOne<PurchaseInvoice>().WithMany().HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturns_Invoices_PurchaseInvoiceId");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturns_Suppliers_SupplierId");
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturns_Warehouses_WarehouseId");
        builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturns_Journals_JournalEntryId");
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PurchaseReturnId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturnLines_Returns_PurchaseReturnId");
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
