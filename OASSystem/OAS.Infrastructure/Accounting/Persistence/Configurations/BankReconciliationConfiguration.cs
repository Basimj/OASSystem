using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class BankReconciliationConfiguration : IEntityTypeConfiguration<BankReconciliation>
{
    public void Configure(EntityTypeBuilder<BankReconciliation> builder)
    {
        builder.ToTable("tbl_BankReconciliations", "dbo");
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BankAccountId).IsRequired();
        builder.Property(x => x.BankStatementId).IsRequired();
        builder.Property(x => x.ReconciliationDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.BookClosingBalance).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.StatementClosingBalance).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Difference).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>();
        builder.Property(x => x.SubmittedBy);
        builder.Property(x => x.SubmittedAtUtc).HasColumnType("datetimeoffset(0)");
        builder.Property(x => x.ApprovedBy);
        builder.Property(x => x.ApprovedAtUtc).HasColumnType("datetimeoffset(0)");
        builder.Property(x => x.ClosedBy);
        builder.Property(x => x.ClosedAtUtc).HasColumnType("datetimeoffset(0)");
        builder.Property(x => x.ReopenedBy);
        builder.Property(x => x.ReopenedAtUtc).HasColumnType("datetimeoffset(0)");
        builder.Property(x => x.ReopenReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.BankAccountId).HasDatabaseName("IX_BankReconciliations_BankAccountId");
        builder.HasIndex(x => x.BankStatementId)
            .IsUnique()
            .HasFilter("[Status] <> 5")
            .HasDatabaseName("UX_BankReconciliations_BankStatementId_Active");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_BankReconciliations_Status");
        builder.HasIndex(x => x.ReconciliationDate).HasDatabaseName("IX_BankReconciliations_ReconciliationDate");

        builder.HasOne<BankAccount>()
            .WithMany()
            .HasForeignKey(x => x.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_BankReconciliations_BankAccount");

        builder.HasOne<BankStatement>()
            .WithMany()
            .HasForeignKey(x => x.BankStatementId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_BankReconciliations_BankStatement");

        builder.HasMany(x => x.Matches)
            .WithOne()
            .HasForeignKey(x => x.BankReconciliationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_BankReconciliationMatches_BankReconciliation");

        builder.Navigation(x => x.Matches).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
