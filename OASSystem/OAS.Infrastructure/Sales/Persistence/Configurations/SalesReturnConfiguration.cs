using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("tbl_SalesReturns", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SalesReturns_ExchangeRate_Positive", "[ExchangeRate] > 0");
            t.HasCheckConstraint("CK_SalesReturns_Amounts_NonNegative", "[NetAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0 AND [BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ReturnCode).IsRequired().HasMaxLength(40);
        builder.Property(x => x.SalesInvoiceId).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.ReturnDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.PostingDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.CurrencyId).IsRequired();
        builder.Property(x => x.CurrencyCodeSnapshot).IsRequired().HasMaxLength(10);
        builder.Property(x => x.CurrencyDecimalPlacesSnapshot).IsRequired();
        builder.Property(x => x.ExchangeRate).HasPrecision(19, 8).IsRequired();
        builder.Property(x => x.ExchangeRateDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.ExchangeRateType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.ExchangeRateSource).HasConversion<byte>().IsRequired();
        builder.Property(x => x.BaseCurrencyId).IsRequired();
        builder.Property(x => x.BaseCurrencyCodeSnapshot).IsRequired().HasMaxLength(10);
        builder.Property(x => x.BaseCurrencyDecimalPlacesSnapshot).IsRequired();
        builder.Property(x => x.NetAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.TaxAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.BaseNetAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.BaseTaxAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.BaseTotalAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.JournalEntryId);
        builder.Property(x => x.ConfirmedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.ConfirmedBy).HasMaxLength(64);
        builder.Property(x => x.PostedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.PostedBy).HasMaxLength(64);
        builder.Property(x => x.CancelledAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.ReturnCode).IsUnique().HasDatabaseName("UX_SalesReturns_ReturnCode");
        builder.HasIndex(x => x.SalesInvoiceId).HasDatabaseName("IX_SalesReturns_SalesInvoiceId");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_SalesReturns_CustomerId");
        builder.HasIndex(x => x.PostingDate).HasDatabaseName("IX_SalesReturns_PostingDate");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_SalesReturns_Status");
        builder.HasIndex(x => x.JournalEntryId).HasDatabaseName("IX_SalesReturns_JournalEntryId");

        builder.HasOne<SalesInvoice>().WithMany().HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturns_SalesInvoices_SalesInvoiceId");
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturns_Customers_CustomerId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturns_Currencies_CurrencyId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.BaseCurrencyId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturns_BaseCurrencies_BaseCurrencyId");
        builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturns_JournalEntries_JournalEntryId");
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturnLines_SalesReturns_SalesReturnId");
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
