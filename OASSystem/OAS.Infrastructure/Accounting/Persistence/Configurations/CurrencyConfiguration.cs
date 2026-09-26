using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("tbl_Currencies", "dbo");
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(8);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameEn).HasMaxLength(100);
        builder.Property(x => x.Symbol).HasMaxLength(12);
        builder.Property(x => x.DecimalPlaces).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Currencies_Code");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_Currencies_IsActive");
        builder.ToTable(t => t.HasCheckConstraint("CK_Currencies_DecimalPlaces", "[DecimalPlaces] BETWEEN 0 AND 6"));
    }
}
