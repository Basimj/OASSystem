using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class SalesInvoiceLineConfiguration : IEntityTypeConfiguration<SalesInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLine> builder)
    {
        builder.ToTable("tbl_SalesInvoiceLines", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SalesInvoiceLines_Quantity_Positive", "[Quantity] > 0");
            t.HasCheckConstraint("CK_SalesInvoiceLines_BaseUnitPrice_NonNegative", "[BaseUnitPrice] >= 0");
            t.HasCheckConstraint("CK_SalesInvoiceLines_ActualUnitPrice_NonNegative", "[ActualUnitPrice] >= 0");
            t.HasCheckConstraint("CK_SalesInvoiceLines_DiscountAmount_NonNegative", "[DiscountAmount] >= 0");
            t.HasCheckConstraint("CK_SalesInvoiceLines_DiscountWithinGross", "[DiscountAmount] <= ([Quantity] * [ActualUnitPrice])");
            t.HasCheckConstraint("CK_SalesInvoiceLines_TaxAmount_NonNegative", "[TaxAmount] >= 0");
            t.HasCheckConstraint("CK_SalesInvoiceLines_NetAmount_NonNegative", "[NetAmount] >= 0");
            t.HasCheckConstraint("CK_SalesInvoiceLines_FinalAmount_NonNegative", "[FinalAmount] >= 0");
            t.HasCheckConstraint("CK_SalesInvoiceLines_BaseAmounts_NonNegative", "[BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseFinalAmount] >= 0");
            t.HasCheckConstraint("CK_SalesInvoiceLines_CostSnapshots_NonNegative", "([UnitCostSnapshot] IS NULL OR [UnitCostSnapshot] >= 0) AND ([TotalCostSnapshot] IS NULL OR [TotalCostSnapshot] >= 0)");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SalesInvoiceId).IsRequired();
        builder.Property(x => x.LineNumber).IsRequired();
        builder.Property(x => x.CustomerOrderLineId);
        builder.Property(x => x.GroupId);
        builder.Property(x => x.LineType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.ProductVariantId);
        builder.Property(x => x.WarehouseId);
        builder.Property(x => x.ProductCodeSnapshot).HasMaxLength(100);
        builder.Property(x => x.ProductNameSnapshot).IsRequired().HasMaxLength(250);
        builder.Property(x => x.DescriptionSnapshot).IsRequired().HasMaxLength(500);
        builder.Property(x => x.UnitSnapshot).HasMaxLength(100);
        builder.Property(x => x.Quantity).IsRequired().HasPrecision(18, 3);
        builder.Property(x => x.BaseUnitPrice).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.ActualUnitPrice).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.DiscountType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.DiscountValue).HasPrecision(19, 4);
        builder.Property(x => x.DiscountAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.TaxRate).HasPrecision(9, 4);
        builder.Property(x => x.TaxAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.NetAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.FinalAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.BaseNetAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.BaseTaxAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.BaseFinalAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.UnitCostSnapshot).HasPrecision(19, 4);
        builder.Property(x => x.TotalCostSnapshot).HasPrecision(19, 4);
        builder.Property(x => x.PrescriptionRevisionId);
        builder.Property(x => x.PrescriptionEye).HasConversion<byte>();
        builder.Property(x => x.RequiresProduction).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.SalesInvoiceId, x.LineNumber })
            .IsUnique().HasDatabaseName("UX_SalesInvoiceLines_Invoice_LineNumber");
        builder.HasIndex(x => x.SalesInvoiceId).HasDatabaseName("IX_SalesInvoiceLines_SalesInvoiceId");
        builder.HasIndex(x => x.ProductVariantId).HasDatabaseName("IX_SalesInvoiceLines_ProductVariantId");
        builder.HasIndex(x => x.WarehouseId).HasDatabaseName("IX_SalesInvoiceLines_WarehouseId");
        builder.HasIndex(x => x.CustomerOrderLineId).HasDatabaseName("IX_SalesInvoiceLines_CustomerOrderLineId");
        builder.HasIndex(x => x.PrescriptionRevisionId).HasDatabaseName("IX_SalesInvoiceLines_PrescriptionRevisionId");
        builder.HasIndex(x => x.GroupId).HasDatabaseName("IX_SalesInvoiceLines_GroupId");

        builder.HasOne<CustomerOrderLine>().WithMany().HasForeignKey(x => x.CustomerOrderLineId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoiceLines_CustomerOrderLines_CustomerOrderLineId");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoiceLines_ProductVariants_ProductVariantId");
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoiceLines_Warehouses_WarehouseId");
        builder.HasOne<PrescriptionRevision>().WithMany().HasForeignKey(x => x.PrescriptionRevisionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoiceLines_PrescriptionRevisions_PrescriptionRevisionId");
    }
}
