using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class OpticalQualityCheckConfiguration : IEntityTypeConfiguration<OpticalQualityCheck>
{
    public void Configure(EntityTypeBuilder<OpticalQualityCheck> builder)
    {
        builder.ToTable("tbl_OpticalQualityChecks", "dbo", t => t.HasCheckConstraint("CK_OpticalQualityChecks_AttemptNumber", "[AttemptNumber] > 0"));
        builder.HasKey(x => x.Id).HasName("PK_tbl_OpticalQualityChecks");
        builder.ConfigureSalesAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OpticalJobId).IsRequired();
        builder.Property(x => x.AttemptNumber).IsRequired();
        builder.Property(x => x.Result).HasConversion<byte>().IsRequired();
        builder.Property(x => x.FailureAction).HasConversion<byte?>();
        builder.Property(x => x.GeneralNotes).HasMaxLength(2000);
        builder.Property(x => x.CheckedBy);
        builder.Property(x => x.CheckedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.OpticalJobId, x.AttemptNumber }).IsUnique().HasDatabaseName("UQ_tbl_OpticalQualityChecks_OpticalJobId_AttemptNumber");
        builder.HasOne<OpticalJob>().WithMany().HasForeignKey(x => x.OpticalJobId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalQualityChecks_OpticalJobs");
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.QualityCheckId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalQualityCheckItems_QualityChecks");
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
