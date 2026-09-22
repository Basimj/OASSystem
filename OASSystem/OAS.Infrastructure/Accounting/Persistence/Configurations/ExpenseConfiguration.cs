using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("tbl_Expenses", "accounting");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ExpenseNumber)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.ExpenseDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.ExpenseTypeId)
            .IsRequired();

        builder.Property(x => x.ExpenseAccountId)
            .IsRequired();

        builder.Property(x => x.Beneficiary)
            .HasMaxLength(200);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(x => x.PaymentMethod)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.CashAccountId);

        builder.Property(x => x.BankAccountId);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.JournalEntryId);
        builder.Property(x => x.PostedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.ExpenseNumber)
            .IsUnique()
            .HasDatabaseName("UX_Expenses_ExpenseNumber");

        builder.HasIndex(x => x.ExpenseDate)
            .HasDatabaseName("IX_Expenses_ExpenseDate");

        builder.HasIndex(x => x.ExpenseTypeId)
            .HasDatabaseName("IX_Expenses_ExpenseTypeId");

        builder.HasIndex(x => x.ExpenseAccountId)
            .HasDatabaseName("IX_Expenses_ExpenseAccountId");

        builder.HasIndex(x => x.CashAccountId)
            .HasDatabaseName("IX_Expenses_CashAccountId");

        builder.HasIndex(x => x.BankAccountId)
            .HasDatabaseName("IX_Expenses_BankAccountId");

        builder.HasIndex(x => x.JournalEntryId)
            .HasDatabaseName("IX_Expenses_JournalEntryId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Expenses_Status");

        builder.HasOne<ExpenseType>()
            .WithMany()
            .HasForeignKey(x => x.ExpenseTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Expenses_ExpenseType");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.ExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Expenses_ExpenseAccount");

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(x => x.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Expenses_CashAccount");

        builder.HasOne<BankAccount>()
            .WithMany()
            .HasForeignKey(x => x.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Expenses_BankAccount");

        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Expenses_JournalEntry");
    }
}
