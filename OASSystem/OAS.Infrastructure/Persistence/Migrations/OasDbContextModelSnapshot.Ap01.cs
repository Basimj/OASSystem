using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

// Compatibility augmentation for the current generated snapshot.
//
// The generated snapshot on main configures EmployeeSalaryStructure.Lines before
// the relationship that creates that navigation. EF Core 9 therefore throws while
// materializing ModelSnapshot.Model. AP-01 also needs SupplierCreditApplication to
// participate in the snapshot so future migration diffs do not try to recreate it.
//
// Keep this partial beside the generated snapshot until the next EF-generated
// snapshot folds these two pieces into OasDbContextModelSnapshot.cs.
sealed partial class OasDbContextModelSnapshot
{
    private IModel? _ap01Model;

    public override IModel Model => _ap01Model ??= BuildAp01SnapshotModel();

    private IModel BuildAp01SnapshotModel()
    {
        var modelBuilder = new ModelBuilder();

        // Pre-create the collection navigation before the generated BuildModel
        // reaches b.Navigation("Lines"). The generated relationship later applies
        // its final delete behavior and constraint name to this same relationship.
        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.EmployeeSalaryStructure", b =>
        {
            b.HasMany(
                    "OAS.Domain.Features.Employees.Entities.EmployeeSalaryStructureLine",
                    "Lines")
                .WithOne()
                .HasForeignKey("EmployeeSalaryStructureId");
        });

        BuildModel(modelBuilder);
        ConfigureSupplierCreditApplication(modelBuilder);

        return (IModel)modelBuilder.Model;
    }

    private static void ConfigureSupplierCreditApplication(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.SupplierCreditApplication", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PurchaseReturnId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("PurchaseInvoiceId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("SupplierId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("AppliedSourceBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("AppliedTargetBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("AppliedInvoiceCurrencyAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<DateTime>("AppliedAtUtc").IsRequired().HasColumnType("datetime2(3)");
            b.Property<string>("AppliedBy").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<string>("IdempotencyKey").HasMaxLength(80).HasColumnType("nvarchar(80)");
            b.Property<DateTime?>("ReversedAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("ReversedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ReversalReason").HasMaxLength(300).HasColumnType("nvarchar(300)");
            b.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");

            b.HasKey("Id").HasName("PK_tbl_SupplierCreditApplications");
            b.HasIndex("PurchaseReturnId", "Status").HasDatabaseName("IX_SupplierCreditApplications_Return_Status");
            b.HasIndex("PurchaseInvoiceId", "Status").HasDatabaseName("IX_SupplierCreditApplications_Invoice_Status");
            b.HasIndex("SupplierId", "Status").HasDatabaseName("IX_SupplierCreditApplications_Supplier_Status");
            b.HasIndex("IdempotencyKey")
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL")
                .HasDatabaseName("UX_SupplierCreditApplications_IdempotencyKey");

            b.ToTable("tbl_SupplierCreditApplications", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SupplierCreditApplications_SourceBase_Positive", "[AppliedSourceBaseAmount] > 0");
                t.HasCheckConstraint("CK_SupplierCreditApplications_TargetBase_Positive", "[AppliedTargetBaseAmount] > 0");
                t.HasCheckConstraint("CK_SupplierCreditApplications_InvoiceAmount_Positive", "[AppliedInvoiceCurrencyAmount] > 0");
                t.HasCheckConstraint("CK_SupplierCreditApplications_Status", "[Status] IN (1,2)");
                t.HasCheckConstraint(
                    "CK_SupplierCreditApplications_ReversalState",
                    "([Status] = 1 AND [ReversedAtUtc] IS NULL AND [ReversedBy] IS NULL AND [ReversalReason] IS NULL) OR ([Status] = 2 AND [ReversedAtUtc] IS NOT NULL AND [ReversedBy] IS NOT NULL AND [ReversalReason] IS NOT NULL)");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.SupplierCreditApplication", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseReturn", null)
                .WithMany()
                .HasForeignKey("PurchaseReturnId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SupplierCreditApplications_PurchaseReturns_PurchaseReturnId");

            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseInvoice", null)
                .WithMany()
                .HasForeignKey("PurchaseInvoiceId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SupplierCreditApplications_PurchaseInvoices_PurchaseInvoiceId");

            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null)
                .WithMany()
                .HasForeignKey("SupplierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SupplierCreditApplications_Suppliers_SupplierId");
        });
    }
}
