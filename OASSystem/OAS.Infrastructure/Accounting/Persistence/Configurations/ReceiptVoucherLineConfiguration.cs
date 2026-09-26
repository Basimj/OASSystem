using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class ReceiptVoucherLineConfiguration : IEntityTypeConfiguration<ReceiptVoucherLine>
{
    public void Configure(EntityTypeBuilder<ReceiptVoucherLine> builder)
    {
        builder.ToTable("tbl_ReceiptVoucherLines", "dbo", t =>
        {
            t.HasCheckConstraint("CK_ReceiptVoucherLines_Amount_Positive", "[Amount] > 0");
            t.HasCheckConstraint("CK_ReceiptVoucherLines_ExchangeRate_Positive", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
            t.HasCheckConstraint("CK_ReceiptVoucherLines_BaseAmount_Positive", "[BaseAmount] IS NULL OR [BaseAmount] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ReceiptVoucherId).IsRequired();
        builder.Property(x => x.LineNumber).IsRequired();
        builder.Property(x => x.AccountId).IsRequired(); // legacy during Expand
        builder.Property(x => x.Amount).IsRequired().HasPrecision(19, 4);

        builder.Property(x => x.PartyNameSnapshot).HasMaxLength(200);
        builder.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(8);
        builder.Property(x => x.CurrencySymbolSnapshot).HasMaxLength(12);
        builder.Property(x => x.ExchangeRate).HasPrecision(19, 8);
        builder.Property(x => x.ExchangeRateDate).HasColumnType("date");
        builder.Property(x => x.BaseAmount).HasPrecision(19, 4);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.ReferenceDate).HasColumnType("date");
        builder.Property(x => x.ReferenceType).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(300);

        builder.HasIndex(x => new { x.ReceiptVoucherId, x.LineNumber }).IsUnique().HasDatabaseName("UX_ReceiptVoucherLines_Voucher_LineNumber");
        builder.HasIndex(x => x.AccountId).HasDatabaseName("IX_ReceiptVoucherLines_AccountId");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_ReceiptVoucherLines_CustomerId");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("IX_ReceiptVoucherLines_SupplierId");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_ReceiptVoucherLines_EmployeeId");
        builder.HasIndex(x => x.CounterpartyAccountId).HasDatabaseName("IX_ReceiptVoucherLines_CounterpartyAccountId");
        builder.HasIndex(x => x.SettlementAccountId).HasDatabaseName("IX_ReceiptVoucherLines_SettlementAccountId");
        builder.HasIndex(x => x.CurrencyId).HasDatabaseName("IX_ReceiptVoucherLines_CurrencyId");

        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ReceiptVoucherLines_Account");
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.CounterpartyAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ReceiptVoucherLines_CounterpartyAccount");
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.SettlementAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ReceiptVoucherLines_SettlementAccount");
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ReceiptVoucherLines_Customer");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ReceiptVoucherLines_Supplier");
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ReceiptVoucherLines_Employee");
        builder.HasOne<CashAccount>().WithMany().HasForeignKey(x => x.CashAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ReceiptVoucherLines_CashAccount");
        builder.HasOne<BankAccount>().WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ReceiptVoucherLines_BankAccount");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ReceiptVoucherLines_Currency");
    }
}
