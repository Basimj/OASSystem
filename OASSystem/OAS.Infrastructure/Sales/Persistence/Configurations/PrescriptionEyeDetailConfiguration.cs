using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class PrescriptionEyeDetailConfiguration : IEntityTypeConfiguration<PrescriptionEyeDetail>
{
    public void Configure(EntityTypeBuilder<PrescriptionEyeDetail> builder)
    {
        builder.ToTable("tbl_PrescriptionEyeDetails", "dbo", t =>
        {
            t.HasCheckConstraint("CK_PrescriptionEyeDetails_Axis", "[Axis] IS NULL OR ([Axis] >= 0 AND [Axis] <= 180)");
            t.HasCheckConstraint("CK_PrescriptionEyeDetails_ADD", "[ADD] IS NULL OR [ADD] >= 0");
            t.HasCheckConstraint("CK_PrescriptionEyeDetails_Prism", "[Prism] IS NULL OR [Prism] >= 0");
            t.HasCheckConstraint("CK_PrescriptionEyeDetails_PD", "[PD] IS NULL OR [PD] > 0");
            t.HasCheckConstraint("CK_PrescriptionEyeDetails_MonocularPD", "[MonocularPD] IS NULL OR [MonocularPD] > 0");
            t.HasCheckConstraint("CK_PrescriptionEyeDetails_FittingHeight", "[FittingHeight] IS NULL OR [FittingHeight] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PrescriptionRevisionId).IsRequired();
        builder.Property(x => x.Eye).IsRequired().HasConversion<byte>();
        builder.Property(x => x.SPH).HasPrecision(6, 2);
        builder.Property(x => x.CYL).HasPrecision(6, 2);
        builder.Property(x => x.Axis);
        builder.Property(x => x.ADD).HasPrecision(6, 2);
        builder.Property(x => x.Prism).HasPrecision(6, 2);
        builder.Property(x => x.PrismBase).HasConversion<byte>();
        builder.Property(x => x.PD).HasPrecision(6, 2);
        builder.Property(x => x.MonocularPD).HasPrecision(6, 2);
        builder.Property(x => x.VA).HasMaxLength(20);
        builder.Property(x => x.FittingHeight).HasPrecision(6, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.PrescriptionRevisionId, x.Eye })
            .IsUnique()
            .HasDatabaseName("UX_PrescriptionEyeDetails_Revision_Eye");
    }
}
