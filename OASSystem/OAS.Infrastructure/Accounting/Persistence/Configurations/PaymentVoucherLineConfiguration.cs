using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class PaymentVoucherLineConfiguration : IEntityTypeConfiguration<PaymentVoucherLine>
{
    public void Configure(EntityTypeBuilder<PaymentVoucherLine> builder)
    {
        builder.ToTable("tbl_PaymentVoucherLines", "dbo", t =>
        {
            t.HasCheckConstraint("CK_PaymentVoucherLines_Amount_Positive", "[Amount] > 0");
            t.HasCheckConstraint("CK_PaymentVoucherLines_ExchangeRate_Positive", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
            t.HasCheckConstraint("CK_PaymentVoucherLines_BaseAmount_Positive", "[BaseAmount] IS NULL OR [BaseAmount] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PaymentVoucherId).IsRequired();
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

        builder.HasIndex(x => new { x.PaymentVoucherId, x.LineNumber }).IsUnique().HasDatabaseName("UX_PaymentVoucherLines_Voucher_LineNumber");
        builder.HasIndex(x => x.AccountId).HasDatabaseName("IX_PaymentVoucherLines_AccountId");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_PaymentVoucherLines_CustomerId");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("IX_PaymentVoucherLines_SupplierId");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_PaymentVoucherLines_EmployeeId");
        builder.HasIndex(x => x.CounterpartyAccountId).HasDatabaseName("IX_PaymentVoucherLines_CounterpartyAccountId");
        builder.HasIndex(x => x.SettlementAccountId).HasDatabaseName("IX_PaymentVoucherLines_SettlementAccountId");
        builder.HasIndex(x => x.CurrencyId).HasDatabaseName("IX_PaymentVoucherLines_CurrencyId");

        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentVoucherLines_Account");
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.CounterpartyAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentVoucherLines_CounterpartyAccount");
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.SettlementAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentVoucherLines_SettlementAccount");
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentVoucherLines_Customer");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentVoucherLines_Supplier");
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentVoucherLines_Employee");
        builder.HasOne<CashAccount>().WithMany().HasForeignKey(x => x.CashAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentVoucherLines_CashAccount");
        builder.HasOne<BankAccount>().WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentVoucherLines_BankAccount");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentVoucherLines_Currency");
    }
}
