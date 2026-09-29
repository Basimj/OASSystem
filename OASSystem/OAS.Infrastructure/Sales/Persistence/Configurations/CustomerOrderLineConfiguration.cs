using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class CustomerOrderLineConfiguration : IEntityTypeConfiguration<CustomerOrderLine>
{
    public void Configure(EntityTypeBuilder<CustomerOrderLine> builder)
    {
        builder.ToTable("tbl_CustomerOrderLines", "dbo", t =>
        {
            t.HasCheckConstraint("CK_CustomerOrderLines_Quantity_Positive", "[Quantity] > 0");
            t.HasCheckConstraint("CK_CustomerOrderLines_BaseUnitPrice_NonNegative", "[BaseUnitPrice] >= 0");
            t.HasCheckConstraint("CK_CustomerOrderLines_ActualUnitPrice_NonNegative", "[ActualUnitPrice] >= 0");
            t.HasCheckConstraint("CK_CustomerOrderLines_DiscountAmount_NonNegative", "[DiscountAmount] >= 0");
            t.HasCheckConstraint("CK_CustomerOrderLines_DiscountWithinGross", "[DiscountAmount] <= ([Quantity] * [ActualUnitPrice])");
            t.HasCheckConstraint("CK_CustomerOrderLines_TaxAmount_NonNegative", "[TaxAmount] >= 0");
            t.HasCheckConstraint("CK_CustomerOrderLines_NetAmount_NonNegative", "[NetAmount] >= 0");
            t.HasCheckConstraint("CK_CustomerOrderLines_FinalAmount_NonNegative", "[FinalAmount] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CustomerOrderId).IsRequired();
        builder.Property(x => x.LineNumber).IsRequired();
        builder.Property(x => x.GroupId);
        builder.Property(x => x.LineType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.ProductVariantId);
        builder.Property(x => x.WarehouseId);
        builder.Property(x => x.DescriptionSnapshot).IsRequired().HasMaxLength(500);
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
        builder.Property(x => x.PrescriptionRevisionId);
        builder.Property(x => x.PrescriptionEye).HasConversion<byte>();
        builder.Property(x => x.RequiresProduction).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.CustomerOrderId, x.LineNumber })
            .IsUnique().HasDatabaseName("UX_CustomerOrderLines_Order_LineNumber");
        builder.HasIndex(x => x.ProductVariantId).HasDatabaseName("IX_CustomerOrderLines_ProductVariantId");
        builder.HasIndex(x => x.WarehouseId).HasDatabaseName("IX_CustomerOrderLines_WarehouseId");
        builder.HasIndex(x => x.PrescriptionRevisionId).HasDatabaseName("IX_CustomerOrderLines_PrescriptionRevisionId");
        builder.HasIndex(x => x.GroupId).HasDatabaseName("IX_CustomerOrderLines_GroupId");

        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerOrderLines_ProductVariants_ProductVariantId");
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerOrderLines_Warehouses_WarehouseId");
        builder.HasOne<PrescriptionRevision>().WithMany().HasForeignKey(x => x.PrescriptionRevisionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerOrderLines_PrescriptionRevisions_PrescriptionRevisionId");
    }
}
