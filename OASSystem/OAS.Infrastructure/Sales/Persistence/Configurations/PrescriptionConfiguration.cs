using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("tbl_Prescriptions", "dbo");
        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PrescriptionCode).IsRequired().HasMaxLength(40);
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.PrescriptionDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>();
        builder.Property(x => x.PrescribedBy).HasMaxLength(150);
        builder.Property(x => x.ClinicName).HasMaxLength(150);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.PrescriptionCode)
            .IsUnique()
            .HasDatabaseName("UX_Prescriptions_PrescriptionCode");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_Prescriptions_CustomerId");
        builder.HasIndex(x => x.PrescriptionDate).HasDatabaseName("IX_Prescriptions_PrescriptionDate");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_Prescriptions_Status");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Prescriptions_Customers_CustomerId");

        builder.HasMany(x => x.Revisions)
            .WithOne()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PrescriptionRevisions_Prescriptions_PrescriptionId");

        builder.Navigation(x => x.Revisions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
