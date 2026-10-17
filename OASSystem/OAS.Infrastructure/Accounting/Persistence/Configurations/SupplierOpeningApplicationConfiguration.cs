using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class SupplierOpeningApplicationConfiguration : IEntityTypeConfiguration<SupplierOpeningApplication>
{
    public void Configure(EntityTypeBuilder<SupplierOpeningApplication> builder)
    {
        builder.ToTable("tbl_SupplierOpeningApplications", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SupplierOpeningApplications_SourceAmount_Positive", "[AppliedSourceBaseAmount] > 0");
            t.HasCheckConstraint("CK_SupplierOpeningApplications_TargetAmount_Positive", "[AppliedTargetBaseAmount] > 0");
            t.HasCheckConstraint("CK_SupplierOpeningApplications_Status", "[Status] IN (1,2)");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SupplierOpeningItemId).IsRequired();
        builder.Property(x => x.PaymentVoucherLineId).IsRequired();
        builder.Property(x => x.SupplierId).IsRequired();
        builder.Property(x => x.AppliedSourceBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.AppliedTargetBaseAmount).HasPrecision(19,4).IsRequired();
        builder.Property(x => x.AppliedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.AppliedBy).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(80);
        builder.Property(x => x.ReversedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.ReversedBy).HasMaxLength(64);
        builder.Property(x => x.ReversalReason).HasMaxLength(300);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_SupplierOpeningApplications_IdempotencyKey");
        builder.HasIndex(x => new { x.SupplierOpeningItemId, x.Status })
            .HasDatabaseName("IX_SupplierOpeningApplications_Item_Status");
        builder.HasIndex(x => new { x.PaymentVoucherLineId, x.Status })
            .HasDatabaseName("IX_SupplierOpeningApplications_PaymentLine_Status");
        builder.HasOne<SupplierOpeningItem>().WithMany().HasForeignKey(x => x.SupplierOpeningItemId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningApplications_Items_ItemId");
        builder.HasOne<PaymentVoucherLine>().WithMany().HasForeignKey(x => x.PaymentVoucherLineId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningApplications_PaymentVoucherLines_LineId");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningApplications_Suppliers_SupplierId");
    }
}
