using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class BankStatementLineConfiguration : IEntityTypeConfiguration<BankStatementLine>
{
    public void Configure(EntityTypeBuilder<BankStatementLine> builder)
    {
        builder.ToTable("tbl_BankStatementLines", "dbo", t =>
        {
            t.HasCheckConstraint("CK_BankStatementLines_Debit_NonNegative", "[DebitAmount] >= 0");
            t.HasCheckConstraint("CK_BankStatementLines_Credit_NonNegative", "[CreditAmount] >= 0");
            t.HasCheckConstraint("CK_BankStatementLines_OneSidedAmount", "NOT ([DebitAmount] > 0 AND [CreditAmount] > 0)");
            t.HasCheckConstraint("CK_BankStatementLines_NonZeroAmount", "[DebitAmount] > 0 OR [CreditAmount] > 0");
        });

        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BankStatementId).IsRequired();
        builder.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.ValueDate).HasColumnType("date");
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.DebitAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.CreditAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.RunningBalance).HasPrecision(19, 4);
        builder.Property(x => x.ExternalTransactionId).HasMaxLength(100);
        builder.Property(x => x.MatchStatus).IsRequired().HasConversion<byte>();
        builder.Property(x => x.IsIgnored).IsRequired();
        builder.Property(x => x.IgnoreReason).HasMaxLength(500);
        builder.Property(x => x.IgnoredAtUtc).HasColumnType("datetimeoffset(0)");
        builder.Property(x => x.IgnoredBy);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.Ignore(x => x.StatementAmount);

        builder.HasIndex(x => new { x.BankStatementId, x.TransactionDate })
            .HasDatabaseName("IX_BankStatementLines_Statement_TransactionDate");
        builder.HasIndex(x => new { x.BankStatementId, x.MatchStatus })
            .HasDatabaseName("IX_BankStatementLines_Statement_MatchStatus");
        builder.HasIndex(x => new { x.BankStatementId, x.ExternalTransactionId })
            .HasFilter("[ExternalTransactionId] IS NOT NULL")
            .HasDatabaseName("IX_BankStatementLines_Statement_ExternalTransactionId");
    }
}
