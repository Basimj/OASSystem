using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("tbl_SalesInvoices", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SalesInvoices_ExchangeRate_Positive", "[ExchangeRate] > 0");
            t.HasCheckConstraint("CK_SalesInvoices_PaymentTermDays_NonNegative", "[PaymentTermDaysSnapshot] >= 0");
            t.HasCheckConstraint("CK_SalesInvoices_Totals_NonNegative", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0");
            t.HasCheckConstraint("CK_SalesInvoices_BaseTotals_NonNegative", "[BaseSubtotal] >= 0 AND [BaseDiscountAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.InvoiceCode).IsRequired().HasMaxLength(40);
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.CustomerOrderId);
        builder.Property(x => x.PrescriptionRevisionId);
        builder.Property(x => x.InvoiceDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.PostingDate).IsRequired().HasColumnType("date");
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
        builder.Property(x => x.PaymentTermType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.PaymentTermDaysSnapshot).IsRequired();
        builder.Property(x => x.DueDate).HasColumnType("date");
        builder.Property(x => x.BaseCurrencyId).IsRequired();
        builder.Property(x => x.BaseCurrencyCodeSnapshot).IsRequired().HasMaxLength(10);
        builder.Property(x => x.BaseCurrencyDecimalPlacesSnapshot).IsRequired();
        builder.Property(x => x.Subtotal).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.DiscountAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.TaxAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.TotalAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.BaseSubtotal).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.BaseDiscountAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.BaseTaxAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.BaseTotalAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.JournalEntryId);
        builder.Property(x => x.ConfirmedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.ConfirmedBy).HasMaxLength(64);
        builder.Property(x => x.PostedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.PostedBy).HasMaxLength(64);
        builder.Property(x => x.CancelledAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.InvoiceCode).IsUnique().HasDatabaseName("UX_SalesInvoices_InvoiceCode");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_SalesInvoices_CustomerId");
        builder.HasIndex(x => x.InvoiceDate).HasDatabaseName("IX_SalesInvoices_InvoiceDate");
        builder.HasIndex(x => x.PostingDate).HasDatabaseName("IX_SalesInvoices_PostingDate");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_SalesInvoices_Status");
        builder.HasIndex(x => x.CustomerOrderId).HasDatabaseName("IX_SalesInvoices_CustomerOrderId");
        builder.HasIndex(x => x.JournalEntryId).HasDatabaseName("IX_SalesInvoices_JournalEntryId");
        builder.HasIndex(x => x.PrescriptionRevisionId).HasDatabaseName("IX_SalesInvoices_PrescriptionRevisionId");
        builder.HasIndex(x => x.CurrencyId).HasDatabaseName("IX_SalesInvoices_CurrencyId");
        builder.HasIndex(x => x.BaseCurrencyId).HasDatabaseName("IX_SalesInvoices_BaseCurrencyId");

        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoices_Customers_CustomerId");
        builder.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.CustomerOrderId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoices_CustomerOrders_CustomerOrderId");
        builder.HasOne<PrescriptionRevision>().WithMany().HasForeignKey(x => x.PrescriptionRevisionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoices_PrescriptionRevisions_PrescriptionRevisionId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoices_Currencies_CurrencyId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.BaseCurrencyId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoices_BaseCurrencies_BaseCurrencyId");
        builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoices_JournalEntries_JournalEntryId");

        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoiceLines_Invoices_SalesInvoiceId");
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
