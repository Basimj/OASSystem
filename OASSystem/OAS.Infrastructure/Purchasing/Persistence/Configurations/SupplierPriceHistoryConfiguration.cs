using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class SupplierPriceHistoryConfiguration : IEntityTypeConfiguration<SupplierPriceHistory>
{
    public void Configure(EntityTypeBuilder<SupplierPriceHistory> builder)
    {
        builder.ToTable("tbl_SupplierPriceHistory", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SupplierPriceHistory_UnitPrice_NonNegative", "[UnitPrice] >= 0");
            t.HasCheckConstraint("CK_SupplierPriceHistory_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });
        builder.HasKey(x => x.Id); builder.ConfigurePurchasingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SupplierCatalogItemId).IsRequired();
        builder.Property(x => x.CurrencyId).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnType("date").IsRequired();
        builder.Property(x => x.EffectiveTo).HasColumnType("date");
        builder.Property(x => x.IsCurrent).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.SupplierCatalogItemId).HasDatabaseName("IX_SupplierPriceHistory_CatalogItemId");
        builder.HasIndex(x => new { x.SupplierCatalogItemId, x.CurrencyId }).IsUnique().HasFilter("[IsCurrent] = 1").HasDatabaseName("UX_SupplierPriceHistory_Current_Catalog_Currency");
        builder.HasOne<SupplierCatalogItem>().WithMany().HasForeignKey(x => x.SupplierCatalogItemId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SupplierPriceHistory_CatalogItem");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SupplierPriceHistory_Currencies_CurrencyId");
    }
}
