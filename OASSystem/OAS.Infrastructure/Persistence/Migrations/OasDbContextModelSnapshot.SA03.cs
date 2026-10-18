using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

// Called by the installer's guarded one-line hook inside BuildAp05Model.
partial class OasDbContextModelSnapshot
{
    private static void AddSa03CustomerCreditModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerCreditApplication",b=>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            AddAudit(b);
            b.Property<Guid>("SalesReturnId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("CustomerId").HasColumnType("uniqueidentifier");
            b.Property<byte>("SettlementKind").HasColumnType("tinyint");
            b.Property<Guid?>("TargetSalesInvoiceId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("RefundPaymentVoucherLineId").HasColumnType("uniqueidentifier");
            b.Property<decimal>("AppliedSourceBaseAmount").HasPrecision(19,4).HasColumnType("decimal(19,4)");
            b.Property<decimal?>("TargetBaseAmount").HasPrecision(19,4).HasColumnType("decimal(19,4)");
            b.Property<decimal?>("TargetInvoiceCurrencyAmount").HasPrecision(19,4).HasColumnType("decimal(19,4)");
            b.Property<decimal?>("RefundSettlementBaseAmount").HasPrecision(19,4).HasColumnType("decimal(19,4)");
            b.Property<Guid?>("ReversalReceiptVoucherLineId").HasColumnType("uniqueidentifier");
            b.Property<DateTime>("AppliedAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("AppliedBy").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<DateTime?>("ReversedAtUtc").HasColumnType("datetime2(3)");
            b.Property<string>("ReversedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ReversalReason").HasMaxLength(300).HasColumnType("nvarchar(300)");
            b.Property<string>("IdempotencyKey").HasMaxLength(80).HasColumnType("nvarchar(80)");
            b.Property<byte[]>("RowVersion").IsRequired().IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id");
            b.HasIndex("SalesReturnId","Status").HasDatabaseName("IX_CustomerCreditApplications_Return_Status");
            b.HasIndex("TargetSalesInvoiceId","Status").HasDatabaseName("IX_CustomerCreditApplications_Invoice_Status");
            b.HasIndex("CustomerId","Status").HasDatabaseName("IX_CustomerCreditApplications_Customer_Status");
            b.HasIndex("ReversalReceiptVoucherLineId").IsUnique().HasFilter("[ReversalReceiptVoucherLineId] IS NOT NULL")
                .HasDatabaseName("UX_CustomerCreditApplications_ReversalReceipt");
            b.HasIndex("IdempotencyKey").IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL")
                .HasDatabaseName("UX_CustomerCreditApplications_IdempotencyKey");
            b.HasIndex("RefundPaymentVoucherLineId").IsUnique().HasFilter("[RefundPaymentVoucherLineId] IS NOT NULL")
                .HasDatabaseName("UX_CustomerCreditApplications_RefundLine");
            b.ToTable("tbl_CustomerCreditApplications","dbo",t=>
            {
                t.HasCheckConstraint("CK_CustomerCreditApplications_SourcePositive","[AppliedSourceBaseAmount] > 0");
                t.HasCheckConstraint("CK_CustomerCreditApplications_Kind","[SettlementKind] IN (1,2)");
                t.HasCheckConstraint("CK_CustomerCreditApplications_Status","[Status] IN (1,2)");
                t.HasCheckConstraint("CK_CustomerCreditApplications_TargetShape","([SettlementKind]=1 AND [TargetSalesInvoiceId] IS NOT NULL AND [RefundPaymentVoucherLineId] IS NULL AND [TargetBaseAmount]>0 AND [TargetInvoiceCurrencyAmount]>0 AND [RefundSettlementBaseAmount] IS NULL) OR ([SettlementKind]=2 AND [TargetSalesInvoiceId] IS NULL AND [RefundPaymentVoucherLineId] IS NOT NULL AND [TargetBaseAmount] IS NULL AND [TargetInvoiceCurrencyAmount] IS NULL AND [RefundSettlementBaseAmount]>0)");
                t.HasCheckConstraint("CK_CustomerCreditApplications_ReversalState","([Status]=1 AND [ReversedAtUtc] IS NULL AND [ReversedBy] IS NULL AND [ReversalReason] IS NULL) OR ([Status]=2 AND [ReversedAtUtc] IS NOT NULL AND [ReversedBy] IS NOT NULL AND [ReversalReason] IS NOT NULL)");
            });
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerCreditApplication",b=>
        {
            b.HasOne("OAS.Domain.Sales.Entities.SalesReturn",null).WithMany()
                .HasForeignKey("SalesReturnId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoice",null).WithMany()
                .HasForeignKey("TargetSalesInvoiceId").OnDelete(DeleteBehavior.Restrict);
            b.HasOne("OAS.Domain.Accounting.Entities.Customer",null).WithMany()
                .HasForeignKey("CustomerId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne("OAS.Domain.Accounting.Entities.PaymentVoucherLine",null).WithMany()
                .HasForeignKey("RefundPaymentVoucherLineId").OnDelete(DeleteBehavior.Restrict);
            b.HasOne("OAS.Domain.Accounting.Entities.ReceiptVoucherLine",null).WithMany()
                .HasForeignKey("ReversalReceiptVoucherLineId").OnDelete(DeleteBehavior.Restrict);
        });
    }
}
