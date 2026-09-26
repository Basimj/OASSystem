using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("tbl_JournalEntryLines", "dbo", t =>
        {
            t.HasCheckConstraint("CK_JournalEntryLines_BaseDirection", "([DebitAmount] > 0 AND [CreditAmount] = 0) OR ([CreditAmount] > 0 AND [DebitAmount] = 0)");
            t.HasCheckConstraint("CK_JournalEntryLines_ExchangeRate", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
        });

        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.JournalEntryId).IsRequired();
        builder.Property(x => x.LineNumber).IsRequired();
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.DebitAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.CreditAmount).IsRequired().HasPrecision(19, 4);

        builder.Property(x => x.TransactionCurrencyCodeSnapshot).HasMaxLength(8);
        builder.Property(x => x.TransactionDebitAmount).HasPrecision(19, 4);
        builder.Property(x => x.TransactionCreditAmount).HasPrecision(19, 4);
        builder.Property(x => x.ExchangeRate).HasPrecision(19, 8);
        builder.Property(x => x.ExchangeRateDate).HasColumnType("date");
        builder.Property(x => x.PartyNameSnapshot).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(300);

        builder.HasIndex(x => new { x.JournalEntryId, x.LineNumber }).IsUnique().HasDatabaseName("UX_JournalEntryLines_Journal_LineNumber");
        builder.HasIndex(x => x.AccountId).HasDatabaseName("IX_JournalEntryLines_AccountId");
        builder.HasIndex(x => x.TransactionCurrencyId).HasDatabaseName("IX_JournalEntryLines_TransactionCurrencyId");
        builder.HasIndex(x => x.CostCenterId).HasDatabaseName("IX_JournalEntryLines_CostCenterId");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_JournalEntryLines_CustomerId");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("IX_JournalEntryLines_SupplierId");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_JournalEntryLines_EmployeeId");
        builder.HasIndex(x => x.SourceDocumentLineId).HasDatabaseName("IX_JournalEntryLines_SourceDocumentLineId");

        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_JournalEntryLines_Account");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.TransactionCurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_JournalEntryLines_TransactionCurrency");
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_JournalEntryLines_Customer");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_JournalEntryLines_Supplier");
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_JournalEntryLines_Employee");
        builder.HasOne<CostCenter>().WithMany().HasForeignKey(x => x.CostCenterId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_JournalEntryLines_CostCenter");
    }
}
