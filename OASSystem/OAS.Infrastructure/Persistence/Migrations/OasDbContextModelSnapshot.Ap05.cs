using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Common.Entities;
using OAS.Domain.Purchasing.Entities;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

partial class OasDbContextModelSnapshot
{
    private IModel? _ap05Model;

    public override IModel Model => _ap05Model ??= BuildAp05Model();

    private IModel BuildAp05Model()
    {
        var modelBuilder = new ModelBuilder();
        BuildModel(modelBuilder);

        ConfigurePurchaseInvoiceAp05(modelBuilder.Entity<PurchaseInvoice>());
        ConfigureDueDateAudit(modelBuilder.Entity<PurchaseInvoiceDueDateAudit>());
        ConfigureOpeningBatch(modelBuilder.Entity<SupplierOpeningBatch>());
        ConfigureOpeningItem(modelBuilder.Entity<SupplierOpeningItem>());
        ConfigureOpeningApplication(modelBuilder.Entity<SupplierOpeningApplication>());

        return (IModel)modelBuilder.Model;
    }

    private static void ConfigurePurchaseInvoiceAp05(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.Property(x => x.PaymentTermDaysSnapshot);
        builder.Property(x => x.DueDate).HasColumnType("date");
        builder.Property(x => x.DueDateProvenance).HasConversion<byte>().IsRequired();
        builder.HasIndex(x => new { x.SupplierId, x.DueDate, x.Status })
            .HasDatabaseName("IX_PurchaseInvoices_Supplier_DueDate_Status");
        builder.ToTable("tbl_PurchaseInvoices", "dbo", t =>
        {
            t.HasCheckConstraint("CK_PurchaseInvoices_PaymentTermDaysSnapshot", "[PaymentTermDaysSnapshot] IS NULL OR [PaymentTermDaysSnapshot] >= 0");
            t.HasCheckConstraint("CK_PurchaseInvoices_DueDateProvenance", "[DueDateProvenance] BETWEEN 0 AND 3");
        });
    }

