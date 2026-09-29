using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class SalesInvoiceLinePrescriptionSnapshotConfiguration : IEntityTypeConfiguration<SalesInvoiceLinePrescriptionSnapshot>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLinePrescriptionSnapshot> builder)
    {
        builder.ToTable("tbl_SalesInvoiceLinePrescriptionSnapshots", "dbo", t =>
        {
            t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_Axis", "[Axis] IS NULL OR ([Axis] >= 0 AND [Axis] <= 180)");
            t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_ADD", "[ADD] IS NULL OR [ADD] >= 0");
            t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_Prism", "[Prism] IS NULL OR [Prism] >= 0");
            t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_PD", "[PD] IS NULL OR [PD] > 0");
            t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_MonocularPD", "[MonocularPD] IS NULL OR [MonocularPD] > 0");
            t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_FittingHeight", "[FittingHeight] IS NULL OR [FittingHeight] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SalesInvoiceLineId).IsRequired();
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
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.SalesInvoiceLineId)
            .IsUnique().HasDatabaseName("UX_SalesInvoicePrescriptionSnapshots_LineId");
        builder.HasIndex(x => x.PrescriptionRevisionId)
            .HasDatabaseName("IX_SalesInvoicePrescriptionSnapshots_PrescriptionRevisionId");

        builder.HasOne<SalesInvoiceLine>()
            .WithOne(x => x.PrescriptionSnapshot)
            .HasForeignKey<SalesInvoiceLinePrescriptionSnapshot>(x => x.SalesInvoiceLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SalesInvoicePrescriptionSnapshots_Lines_SalesInvoiceLineId");
        builder.HasOne<PrescriptionRevision>().WithMany().HasForeignKey(x => x.PrescriptionRevisionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesInvoicePrescriptionSnapshots_Revisions_PrescriptionRevisionId");
    }
}
