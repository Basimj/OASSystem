using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("tbl_PaymentAllocations", "dbo", t =>
        {
            t.HasCheckConstraint("CK_PaymentAllocations_Amount_Positive", "[AllocatedAmount] > 0");
            t.HasCheckConstraint("CK_PaymentAllocations_BaseAmount_Positive", "[BaseAllocatedAmount] IS NULL OR [BaseAllocatedAmount] > 0");
            t.HasCheckConstraint("CK_PaymentAllocations_ExchangeRate_Positive", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
            t.HasCheckConstraint("CK_PaymentAllocations_TypedSource", "([ReceiptVoucherLineId] IS NULL OR [PaymentVoucherLineId] IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PaymentSourceType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.PaymentSourceId).IsRequired();
        builder.Property(x => x.TargetDocumentType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.TargetDocumentId).IsRequired();
        builder.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(8);
        builder.Property(x => x.AllocatedAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.ExchangeRate).HasPrecision(19, 8);
        builder.Property(x => x.BaseAllocatedAmount).HasPrecision(19, 4);
        builder.Property(x => x.AllocatedAtUtc).IsRequired().HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.PaymentSourceId).HasDatabaseName("IX_PaymentAllocations_PaymentSourceId");
        builder.HasIndex(x => x.ReceiptVoucherLineId).HasDatabaseName("IX_PaymentAllocations_ReceiptVoucherLineId");
        builder.HasIndex(x => x.PaymentVoucherLineId).HasDatabaseName("IX_PaymentAllocations_PaymentVoucherLineId");
        builder.HasIndex(x => x.TargetDocumentId).HasDatabaseName("IX_PaymentAllocations_TargetDocumentId");
        builder.HasIndex(x => x.CurrencyId).HasDatabaseName("IX_PaymentAllocations_CurrencyId");

        builder.HasOne<ReceiptVoucherLine>().WithMany().HasForeignKey(x => x.ReceiptVoucherLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentAllocations_ReceiptVoucherLine");
        builder.HasOne<PaymentVoucherLine>().WithMany().HasForeignKey(x => x.PaymentVoucherLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentAllocations_PaymentVoucherLine");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PaymentAllocations_Currency");
    }
}
