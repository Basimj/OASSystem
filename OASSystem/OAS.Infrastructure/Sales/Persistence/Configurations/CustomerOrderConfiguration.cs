using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class CustomerOrderConfiguration : IEntityTypeConfiguration<CustomerOrder>
{
    public void Configure(EntityTypeBuilder<CustomerOrder> builder)
    {
        builder.ToTable("tbl_CustomerOrders", "dbo", t =>
        {
            t.HasCheckConstraint("CK_CustomerOrders_ExchangeRate_Positive", "[ExchangeRate] > 0");
            t.HasCheckConstraint("CK_CustomerOrders_PaymentTermDays_NonNegative", "[PaymentTermDaysSnapshot] >= 0");
            t.HasCheckConstraint("CK_CustomerOrders_Totals_NonNegative", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OrderCode).IsRequired().HasMaxLength(40);
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.PrescriptionRevisionId);
        builder.Property(x => x.OrderDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.RequiredDate).HasColumnType("date");
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>();
        builder.Property(x => x.CurrencyId).IsRequired();
        builder.Property(x => x.CurrencyCodeSnapshot).IsRequired().HasMaxLength(10);
        builder.Property(x => x.CurrencySymbolSnapshot).HasMaxLength(10);
        builder.Property(x => x.CurrencyDecimalPlacesSnapshot).IsRequired();
        builder.Property(x => x.ExchangeRate).IsRequired().HasPrecision(19, 8);
        builder.Property(x => x.ExchangeRateDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.ExchangeRateType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.ExchangeRateSource).IsRequired().HasConversion<byte>();
        builder.Property(x => x.TaxCalculationMode).IsRequired().HasConversion<byte>();
        builder.Property(x => x.PaymentPlan).IsRequired().HasConversion<byte>();
        builder.Property(x => x.PaymentTermType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.PaymentTermDaysSnapshot).IsRequired();
        builder.Property(x => x.Subtotal).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.DiscountAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.TaxAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.TotalAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.ConfirmedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.ConfirmedBy).HasMaxLength(64);
        builder.Property(x => x.CancelledAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.OrderCode).IsUnique().HasDatabaseName("UX_CustomerOrders_OrderCode");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_CustomerOrders_CustomerId");
        builder.HasIndex(x => x.OrderDate).HasDatabaseName("IX_CustomerOrders_OrderDate");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_CustomerOrders_Status");
        builder.HasIndex(x => x.PrescriptionRevisionId).HasDatabaseName("IX_CustomerOrders_PrescriptionRevisionId");
        builder.HasIndex(x => x.CurrencyId).HasDatabaseName("IX_CustomerOrders_CurrencyId");
        builder.HasIndex(x => x.PaymentPlan).HasDatabaseName("IX_CustomerOrders_PaymentPlan");

        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerOrders_Customers_CustomerId");
        builder.HasOne<PrescriptionRevision>().WithMany().HasForeignKey(x => x.PrescriptionRevisionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerOrders_PrescriptionRevisions_PrescriptionRevisionId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerOrders_Currencies_CurrencyId");

        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.CustomerOrderId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerOrderLines_Orders_CustomerOrderId");
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
