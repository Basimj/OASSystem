using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("tbl_ExchangeRates", "dbo");
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CurrencyId).IsRequired();
        builder.Property(x => x.RateDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.Rate).IsRequired().HasPrecision(19, 8);
        builder.Property(x => x.RateType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.CurrencyId, x.RateDate, x.RateType }).IsUnique().HasDatabaseName("UX_ExchangeRates_Currency_Date_Type");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ExchangeRates_Currency");
        builder.ToTable(t => t.HasCheckConstraint("CK_ExchangeRates_Rate_Positive", "[Rate] > 0"));
    }
}
