using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class SupplierCreditApplicationConfiguration : IEntityTypeConfiguration<SupplierCreditApplication>
{
    public void Configure(EntityTypeBuilder<SupplierCreditApplication> builder)
    {
        builder.ToTable("tbl_SupplierCreditApplications", "dbo", table =>
        {
            table.HasCheckConstraint("CK_SupplierCreditApplications_SourceBase_Positive", "[AppliedSourceBaseAmount] > 0");
            table.HasCheckConstraint("CK_SupplierCreditApplications_TargetBase_Positive", "[AppliedTargetBaseAmount] > 0");
            table.HasCheckConstraint("CK_SupplierCreditApplications_InvoiceAmount_Positive", "[AppliedInvoiceCurrencyAmount] > 0");
            table.HasCheckConstraint("CK_SupplierCreditApplications_Status", "[Status] IN (1,2)");
            table.HasCheckConstraint("CK_SupplierCreditApplications_ReversalState", "([Status] = 1 AND [ReversedAtUtc] IS NULL AND [ReversedBy] IS NULL AND [ReversalReason] IS NULL) OR ([Status] = 2 AND [ReversedAtUtc] IS NOT NULL AND [ReversedBy] IS NOT NULL AND [ReversalReason] IS NOT NULL)");
        });

        builder.HasKey(x => x.Id).HasName("PK_tbl_SupplierCreditApplications");
        builder.ConfigurePurchasingAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PurchaseReturnId).IsRequired();
        builder.Property(x => x.PurchaseInvoiceId).IsRequired();
        builder.Property(x => x.SupplierId).IsRequired();
        builder.Property(x => x.AppliedSourceBaseAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.AppliedTargetBaseAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.AppliedInvoiceCurrencyAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.AppliedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.AppliedBy).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(80);
        builder.Property(x => x.ReversedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.ReversedBy).HasMaxLength(64);
        builder.Property(x => x.ReversalReason).HasMaxLength(300);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.PurchaseReturnId, x.Status })
            .HasDatabaseName("IX_SupplierCreditApplications_Return_Status");
        builder.HasIndex(x => new { x.PurchaseInvoiceId, x.Status })
            .HasDatabaseName("IX_SupplierCreditApplications_Invoice_Status");
        builder.HasIndex(x => new { x.SupplierId, x.Status })
            .HasDatabaseName("IX_SupplierCreditApplications_Supplier_Status");
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_SupplierCreditApplications_IdempotencyKey");

        builder.HasOne<PurchaseReturn>().WithMany().HasForeignKey(x => x.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierCreditApplications_PurchaseReturns_PurchaseReturnId");
        builder.HasOne<PurchaseInvoice>().WithMany().HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierCreditApplications_PurchaseInvoices_PurchaseInvoiceId");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierCreditApplications_Suppliers_SupplierId");
    }
}
