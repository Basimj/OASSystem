using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class AccountingSettingsConfiguration : IEntityTypeConfiguration<AccountingSettings>
{
    public void Configure(EntityTypeBuilder<AccountingSettings> builder)
    {
        builder.ToTable("tbl_AccountingSettings", "dbo");
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BaseCurrencyId).IsRequired();
        builder.Property(x => x.DefaultExchangeRateType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.BaseCurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AccountingSettings_BaseCurrency");
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.EmployeeParentAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AccountingSettings_EmployeeParentAccount");
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.CashParentAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AccountingSettings_CashParentAccount");
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.BankParentAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AccountingSettings_BankParentAccount");
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.ExchangeGainAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AccountingSettings_ExchangeGainAccount");
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.ExchangeLossAccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AccountingSettings_ExchangeLossAccount");
    }
}
