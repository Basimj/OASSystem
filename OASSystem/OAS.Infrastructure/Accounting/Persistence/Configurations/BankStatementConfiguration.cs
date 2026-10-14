using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class BankStatementConfiguration : IEntityTypeConfiguration<BankStatement>
{
    public void Configure(EntityTypeBuilder<BankStatement> builder)
    {
        builder.ToTable("tbl_BankStatements", "dbo", t =>
        {
            t.HasCheckConstraint("CK_BankStatements_Period", "[FromDate] <= [ToDate]");
        });

        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BankAccountId).IsRequired();
        builder.Property(x => x.StatementNumber).HasMaxLength(50);
        builder.Property(x => x.FromDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.ToDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.OpeningBalance).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.ClosingBalance).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.CurrencyId).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>();
        builder.Property(x => x.SourceType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.ImportedFileName).HasMaxLength(260);
        builder.Property(x => x.ImportedFileHash).HasMaxLength(128);
        builder.Property(x => x.ImportedAtUtc).HasColumnType("datetimeoffset(0)");
        builder.Property(x => x.ImportedBy);
        builder.Property(x => x.ReviewedAtUtc).HasColumnType("datetimeoffset(0)");
        builder.Property(x => x.ReviewedBy);
        builder.Property(x => x.ReconciledAtUtc).HasColumnType("datetimeoffset(0)");
        builder.Property(x => x.ReconciledBy);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.BankAccountId, x.FromDate, x.ToDate })
            .HasDatabaseName("IX_BankStatements_BankAccountId_FromDate_ToDate");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_BankStatements_Status");
        builder.HasIndex(x => new { x.BankAccountId, x.StatementNumber })
            .HasDatabaseName("IX_BankStatements_BankAccountId_StatementNumber");
        builder.HasIndex(x => x.ImportedFileHash)
            .IsUnique()
            .HasFilter("[ImportedFileHash] IS NOT NULL AND [Status] <> 5")
            .HasDatabaseName("UX_BankStatements_ImportedFileHash_Active");

        builder.HasOne<BankAccount>()
            .WithMany()
            .HasForeignKey(x => x.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_BankStatements_BankAccount");

        builder.HasOne<Currency>()
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_BankStatements_Currency");

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.BankStatementId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_BankStatementLines_BankStatement");

        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
