using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class CustomerCreditApplicationConfiguration : IEntityTypeConfiguration<CustomerCreditApplication>
{
    public void Configure(EntityTypeBuilder<CustomerCreditApplication> b)
    {
        b.ToTable("tbl_CustomerCreditApplications", "dbo", t =>
        {
            t.HasCheckConstraint("CK_CustomerCreditApplications_SourcePositive", "[AppliedSourceBaseAmount] > 0");
            t.HasCheckConstraint("CK_CustomerCreditApplications_Kind", "[SettlementKind] IN (1,2)");
            t.HasCheckConstraint("CK_CustomerCreditApplications_Status", "[Status] IN (1,2)");
            t.HasCheckConstraint("CK_CustomerCreditApplications_TargetShape", "([SettlementKind]=1 AND [TargetSalesInvoiceId] IS NOT NULL AND [RefundPaymentVoucherLineId] IS NULL AND [TargetBaseAmount]>0 AND [TargetInvoiceCurrencyAmount]>0 AND [RefundSettlementBaseAmount] IS NULL) OR ([SettlementKind]=2 AND [TargetSalesInvoiceId] IS NULL AND [RefundPaymentVoucherLineId] IS NOT NULL AND [TargetBaseAmount] IS NULL AND [TargetInvoiceCurrencyAmount] IS NULL AND [RefundSettlementBaseAmount]>0)");
            t.HasCheckConstraint("CK_CustomerCreditApplications_ReversalState", "([Status]=1 AND [ReversedAtUtc] IS NULL AND [ReversedBy] IS NULL AND [ReversalReason] IS NULL) OR ([Status]=2 AND [ReversedAtUtc] IS NOT NULL AND [ReversedBy] IS NOT NULL AND [ReversalReason] IS NOT NULL)");
        });
        b.HasKey(x=>x.Id);
        b.Property(x=>x.Id).ValueGeneratedNever();
        b.Property(x=>x.SettlementKind).HasConversion<byte>();
        b.Property(x=>x.Status).HasConversion<byte>();
        b.Property(x=>x.AppliedSourceBaseAmount).HasPrecision(19,4);
        b.Property(x=>x.TargetBaseAmount).HasPrecision(19,4);
        b.Property(x=>x.TargetInvoiceCurrencyAmount).HasPrecision(19,4);
        b.Property(x=>x.RefundSettlementBaseAmount).HasPrecision(19,4);
        b.Property(x=>x.ReversalReceiptVoucherLineId);
        b.Property(x=>x.AppliedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x=>x.AppliedBy).IsRequired().HasMaxLength(64);
        b.Property(x=>x.ReversedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x=>x.ReversedBy).HasMaxLength(64);
        b.Property(x=>x.ReversalReason).HasMaxLength(300);
        b.Property(x=>x.IdempotencyKey).HasMaxLength(80);
        b.Property(x=>x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.Property(x=>x.CreatedAtUtc).HasColumnName("CreatedAt");
        b.Property(x=>x.LastModifiedAtUtc).HasColumnName("UpdatedAt");
        b.Property(x=>x.LastModifiedBy).HasColumnName("UpdatedBy").HasMaxLength(64);
        b.Property(x=>x.CreatedBy).HasMaxLength(64);
        b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
        b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
        b.HasIndex(x=>new {x.SalesReturnId,x.Status}).HasDatabaseName("IX_CustomerCreditApplications_Return_Status");
        b.HasIndex(x=>new {x.TargetSalesInvoiceId,x.Status}).HasDatabaseName("IX_CustomerCreditApplications_Invoice_Status");
        b.HasIndex(x=>new {x.CustomerId,x.Status}).HasDatabaseName("IX_CustomerCreditApplications_Customer_Status");
        b.HasIndex(x=>x.ReversalReceiptVoucherLineId).IsUnique().HasFilter("[ReversalReceiptVoucherLineId] IS NOT NULL")
            .HasDatabaseName("UX_CustomerCreditApplications_ReversalReceipt");
        b.HasIndex(x=>x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_CustomerCreditApplications_IdempotencyKey");
        b.HasIndex(x=>x.RefundPaymentVoucherLineId).IsUnique()
            .HasFilter("[RefundPaymentVoucherLineId] IS NOT NULL")
            .HasDatabaseName("UX_CustomerCreditApplications_RefundLine");
        b.HasOne<SalesReturn>().WithMany().HasForeignKey(x=>x.SalesReturnId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SalesInvoice>().WithMany().HasForeignKey(x=>x.TargetSalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Customer>().WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PaymentVoucherLine>().WithMany().HasForeignKey(x=>x.RefundPaymentVoucherLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ReceiptVoucherLine>().WithMany().HasForeignKey(x=>x.ReversalReceiptVoucherLineId).OnDelete(DeleteBehavior.Restrict);
    }
}
