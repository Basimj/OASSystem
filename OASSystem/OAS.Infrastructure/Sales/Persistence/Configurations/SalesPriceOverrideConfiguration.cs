using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class SalesPriceOverrideConfiguration : IEntityTypeConfiguration<SalesPriceOverride>
{
    public void Configure(EntityTypeBuilder<SalesPriceOverride> builder)
    {
        builder.ToTable("tbl_SalesPriceOverrides", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SalesPriceOverrides_Prices_NonNegative", "[OriginalPrice] >= 0 AND [OverridePrice] >= 0");
            t.HasCheckConstraint("CK_SalesPriceOverrides_Prices_Different", "[OriginalPrice] <> [OverridePrice]");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SalesInvoiceId).IsRequired();
        builder.Property(x => x.SalesInvoiceLineId).IsRequired();
        builder.Property(x => x.OriginalPrice).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.OverridePrice).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>();
        builder.Property(x => x.RequestedBy).IsRequired().HasMaxLength(64);
        builder.Property(x => x.RequestedAtUtc).IsRequired().HasColumnType("datetimeoffset");
        builder.Property(x => x.ApprovedBy).HasMaxLength(64);
        builder.Property(x => x.ApprovedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.SalesInvoiceId).HasDatabaseName("IX_SalesPriceOverrides_SalesInvoiceId");
        builder.HasIndex(x => x.SalesInvoiceLineId).HasDatabaseName("IX_SalesPriceOverrides_SalesInvoiceLineId");
        builder.HasIndex(x => new { x.SalesInvoiceLineId, x.Status }).HasDatabaseName("IX_SalesPriceOverrides_Line_Status");

        builder.HasOne<SalesInvoice>().WithMany().HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesPriceOverrides_Invoices_SalesInvoiceId");
        builder.HasOne<SalesInvoiceLine>().WithMany().HasForeignKey(x => x.SalesInvoiceLineId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesPriceOverrides_Lines_SalesInvoiceLineId");
    }
}
