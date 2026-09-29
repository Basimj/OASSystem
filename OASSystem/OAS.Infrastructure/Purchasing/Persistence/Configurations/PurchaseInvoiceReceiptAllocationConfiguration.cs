using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseInvoiceReceiptAllocationConfiguration : IEntityTypeConfiguration<PurchaseInvoiceReceiptAllocation>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceReceiptAllocation> builder)
    {
        builder.ToTable("tbl_PurchaseInvoiceReceiptAllocations", "dbo", t => t.HasCheckConstraint("CK_PurchaseInvoiceReceiptAllocations_MatchedQuantity_Positive", "[MatchedQuantity] > 0")); builder.HasKey(x => x.Id); builder.ConfigurePurchasingAudit(); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property(x => x.PurchaseInvoiceLineId).IsRequired(); builder.Property(x => x.PurchaseReceiptLineId).IsRequired(); builder.Property(x => x.MatchedQuantity).HasPrecision(18,3).IsRequired(); builder.Property(x => x.MatchedNetAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.QuantityVariance).HasPrecision(18,3).IsRequired(); builder.Property(x => x.PriceVarianceAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.TaxVarianceAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.MatchStatus).HasConversion<byte>().IsRequired(); builder.Property(x => x.ApprovalReason).HasMaxLength(500); builder.Property(x => x.ApprovedBy).HasMaxLength(64); builder.Property(x => x.ApprovedAt).HasColumnType("datetimeoffset"); builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken(); builder.HasIndex(x => x.PurchaseInvoiceLineId).HasDatabaseName("IX_PurchaseInvoiceReceiptAllocations_InvoiceLine"); builder.HasIndex(x => x.PurchaseReceiptLineId).HasDatabaseName("IX_PurchaseInvoiceReceiptAllocations_ReceiptLine"); builder.HasIndex(x => x.MatchStatus).HasDatabaseName("IX_PurchaseInvoiceReceiptAllocations_MatchStatus"); builder.HasIndex(x => new { x.PurchaseInvoiceLineId, x.PurchaseReceiptLineId }).IsUnique().HasDatabaseName("UX_PurchaseInvoiceReceiptAllocations_InvoiceLine_ReceiptLine"); builder.HasOne<PurchaseInvoiceLine>().WithMany().HasForeignKey(x => x.PurchaseInvoiceLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseInvoiceReceiptAllocations_InvoiceLines_PurchaseInvoiceLineId"); builder.HasOne<PurchaseReceiptLine>().WithMany().HasForeignKey(x => x.PurchaseReceiptLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseInvoiceReceiptAllocations_ReceiptLines_PurchaseReceiptLineId");
    }
}
