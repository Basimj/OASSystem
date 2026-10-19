using Microsoft.EntityFrameworkCore;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

partial class OasDbContextModelSnapshot
{
    // The generated model snapshot is a property-bag model: entities in BuildModel
    // are declared using Entity(string, ...), not CLR entity types.
    // Applying the runtime IEntityTypeConfiguration<PurchaseReceiptFollowUpJob>
    // here implicitly introduces a CLR PurchaseReceipt principal and collides
    // with the existing property-bag entity of that exact same model name.
    // Preserve the snapshot's own string-based representation instead.
    private static void AddAp09PurchaseReceiptFollowUpModel(ModelBuilder modelBuilder)
    {
        const string entityName = "OAS.Domain.Purchasing.Entities.PurchaseReceiptFollowUpJob";

        modelBuilder.Entity(entityName, b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<Guid>("PurchaseReceiptId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CustomerOrderId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("PurchaseRequestLineId").HasColumnType("uniqueidentifier");
            b.Property<byte>("TargetKind").HasColumnType("tinyint");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<int>("AttemptCount").HasColumnType("int");
            b.Property<DateTime>("NextAttemptAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("LeaseOwner").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<DateTime?>("LeaseExpiresAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("LastErrorCode").HasMaxLength(80).HasColumnType("nvarchar(80)");
            b.Property<string>("LastErrorSummary").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2(3)");
            b.Property<DateTime?>("CompletedAtUtc").HasColumnType("datetime2(3)");
            b.Property<byte[]>("RowVersion")
                .IsRequired().IsConcurrencyToken().ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");

            b.HasKey("Id");
            b.HasIndex("PurchaseReceiptId", "CustomerOrderId")
                .IsUnique().HasFilter("[CustomerOrderId] IS NOT NULL")
                .HasDatabaseName("UX_PurchasingIntegrationJobs_Receipt_CustomerOrder");
            b.HasIndex("PurchaseReceiptId", "PurchaseRequestLineId")
                .IsUnique().HasFilter("[PurchaseRequestLineId] IS NOT NULL")
                .HasDatabaseName("UX_PurchasingIntegrationJobs_Receipt_RequestLine");
            b.HasIndex("Status", "NextAttemptAtUtc", "LeaseExpiresAtUtc")
                .HasDatabaseName("IX_PurchasingIntegrationJobs_Status_NextAttempt");

            b.ToTable("tbl_PurchasingIntegrationJobs", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchasingIntegrationJobs_Target",
                    "([TargetKind] = 1 AND [CustomerOrderId] IS NOT NULL AND [PurchaseRequestLineId] IS NULL) OR ([TargetKind] = 2 AND [PurchaseRequestLineId] IS NOT NULL AND [CustomerOrderId] IS NULL)");
                t.HasCheckConstraint("CK_PurchasingIntegrationJobs_Attempts", "[AttemptCount] >= 0");
                t.HasCheckConstraint("CK_PurchasingIntegrationJobs_Status", "[Status] BETWEEN 1 AND 5");
            });
        });

        modelBuilder.Entity(entityName, b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseReceipt", null)
                .WithMany().HasForeignKey("PurchaseReceiptId")
                .OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_PurchasingIntegrationJobs_Receipts");
            b.HasOne("OAS.Domain.Sales.Entities.CustomerOrder", null)
                .WithMany().HasForeignKey("CustomerOrderId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchasingIntegrationJobs_CustomerOrders");
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseRequestLine", null)
                .WithMany().HasForeignKey("PurchaseRequestLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchasingIntegrationJobs_RequestLines");
        });
    }
}
