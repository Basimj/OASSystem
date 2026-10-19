using Microsoft.EntityFrameworkCore;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

// Snapshot-only metadata for schema changes that already have checked-in migrations.
// This must NOT be applied as a runtime IEntityTypeConfiguration: the generated
// migration snapshot uses shared-type/property-bag entity definitions by name.
partial class OasDbContextModelSnapshot
{
    private static void AddSa07PreviouslyMigratedModel(ModelBuilder modelBuilder)
    {
        // AP-02: Purchase invoice currency/rate snapshots. Migration 20261017150000.
        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoice", b =>
        {
            b.Property<string>("BaseCurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<byte?>("BaseCurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<Guid?>("BaseCurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<byte?>("CurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<byte?>("ExchangeRateSource").HasColumnType("tinyint");
            b.Property<byte?>("ExchangeRateType").HasColumnType("tinyint");
            b.HasIndex("BaseCurrencyId").HasDatabaseName("IX_PurchaseInvoices_BaseCurrencyId");
            b.ToTable("tbl_PurchaseInvoices", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseInvoices_BaseCurrencyDecimalPlacesSnapshot", "[BaseCurrencyDecimalPlacesSnapshot] IS NULL OR ([BaseCurrencyDecimalPlacesSnapshot] BETWEEN 0 AND 6)");
                t.HasCheckConstraint("CK_PurchaseInvoices_CurrencyDecimalPlacesSnapshot", "[CurrencyDecimalPlacesSnapshot] IS NULL OR ([CurrencyDecimalPlacesSnapshot] BETWEEN 0 AND 6)");
            });
        });
        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoice", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null)
                .WithMany().HasForeignKey("BaseCurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchaseInvoices_Currencies_BaseCurrencyId");
        });

        // AP-02/AP-03: Payment allocation FX, concurrency and reversal fields.
        // Migrations 20261017150000 and 20261017163000 already create the DDL.
        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentAllocation", b =>
        {
            b.Property<string>("IdempotencyKey").HasMaxLength(80).HasColumnType("nvarchar(80)");
            b.Property<string>("ReversalReason").HasMaxLength(300).HasColumnType("nvarchar(300)");
            b.Property<DateTime?>("ReversedAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("ReversedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte[]>("RowVersion")
                .IsRequired().IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<decimal?>("TargetDocumentAmount").HasPrecision(19, 4).HasColumnType("decimal(19,4)");
            b.HasIndex("PaymentVoucherLineId", "AllocatedAtUtc")
                .HasDatabaseName("IX_PaymentAllocations_PaymentVoucherLine_Date");
            b.HasIndex("TargetDocumentType", "TargetDocumentId", "AllocatedAtUtc")
                .HasDatabaseName("IX_PaymentAllocations_TargetDocument_Date");
            b.HasIndex("IdempotencyKey").IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL")
                .HasDatabaseName("UX_PaymentAllocations_IdempotencyKey");
            b.ToTable("tbl_PaymentAllocations", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PaymentAllocations_ReversalAudit", "([ReversedAtUtc] IS NULL AND [ReversedBy] IS NULL AND [ReversalReason] IS NULL) OR ([ReversedAtUtc] IS NOT NULL AND [ReversedBy] IS NOT NULL AND [ReversalReason] IS NOT NULL)");
                t.HasCheckConstraint("CK_PaymentAllocations_TargetDocumentAmount_Positive", "[TargetDocumentAmount] IS NULL OR [TargetDocumentAmount] > 0");
            });
        });

        // AP-01: the table exists in migration 20261017040000. Snapshot must not
        // make EF attempt to CREATE it again in a later deployment.
        const string supplierCredit = "OAS.Domain.Purchasing.Entities.SupplierCreditApplication";
        modelBuilder.Entity(supplierCredit, b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            AddAudit(b);
            b.Property<Guid>("PurchaseReturnId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("PurchaseInvoiceId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("SupplierId").HasColumnType("uniqueidentifier");
            b.Property<decimal>("AppliedSourceBaseAmount").HasPrecision(19, 4).HasColumnType("decimal(19,4)");
            b.Property<decimal>("AppliedTargetBaseAmount").HasPrecision(19, 4).HasColumnType("decimal(19,4)");
            b.Property<decimal>("AppliedInvoiceCurrencyAmount").HasPrecision(19, 4).HasColumnType("decimal(19,4)");
            b.Property<DateTime>("AppliedAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("AppliedBy").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<string>("IdempotencyKey").HasMaxLength(80).HasColumnType("nvarchar(80)");
            b.Property<DateTime?>("ReversedAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("ReversedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ReversalReason").HasMaxLength(300).HasColumnType("nvarchar(300)");
            b.Property<byte[]>("RowVersion")
                .IsRequired().IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id").HasName("PK_tbl_SupplierCreditApplications");
            b.HasIndex("PurchaseReturnId", "Status")
                .HasDatabaseName("IX_SupplierCreditApplications_Return_Status");
            b.HasIndex("PurchaseInvoiceId", "Status")
                .HasDatabaseName("IX_SupplierCreditApplications_Invoice_Status");
            b.HasIndex("SupplierId", "Status")
                .HasDatabaseName("IX_SupplierCreditApplications_Supplier_Status");
            b.HasIndex("IdempotencyKey").IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL")
                .HasDatabaseName("UX_SupplierCreditApplications_IdempotencyKey");
            b.ToTable("tbl_SupplierCreditApplications", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SupplierCreditApplications_SourceBase_Positive", "[AppliedSourceBaseAmount] > 0");
                t.HasCheckConstraint("CK_SupplierCreditApplications_TargetBase_Positive", "[AppliedTargetBaseAmount] > 0");
                t.HasCheckConstraint("CK_SupplierCreditApplications_InvoiceAmount_Positive", "[AppliedInvoiceCurrencyAmount] > 0");
                t.HasCheckConstraint("CK_SupplierCreditApplications_Status", "[Status] IN (1,2)");
                t.HasCheckConstraint("CK_SupplierCreditApplications_ReversalState", "([Status] = 1 AND [ReversedAtUtc] IS NULL AND [ReversedBy] IS NULL AND [ReversalReason] IS NULL) OR ([Status] = 2 AND [ReversedAtUtc] IS NOT NULL AND [ReversedBy] IS NOT NULL AND [ReversalReason] IS NOT NULL)");
            });
        });
        modelBuilder.Entity(supplierCredit, b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseReturn", null)
                .WithMany().HasForeignKey("PurchaseReturnId").OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_SupplierCreditApplications_PurchaseReturns_PurchaseReturnId");
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseInvoice", null)
                .WithMany().HasForeignKey("PurchaseInvoiceId").OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_SupplierCreditApplications_PurchaseInvoices_PurchaseInvoiceId");
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null)
                .WithMany().HasForeignKey("SupplierId").OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_SupplierCreditApplications_Suppliers_SupplierId");
        });


        // EF metadata may materialize audit column names before table mapping is
        // final. Reapply these column annotations after all property-bag tables
        // are registered; their physical columns were already migrated as UpdatedBy.
        foreach (var name in new[]
        {
            "OAS.Domain.Accounting.Entities.SupplierOpeningItem",
            "OAS.Domain.Accounting.Entities.SupplierOpeningBatch",
            "OAS.Domain.Accounting.Entities.SupplierOpeningApplication",
            "OAS.Domain.Purchasing.Entities.PurchaseInvoiceDueDateAudit",
            "OAS.Domain.Sales.Entities.CustomerCreditApplication",
            "OAS.Domain.Purchasing.Entities.SupplierCreditApplication"
        })
        {
            modelBuilder.Entity(name, b =>
                b.Property<string>("LastModifiedBy").HasColumnName("UpdatedBy"));
        }

        // AP-09: missing relationship indexes, already created by migration.
        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceiptFollowUpJob", b =>
        {
            b.HasIndex("CustomerOrderId").HasDatabaseName("IX_tbl_PurchasingIntegrationJobs_CustomerOrderId");
            b.HasIndex("PurchaseRequestLineId").HasDatabaseName("IX_tbl_PurchasingIntegrationJobs_PurchaseRequestLineId");
        });
    }
}
