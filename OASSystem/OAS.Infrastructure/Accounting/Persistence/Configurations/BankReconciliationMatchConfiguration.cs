using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class BankReconciliationMatchConfiguration : IEntityTypeConfiguration<BankReconciliationMatch>
{
    public void Configure(EntityTypeBuilder<BankReconciliationMatch> builder)
    {
        builder.ToTable("tbl_BankReconciliationMatches", "dbo", t =>
        {
            t.HasCheckConstraint("CK_BankReconciliationMatches_MatchedAmount_Positive", "[MatchedAmount] > 0");
            t.HasCheckConstraint(
                "CK_BankReconciliationMatches_ExactlyOneSource",
                "(([MatchType] = 1 AND [JournalEntryLineId] IS NOT NULL AND [ReceiptVoucherLineId] IS NULL AND [PaymentVoucherLineId] IS NULL) OR " +
                "([MatchType] = 2 AND [JournalEntryLineId] IS NULL AND [ReceiptVoucherLineId] IS NOT NULL AND [PaymentVoucherLineId] IS NULL) OR " +
                "([MatchType] = 3 AND [JournalEntryLineId] IS NULL AND [ReceiptVoucherLineId] IS NULL AND [PaymentVoucherLineId] IS NOT NULL))");
        });

        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BankReconciliationId).IsRequired();
        builder.Property(x => x.BankStatementLineId).IsRequired();
        builder.Property(x => x.JournalEntryLineId);
        builder.Property(x => x.ReceiptVoucherLineId);
        builder.Property(x => x.PaymentVoucherLineId);
        builder.Property(x => x.MatchedAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.MatchType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.MatchMethod).IsRequired().HasConversion<byte>();
        builder.Property(x => x.IsVoided).IsRequired();
        builder.Property(x => x.VoidedAtUtc).HasColumnType("datetimeoffset(0)");
        builder.Property(x => x.VoidedBy);
        builder.Property(x => x.VoidReason).HasMaxLength(500);

        builder.HasIndex(x => new { x.BankReconciliationId, x.BankStatementLineId })
            .HasDatabaseName("IX_BankReconciliationMatches_Reconciliation_StatementLine");
        builder.HasIndex(x => x.JournalEntryLineId)
            .HasFilter("[JournalEntryLineId] IS NOT NULL AND [IsVoided] = 0")
            .HasDatabaseName("IX_BankReconciliationMatches_JournalEntryLine_Active");
        builder.HasIndex(x => x.ReceiptVoucherLineId)
            .HasFilter("[ReceiptVoucherLineId] IS NOT NULL AND [IsVoided] = 0")
            .HasDatabaseName("IX_BankReconciliationMatches_ReceiptVoucherLine_Active");
        builder.HasIndex(x => x.PaymentVoucherLineId)
            .HasFilter("[PaymentVoucherLineId] IS NOT NULL AND [IsVoided] = 0")
            .HasDatabaseName("IX_BankReconciliationMatches_PaymentVoucherLine_Active");

        builder.HasOne<BankStatementLine>()
            .WithMany()
            .HasForeignKey(x => x.BankStatementLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_BankReconciliationMatches_BankStatementLine");

        builder.HasOne<JournalEntryLine>()
            .WithMany()
            .HasForeignKey(x => x.JournalEntryLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_BankReconciliationMatches_JournalEntryLine");

        builder.HasOne<ReceiptVoucherLine>()
            .WithMany()
            .HasForeignKey(x => x.ReceiptVoucherLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_BankReconciliationMatches_ReceiptVoucherLine");

        builder.HasOne<PaymentVoucherLine>()
            .WithMany()
            .HasForeignKey(x => x.PaymentVoucherLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_BankReconciliationMatches_PaymentVoucherLine");
    }
}
