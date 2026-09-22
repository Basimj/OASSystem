using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("tbl_PaymentAllocations", "dbo");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PaymentSourceType)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.PaymentSourceId)
            .IsRequired();

        builder.Property(x => x.TargetDocumentType)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.TargetDocumentId)
            .IsRequired();

        builder.Property(x => x.AllocatedAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(x => x.AllocatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");
        builder.HasIndex(x => x.PaymentSourceId)
            .HasDatabaseName("IX_PaymentAllocations_PaymentSourceId");

        builder.HasIndex(x => x.TargetDocumentId)
            .HasDatabaseName("IX_PaymentAllocations_TargetDocumentId");
    }
}
