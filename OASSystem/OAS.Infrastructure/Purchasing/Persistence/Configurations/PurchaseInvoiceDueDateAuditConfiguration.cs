using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseInvoiceDueDateAuditConfiguration : IEntityTypeConfiguration<PurchaseInvoiceDueDateAudit>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceDueDateAudit> builder)
    {
        builder.ToTable("tbl_PurchaseInvoiceDueDateAudits", "dbo");
        builder.HasKey(x => x.Id);
        builder.ConfigurePurchasingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PurchaseInvoiceId).IsRequired();
        builder.Property(x => x.OldDueDate).HasColumnType("date");
        builder.Property(x => x.OldPaymentTermDays);
        builder.Property(x => x.OldProvenance).HasConversion<byte>().IsRequired();
        builder.Property(x => x.NewDueDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.NewPaymentTermDays);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.VerifiedBy).HasMaxLength(64).IsRequired();
        builder.Property(x => x.VerifiedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.HasIndex(x => new { x.PurchaseInvoiceId, x.VerifiedAtUtc })
            .HasDatabaseName("IX_PurchaseInvoiceDueDateAudits_Invoice_VerifiedAt");
        builder.HasOne<PurchaseInvoice>().WithMany().HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PurchaseInvoiceDueDateAudits_PurchaseInvoices_PurchaseInvoiceId");
    }
}