    private static void ConfigureDueDateAudit(EntityTypeBuilder<PurchaseInvoiceDueDateAudit> builder)
    {
        builder.ToTable("tbl_PurchaseInvoiceDueDateAudits", "dbo");
        builder.HasKey(x => x.Id);
        ConfigureAudit(builder);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PurchaseInvoiceId).IsRequired();
        builder.Property(x => x.OldDueDate).HasColumnType("date");
        builder.Property(x => x.OldPaymentTermDays);
        builder.Property(x => x.OldProvenance).HasConversion<byte>().IsRequired();
        builder.Property(x => x.NewDueDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.NewPaymentTermDays);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.VerifiedBy).HasMaxLength(64).IsRequired();
        builder.Property(x => x.VerifiedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.HasIndex(x => new { x.PurchaseInvoiceId, x.VerifiedAtUtc })
            .HasDatabaseName("IX_PurchaseInvoiceDueDateAudits_Invoice_VerifiedAt");
        builder.HasOne<PurchaseInvoice>().WithMany().HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PurchaseInvoiceDueDateAudits_PurchaseInvoices_PurchaseInvoiceId");
    }

    private static void ConfigureOpeningBatch(EntityTypeBuilder<SupplierOpeningBatch> builder)
    {
        builder.ToTable("tbl_SupplierOpeningBatches", "dbo");
        builder.HasKey(x => x.Id);
        ConfigureAudit(builder);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BatchCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.AsOfDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.BaseCurrencyId).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.JournalEntryId);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.PostedBy).HasMaxLength(64);
        builder.Property(x => x.PostedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.BatchCode).IsUnique().HasDatabaseName("UX_SupplierOpeningBatches_BatchCode");
        builder.HasIndex(x => x.JournalEntryId).IsUnique().HasFilter("[JournalEntryId] IS NOT NULL")
            .HasDatabaseName("UX_SupplierOpeningBatches_JournalEntryId");
        builder.HasIndex(x => new { x.AsOfDate, x.Status }).HasDatabaseName("IX_SupplierOpeningBatches_AsOf_Status");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.BaseCurrencyId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningBatches_Currencies_BaseCurrencyId");
        builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningBatches_JournalEntries_JournalEntryId");
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningItems_Batches_BatchId");
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureOpeningItem(EntityTypeBuilder<SupplierOpeningItem> builder)
    {
        builder.ToTable("tbl_SupplierOpeningItems", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SupplierOpeningItems_OriginalOpenAmount_Positive", "[OriginalOpenAmount] > 0");
            t.HasCheckConstraint("CK_SupplierOpeningItems_ExchangeRateSnapshot_Positive", "[ExchangeRateSnapshot] > 0");
            t.HasCheckConstraint("CK_SupplierOpeningItems_BaseOpenAmount_Positive", "[BaseOpenAmount] > 0");
            t.HasCheckConstraint("CK_SupplierOpeningItems_CurrencyDecimals", "[CurrencyDecimalPlacesSnapshot] BETWEEN 0 AND 6");
            t.HasCheckConstraint("CK_SupplierOpeningItems_DueDateProvenance", "[DueDateProvenance] BETWEEN 0 AND 2");
        });
        builder.HasKey(x => x.Id);
        ConfigureAudit(builder);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BatchId).IsRequired();
        builder.Property(x => x.SupplierId).IsRequired();
        builder.Property(x => x.LegacyReference).HasMaxLength(100);
        builder.Property(x => x.DocumentDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.DueDate).HasColumnType("date");
        builder.Property(x => x.DueDateProvenance).HasConversion<byte>().IsRequired();
        builder.Property(x => x.CurrencyId).IsRequired();
        builder.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(8).IsRequired();
        builder.Property(x => x.CurrencyDecimalPlacesSnapshot).IsRequired();
        builder.Property(x => x.OriginalOpenAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.ExchangeRateSnapshot).HasPrecision(19,8).IsRequired();
        builder.Property(x => x.BaseOpenAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.BatchId, x.SupplierId, x.LegacyReference })
            .IsUnique().HasFilter("[LegacyReference] IS NOT NULL")
            .HasDatabaseName("UX_SupplierOpeningItems_Batch_Supplier_LegacyReference");
        builder.HasIndex(x => new { x.SupplierId, x.DueDate })
            .HasDatabaseName("IX_SupplierOpeningItems_Supplier_DueDate");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningItems_Suppliers_SupplierId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningItems_Currencies_CurrencyId");
    }

    private static void ConfigureOpeningApplication(EntityTypeBuilder<SupplierOpeningApplication> builder)
    {
        builder.ToTable("tbl_SupplierOpeningApplications", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SupplierOpeningApplications_SourceAmount_Positive", "[AppliedSourceBaseAmount] > 0");
            t.HasCheckConstraint("CK_SupplierOpeningApplications_TargetAmount_Positive", "[AppliedTargetBaseAmount] > 0");
            t.HasCheckConstraint("CK_SupplierOpeningApplications_Status", "[Status] IN (1,2)");
        });
        builder.HasKey(x => x.Id);
        ConfigureAudit(builder);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SupplierOpeningItemId).IsRequired();
        builder.Property(x => x.PaymentVoucherLineId).IsRequired();
        builder.Property(x => x.SupplierId).IsRequired();
        builder.Property(x => x.AppliedSourceBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.AppliedTargetBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.AppliedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.AppliedBy).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(80);
        builder.Property(x => x.ReversedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.ReversedBy).HasMaxLength(64);
        builder.Property(x => x.ReversalReason).HasMaxLength(300);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_SupplierOpeningApplications_IdempotencyKey");
        builder.HasIndex(x => new { x.SupplierOpeningItemId, x.Status })
            .HasDatabaseName("IX_SupplierOpeningApplications_Item_Status");
        builder.HasIndex(x => new { x.PaymentVoucherLineId, x.Status })
            .HasDatabaseName("IX_SupplierOpeningApplications_PaymentLine_Status");
        builder.HasOne<SupplierOpeningItem>().WithMany().HasForeignKey(x => x.SupplierOpeningItemId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningApplications_Items_ItemId");
        builder.HasOne<PaymentVoucherLine>().WithMany().HasForeignKey(x => x.PaymentVoucherLineId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningApplications_PaymentVoucherLines_LineId");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningApplications_Suppliers_SupplierId");
    }

    private static void ConfigureAudit<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity<Guid>
    {
        builder.Property(x => x.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetimeoffset").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy").HasMaxLength(64);
        builder.Property(x => x.LastModifiedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetimeoffset");
        builder.Property(x => x.LastModifiedBy).HasColumnName("UpdatedBy").HasMaxLength(64);
        builder.Property<string?>("CreatedFromDevice").HasColumnName("CreatedFromDevice").HasMaxLength(256);
        builder.Property<string?>("UpdatedFromDevice").HasColumnName("UpdatedFromDevice").HasMaxLength(256);
    }
}
