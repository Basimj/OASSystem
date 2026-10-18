using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseReceiptFollowUpJobConfiguration : IEntityTypeConfiguration<PurchaseReceiptFollowUpJob>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptFollowUpJob> b)
    {
        b.ToTable("tbl_PurchasingIntegrationJobs", "dbo", t =>
        {
            t.HasCheckConstraint("CK_PurchasingIntegrationJobs_Target", "([TargetKind] = 1 AND [CustomerOrderId] IS NOT NULL AND [PurchaseRequestLineId] IS NULL) OR ([TargetKind] = 2 AND [PurchaseRequestLineId] IS NOT NULL AND [CustomerOrderId] IS NULL)");
            t.HasCheckConstraint("CK_PurchasingIntegrationJobs_Attempts", "[AttemptCount] >= 0");
            t.HasCheckConstraint("CK_PurchasingIntegrationJobs_Status", "[Status] BETWEEN 1 AND 5");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TargetKind).HasConversion<byte>().IsRequired();
        b.Property(x => x.Status).HasConversion<byte>().IsRequired();
        b.Property(x => x.NextAttemptAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.LeaseExpiresAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CompletedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.LeaseOwner).HasMaxLength(100);
        b.Property(x => x.LastErrorCode).HasMaxLength(80);
        b.Property(x => x.LastErrorSummary).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.HasIndex(x => new { x.PurchaseReceiptId, x.CustomerOrderId }).IsUnique()
          .HasFilter("[CustomerOrderId] IS NOT NULL").HasDatabaseName("UX_PurchasingIntegrationJobs_Receipt_CustomerOrder");
        b.HasIndex(x => new { x.PurchaseReceiptId, x.PurchaseRequestLineId }).IsUnique()
          .HasFilter("[PurchaseRequestLineId] IS NOT NULL").HasDatabaseName("UX_PurchasingIntegrationJobs_Receipt_RequestLine");
        b.HasIndex(x => new { x.Status, x.NextAttemptAtUtc, x.LeaseExpiresAtUtc })
          .HasDatabaseName("IX_PurchasingIntegrationJobs_Status_NextAttempt");
        b.HasOne<PurchaseReceipt>().WithMany().HasForeignKey(x => x.PurchaseReceiptId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchasingIntegrationJobs_Receipts");
        b.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.CustomerOrderId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchasingIntegrationJobs_CustomerOrders");
        b.HasOne<PurchaseRequestLine>().WithMany().HasForeignKey(x => x.PurchaseRequestLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchasingIntegrationJobs_RequestLines");
    }
}
