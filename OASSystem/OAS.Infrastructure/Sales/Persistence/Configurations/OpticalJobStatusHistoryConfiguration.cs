using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class OpticalJobStatusHistoryConfiguration : IEntityTypeConfiguration<OpticalJobStatusHistory>
{
    public void Configure(EntityTypeBuilder<OpticalJobStatusHistory> builder)
    {
        builder.ToTable("tbl_OpticalJobStatusHistory", "dbo");
        builder.HasKey(x => x.Id).HasName("PK_tbl_OpticalJobStatusHistory");
        builder.ConfigureSalesAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OpticalJobId).IsRequired();
        builder.Property(x => x.FromStatus).HasConversion<byte?>();
        builder.Property(x => x.ToStatus).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.ChangedBy).IsRequired();
        builder.Property(x => x.ChangedAtUtc).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.HasIndex(x => new { x.OpticalJobId, x.ChangedAtUtc }).HasDatabaseName("IX_OpticalJobStatusHistory_OpticalJobId_ChangedAtUtc");
        builder.HasOne<OpticalJob>().WithMany().HasForeignKey(x => x.OpticalJobId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_OpticalJobStatusHistory_OpticalJobs");
    }
}
