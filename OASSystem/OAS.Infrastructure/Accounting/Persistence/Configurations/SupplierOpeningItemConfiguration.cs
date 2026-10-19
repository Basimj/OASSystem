using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class SupplierOpeningItemConfiguration : IEntityTypeConfiguration<SupplierOpeningItem>
{
    public void Configure(EntityTypeBuilder<SupplierOpeningItem> builder)
    {
        builder.ToTable("tbl_SupplierOpeningItems", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SupplierOpeningItems_OriginalOpenAmount_Positive", "[OriginalOpenAmount] > 0");
            t.HasCheckConstraint("CK_SupplierOpeningItems_ExchangeRateSnapshot_Positive", "[ExchangeRateSnapshot] > 0");
            t.HasCheckConstraint("CK_SupplierOpeningItems_BaseOpenAmount_Positive", "[BaseOpenAmount] > 0");
            t.HasCheckConstraint("CK_SupplierOpeningItems_CurrencyDecimals", "[CurrencyDecimalPlacesSnapshot] BETWEEN 0 AND 6");
            t.HasCheckConstraint("CK_SupplierOpeningItems_DueDateProvenance", "[DueDateProvenance] BETWEEN 0 AND 2");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BatchId).IsRequired();
        builder.Property(x => x.SupplierId).IsRequired();
        builder.Property(x => x.LegacyReference).HasMaxLength(100);
        builder.Property(x => x.DocumentDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.DueDate).HasColumnType("date");
        builder.Property(x => x.DueDateProvenance).HasConversion<byte>().IsRequired();
        builder.Property(x => x.CurrencyId).IsRequired();
        builder.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(8).IsRequired();
        builder.Property(x => x.CurrencyDecimalPlacesSnapshot).IsRequired();
        builder.Property(x => x.OriginalOpenAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.ExchangeRateSnapshot).HasPrecision(19,8).IsRequired();
        builder.Property(x => x.BaseOpenAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        // Match the existing migration and snapshot. EF's convention uses
        // IX_tbl_SupplierOpeningItems_BatchId, which creates a false pending rename.
        builder.HasIndex(x => x.BatchId)
            .HasDatabaseName("IX_SupplierOpeningItems_BatchId");
        builder.HasIndex(x => new { x.BatchId, x.SupplierId, x.LegacyReference })
            .IsUnique().HasFilter("[LegacyReference] IS NOT NULL")
            .HasDatabaseName("UX_SupplierOpeningItems_Batch_Supplier_LegacyReference");
        builder.HasIndex(x => new { x.SupplierId, x.DueDate })
            .HasDatabaseName("IX_SupplierOpeningItems_Supplier_DueDate");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningItems_Suppliers_SupplierId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningItems_Currencies_CurrencyId");
    }
}
