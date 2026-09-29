using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class PrescriptionRevisionConfiguration : IEntityTypeConfiguration<PrescriptionRevision>
{
    public void Configure(EntityTypeBuilder<PrescriptionRevision> builder)
    {
        builder.ToTable("tbl_PrescriptionRevisions", "dbo", t =>
        {
            t.HasCheckConstraint("CK_PrescriptionRevisions_RevisionNumber_Positive", "[RevisionNumber] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PrescriptionId).IsRequired();
        builder.Property(x => x.RevisionNumber).IsRequired();
        builder.Property(x => x.EffectiveDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.IsCurrent).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.PrescriptionId, x.RevisionNumber })
            .IsUnique()
            .HasDatabaseName("UX_PrescriptionRevisions_Prescription_Revision");
        builder.HasIndex(x => x.PrescriptionId)
            .IsUnique()
            .HasFilter("[IsCurrent] = 1")
            .HasDatabaseName("UX_PrescriptionRevisions_Current");

        builder.HasMany(x => x.EyeDetails)
            .WithOne()
            .HasForeignKey(x => x.PrescriptionRevisionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PrescriptionEyeDetails_Revisions_PrescriptionRevisionId");

        builder.Navigation(x => x.EyeDetails)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
