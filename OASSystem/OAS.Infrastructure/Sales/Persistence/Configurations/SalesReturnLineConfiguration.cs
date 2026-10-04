using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> builder)
    {
        builder.ToTable("tbl_SalesReturnLines", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SalesReturnLines_Quantity_Positive", "[Quantity] > 0");
            t.HasCheckConstraint("CK_SalesReturnLines_Amounts_NonNegative", "[NetAmount] >= 0 AND [TaxAmount] >= 0 AND [FinalAmount] >= 0 AND [BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseFinalAmount] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SalesReturnId).IsRequired();
        builder.Property(x => x.LineNumber).IsRequired();
        builder.Property(x => x.SalesInvoiceLineId).IsRequired();
        builder.Property(x => x.LineType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.ProductVariantId);
        builder.Property(x => x.WarehouseId);
        builder.Property(x => x.ProductCodeSnapshot).HasMaxLength(64);
        builder.Property(x => x.ProductNameSnapshot).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Quantity).HasPrecision(19, 3).IsRequired();
        builder.Property(x => x.NetAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.TaxAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.FinalAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.BaseNetAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.BaseTaxAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.BaseFinalAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.UnitCostSnapshot).HasPrecision(19, 4);
        builder.Property(x => x.TotalCostSnapshot).HasPrecision(19, 4);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.SalesReturnId, x.LineNumber }).IsUnique().HasDatabaseName("UX_SalesReturnLines_Return_LineNumber");
        builder.HasIndex(x => x.SalesInvoiceLineId).HasDatabaseName("IX_SalesReturnLines_SalesInvoiceLineId");
        builder.HasOne<SalesInvoiceLine>().WithMany().HasForeignKey(x => x.SalesInvoiceLineId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturnLines_SalesInvoiceLines_SalesInvoiceLineId");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturnLines_ProductVariants_ProductVariantId");
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturnLines_Warehouses_WarehouseId");
    }
}
