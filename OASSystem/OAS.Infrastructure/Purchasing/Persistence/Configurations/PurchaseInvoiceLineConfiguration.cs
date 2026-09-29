using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseInvoiceLineConfiguration : IEntityTypeConfiguration<PurchaseInvoiceLine>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceLine> builder)
    {
        builder.ToTable("tbl_PurchaseInvoiceLines", "dbo", t => { t.HasCheckConstraint("CK_PurchaseInvoiceLines_Quantity_Positive", "[Quantity] > 0"); t.HasCheckConstraint("CK_PurchaseInvoiceLines_Amounts_NonNegative", "[UnitPrice] >= 0 AND [GrossAmount] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] >= 0 AND [TaxAmount] >= 0 AND [FinalAmount] >= 0 AND [BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseFinalAmount] >= 0"); }); builder.HasKey(x => x.Id); builder.ConfigurePurchasingAudit(); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property(x => x.PurchaseInvoiceId).IsRequired(); builder.Property(x => x.LineSequence).IsRequired(); builder.Property(x => x.PurchaseOrderLineId); builder.Property(x => x.ProductVariantId).IsRequired(); builder.Property(x => x.ProductCodeSnapshot).HasMaxLength(64); builder.Property(x => x.DescriptionSnapshot).IsRequired().HasMaxLength(250); builder.Property(x => x.Quantity).HasPrecision(18,3).IsRequired(); builder.Property(x => x.UnitPrice).HasPrecision(19,4).IsRequired(); builder.Property(x => x.GrossAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.DiscountAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.NetAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.TaxRate).HasPrecision(9,6).IsRequired(); builder.Property(x => x.TaxAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.FinalAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.BaseNetAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.BaseTaxAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.BaseFinalAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken(); builder.HasIndex(x => new { x.PurchaseInvoiceId, x.LineSequence }).IsUnique().HasDatabaseName("UX_PurchaseInvoiceLines_Invoice_LineSequence"); builder.HasIndex(x => x.PurchaseOrderLineId).HasDatabaseName("IX_PurchaseInvoiceLines_PurchaseOrderLineId"); builder.HasIndex(x => x.ProductVariantId).HasDatabaseName("IX_PurchaseInvoiceLines_ProductVariantId"); builder.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseInvoiceLines_OrderLines_PurchaseOrderLineId"); builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseInvoiceLines_ProductVariants_ProductVariantId");
    }
}
