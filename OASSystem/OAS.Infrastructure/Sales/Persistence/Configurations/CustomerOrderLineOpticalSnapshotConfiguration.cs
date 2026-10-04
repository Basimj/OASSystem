using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class CustomerOrderLineOpticalSnapshotConfiguration : IEntityTypeConfiguration<CustomerOrderLineOpticalSnapshot>
{
    public void Configure(EntityTypeBuilder<CustomerOrderLineOpticalSnapshot> builder)
    {
        builder.ToTable("tbl_CustomerOrderLineOpticalSnapshots", "dbo", table =>
        {
            table.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_Source", "([MeasurementSource] = 1 AND [PrescriptionRevisionId] IS NOT NULL) OR ([MeasurementSource] = 2)");
            table.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_Axis", "[Axis] IS NULL OR ([Axis] >= 0 AND [Axis] <= 180)");
            table.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_ADD", "[ADD] IS NULL OR [ADD] >= 0");
            table.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_Prism", "[Prism] IS NULL OR [Prism] >= 0");
            table.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_PD", "[PD] IS NULL OR [PD] > 0");
            table.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_MonocularPD", "[MonocularPD] IS NULL OR [MonocularPD] > 0");
            table.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_FittingHeight", "[FittingHeight] IS NULL OR [FittingHeight] > 0");
            table.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_RefractiveIndex", "[RefractiveIndexSnapshot] IS NULL OR [RefractiveIndexSnapshot] > 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_tbl_CustomerOrderLineOpticalSnapshots");
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CustomerOrderLineId).IsRequired();
        builder.Property(x => x.MeasurementSource).IsRequired().HasConversion<byte>();
        builder.Property(x => x.PrescriptionRevisionId);
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
        builder.Property(x => x.LensTypeSnapshot).HasMaxLength(100);
        builder.Property(x => x.MaterialSnapshot).HasMaxLength(100);
        builder.Property(x => x.CoatingSnapshot).HasMaxLength(100);
        builder.Property(x => x.RefractiveIndexSnapshot).HasPrecision(5, 3);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.CustomerOrderLineId)
            .IsUnique()
            .HasDatabaseName("UX_CustomerOrderLineOpticalSnapshots_CustomerOrderLineId");
        builder.HasIndex(x => x.PrescriptionRevisionId)
            .HasDatabaseName("IX_CustomerOrderLineOpticalSnapshots_PrescriptionRevisionId");
        builder.HasIndex(x => x.Eye)
            .HasDatabaseName("IX_CustomerOrderLineOpticalSnapshots_Eye");

        builder.HasOne<CustomerOrderLine>()
            .WithOne()
            .HasForeignKey<CustomerOrderLineOpticalSnapshot>(x => x.CustomerOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CustomerOrderLineOpticalSnapshots_CustomerOrderLines_CustomerOrderLineId");
        builder.HasOne<PrescriptionRevision>()
            .WithMany()
            .HasForeignKey(x => x.PrescriptionRevisionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CustomerOrderLineOpticalSnapshots_PrescriptionRevisions_PrescriptionRevisionId");
    }
}
