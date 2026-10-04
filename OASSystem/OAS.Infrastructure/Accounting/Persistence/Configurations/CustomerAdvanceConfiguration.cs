using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class CustomerAdvanceConfiguration : IEntityTypeConfiguration<CustomerAdvance>
{
    public void Configure(EntityTypeBuilder<CustomerAdvance> builder)
    {
        builder.ToTable("tbl_CustomerAdvances", "dbo", table =>
        {
            table.HasCheckConstraint("CK_CustomerAdvances_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_CustomerAdvances_BaseAmount", "[BaseAmount] > 0");
            table.HasCheckConstraint("CK_CustomerAdvances_ExchangeRate", "[ExchangeRate] > 0");
            table.HasCheckConstraint("CK_CustomerAdvances_AppliedAmount", "[AppliedAmount] >= 0 AND [AppliedAmount] <= [Amount]");
            table.HasCheckConstraint("CK_CustomerAdvances_BaseAppliedAmount", "[BaseAppliedAmount] >= 0 AND [BaseAppliedAmount] <= [BaseAmount]");
        });

        builder.HasKey(x => x.Id).HasName("PK_tbl_CustomerAdvances");
        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AdvanceNumber).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.CustomerOrderId).IsRequired();
        builder.Property(x => x.ReceiptVoucherLineId).IsRequired();
        builder.Property(x => x.CurrencyId).IsRequired();
        builder.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(8).IsRequired();
        builder.Property(x => x.CurrencySymbolSnapshot).HasMaxLength(12);
        builder.Property(x => x.CurrencyDecimalPlacesSnapshot).IsRequired();
        builder.Property(x => x.BaseCurrencyId).IsRequired();
        builder.Property(x => x.BaseCurrencyCodeSnapshot).HasMaxLength(8).IsRequired();
        builder.Property(x => x.BaseCurrencyDecimalPlacesSnapshot).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.ExchangeRate).HasPrecision(19, 8).IsRequired();
        builder.Property(x => x.ExchangeRateDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.ExchangeRateType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.ExchangeRateSource).HasConversion<byte>().IsRequired();
        builder.Property(x => x.BaseAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.AppliedAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.BaseAppliedAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.ReceivedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.AdvanceNumber).IsUnique().HasDatabaseName("UX_CustomerAdvances_AdvanceNumber");
        builder.HasIndex(x => x.ReceiptVoucherLineId).IsUnique().HasDatabaseName("UX_CustomerAdvances_ReceiptVoucherLineId");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_CustomerAdvances_CustomerId");
        builder.HasIndex(x => x.CustomerOrderId).HasDatabaseName("IX_CustomerAdvances_CustomerOrderId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_CustomerAdvances_Status");
        builder.HasIndex(x => x.CurrencyId).HasDatabaseName("IX_CustomerAdvances_CurrencyId");
        builder.HasIndex(x => x.BaseCurrencyId).HasDatabaseName("IX_CustomerAdvances_BaseCurrencyId");

        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerAdvances_Customers_CustomerId");
        builder.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.CustomerOrderId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerAdvances_CustomerOrders_CustomerOrderId");
        builder.HasOne<ReceiptVoucherLine>().WithMany().HasForeignKey(x => x.ReceiptVoucherLineId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerAdvances_ReceiptVoucherLines_ReceiptVoucherLineId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerAdvances_Currencies_CurrencyId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.BaseCurrencyId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerAdvances_BaseCurrencies_BaseCurrencyId");
    }
}
