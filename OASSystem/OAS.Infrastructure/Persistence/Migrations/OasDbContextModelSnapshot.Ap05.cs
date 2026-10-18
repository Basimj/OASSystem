using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoice", b =>
        {
            b.Property<DateOnly?>("DueDate").HasColumnType("date");
            b.Property<byte>("DueDateProvenance").HasColumnType("tinyint");
            b.Property<int?>("PaymentTermDaysSnapshot").HasColumnType("int");
            b.HasIndex("SupplierId", "DueDate", "Status")
                .HasDatabaseName("IX_PurchaseInvoices_Supplier_DueDate_Status");
            b.ToTable("tbl_PurchaseInvoices", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseInvoices_DueDateProvenance", "[DueDateProvenance] BETWEEN 0 AND 3");
                t.HasCheckConstraint("CK_PurchaseInvoices_PaymentTermDaysSnapshot", "[PaymentTermDaysSnapshot] IS NULL OR [PaymentTermDaysSnapshot] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoiceDueDateAudit", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            AddAudit(b);
            b.Property<DateOnly?>("OldDueDate").HasColumnType("date");
            b.Property<int?>("OldPaymentTermDays").HasColumnType("int");
            b.Property<byte>("OldProvenance").HasColumnType("tinyint");
            b.Property<DateOnly>("NewDueDate").HasColumnType("date");
            b.Property<int?>("NewPaymentTermDays").HasColumnType("int");
            b.Property<Guid>("PurchaseInvoiceId").HasColumnType("uniqueidentifier");
            b.Property<string>("Reason").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("VerifiedBy").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTime>("VerifiedAtUtc").HasColumnType("datetime2(3)");
            b.HasKey("Id");
            b.HasIndex("PurchaseInvoiceId", "VerifiedAtUtc")
                .HasDatabaseName("IX_PurchaseInvoiceDueDateAudits_Invoice_VerifiedAt");
            b.ToTable("tbl_PurchaseInvoiceDueDateAudits", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.SupplierOpeningBatch", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            AddAudit(b);
            b.Property<DateOnly>("AsOfDate").HasColumnType("date");
            b.Property<Guid>("BaseCurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("BatchCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<DateTime?>("PostedAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("PostedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.HasKey("Id");
            b.HasIndex("AsOfDate", "Status").HasDatabaseName("IX_SupplierOpeningBatches_AsOf_Status");
            b.HasIndex("BaseCurrencyId");
            b.HasIndex("BatchCode").IsUnique().HasDatabaseName("UX_SupplierOpeningBatches_BatchCode");
            b.HasIndex("JournalEntryId").IsUnique().HasFilter("[JournalEntryId] IS NOT NULL")
                .HasDatabaseName("UX_SupplierOpeningBatches_JournalEntryId");
            b.ToTable("tbl_SupplierOpeningBatches", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.SupplierOpeningItem", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            AddAudit(b);
            b.Property<Guid>("BatchId").HasColumnType("uniqueidentifier");
            b.Property<decimal>("BaseOpenAmount").HasPrecision(19, 4).HasColumnType("decimal(19,4)");
            b.Property<Guid>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencyCodeSnapshot").IsRequired().HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<byte>("CurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<DateOnly>("DocumentDate").HasColumnType("date");
            b.Property<DateOnly?>("DueDate").HasColumnType("date");
            b.Property<byte>("DueDateProvenance").HasColumnType("tinyint");
            b.Property<decimal>("ExchangeRateSnapshot").HasPrecision(19, 8).HasColumnType("decimal(19,8)");
            b.Property<string>("LegacyReference").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<decimal>("OriginalOpenAmount").HasPrecision(19, 4).HasColumnType("decimal(19,4)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<Guid>("SupplierId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("BatchId");
            b.HasIndex("CurrencyId");
            b.HasIndex("SupplierId", "DueDate").HasDatabaseName("IX_SupplierOpeningItems_Supplier_DueDate");
            b.HasIndex("BatchId", "SupplierId", "LegacyReference").IsUnique().HasFilter("[LegacyReference] IS NOT NULL")
                .HasDatabaseName("UX_SupplierOpeningItems_Batch_Supplier_LegacyReference");
            b.ToTable("tbl_SupplierOpeningItems", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SupplierOpeningItems_BaseOpenAmount_Positive", "[BaseOpenAmount] > 0");
                t.HasCheckConstraint("CK_SupplierOpeningItems_CurrencyDecimals", "[CurrencyDecimalPlacesSnapshot] BETWEEN 0 AND 6");
                t.HasCheckConstraint("CK_SupplierOpeningItems_DueDateProvenance", "[DueDateProvenance] BETWEEN 0 AND 2");
                t.HasCheckConstraint("CK_SupplierOpeningItems_ExchangeRateSnapshot_Positive", "[ExchangeRateSnapshot] > 0");
                t.HasCheckConstraint("CK_SupplierOpeningItems_OriginalOpenAmount_Positive", "[OriginalOpenAmount] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.SupplierOpeningApplication", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            AddAudit(b);
            b.Property<DateTime>("AppliedAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("AppliedBy").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<decimal>("AppliedSourceBaseAmount").HasPrecision(19, 4).HasColumnType("decimal(19,4)");
            b.Property<decimal>("AppliedTargetBaseAmount").HasPrecision(19, 4).HasColumnType("decimal(19,4)");
            b.Property<string>("IdempotencyKey").HasMaxLength(80).HasColumnType("nvarchar(80)");
            b.Property<Guid>("PaymentVoucherLineId").HasColumnType("uniqueidentifier");
            b.Property<string>("ReversalReason").HasMaxLength(300).HasColumnType("nvarchar(300)");
            b.Property<DateTime?>("ReversedAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("ReversedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<Guid>("SupplierId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("SupplierOpeningItemId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("IdempotencyKey").IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL")
                .HasDatabaseName("UX_SupplierOpeningApplications_IdempotencyKey");
            b.HasIndex("PaymentVoucherLineId", "Status")
                .HasDatabaseName("IX_SupplierOpeningApplications_PaymentLine_Status");
            b.HasIndex("SupplierId");
            b.HasIndex("SupplierOpeningItemId", "Status")
                .HasDatabaseName("IX_SupplierOpeningApplications_Item_Status");
            b.ToTable("tbl_SupplierOpeningApplications", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SupplierOpeningApplications_SourceAmount_Positive", "[AppliedSourceBaseAmount] > 0");
                t.HasCheckConstraint("CK_SupplierOpeningApplications_Status", "[Status] IN (1,2)");
                t.HasCheckConstraint("CK_SupplierOpeningApplications_TargetAmount_Positive", "[AppliedTargetBaseAmount] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoiceDueDateAudit", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseInvoice", null)
                .WithMany()
                .HasForeignKey("PurchaseInvoiceId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseInvoiceDueDateAudits_PurchaseInvoices_PurchaseInvoiceId");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.SupplierOpeningBatch", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null)
                .WithMany().HasForeignKey("BaseCurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_SupplierOpeningBatches_Currencies_BaseCurrencyId");
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null)
                .WithMany().HasForeignKey("JournalEntryId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SupplierOpeningBatches_JournalEntries_JournalEntryId");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.SupplierOpeningItem", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.SupplierOpeningBatch", null)
                .WithMany("Items").HasForeignKey("BatchId").OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_SupplierOpeningItems_Batches_BatchId");
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null)
                .WithMany().HasForeignKey("CurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_SupplierOpeningItems_Currencies_CurrencyId");
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null)
                .WithMany().HasForeignKey("SupplierId").OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_SupplierOpeningItems_Suppliers_SupplierId");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.SupplierOpeningApplication", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.PaymentVoucherLine", null)
                .WithMany().HasForeignKey("PaymentVoucherLineId").OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_SupplierOpeningApplications_PaymentVoucherLines_LineId");
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null)
                .WithMany().HasForeignKey("SupplierId").OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_SupplierOpeningApplications_Suppliers_SupplierId");
            b.HasOne("OAS.Domain.Accounting.Entities.SupplierOpeningItem", null)
                .WithMany().HasForeignKey("SupplierOpeningItemId").OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_SupplierOpeningApplications_Items_ItemId");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.SupplierOpeningBatch", b =>
        {
            b.Navigation("Items").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // SA-03: Include the customer credit application table in the effective snapshot.
        AddSa03CustomerCreditModel(modelBuilder);
        modelBuilder.ApplyConfiguration(new OAS.Infrastructure.Purchasing.Persistence.Configurations.PurchaseReceiptFollowUpJobConfiguration());
        return (IModel)modelBuilder.Model;
    }

    private static void AddAudit(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder b)
    {
        b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnName("CreatedAt").HasColumnType("datetimeoffset");
        b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnName("CreatedBy").HasColumnType("nvarchar(64)");
        b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
        b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnName("UpdatedAt").HasColumnType("datetimeoffset");
        b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
        b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
    }
}
