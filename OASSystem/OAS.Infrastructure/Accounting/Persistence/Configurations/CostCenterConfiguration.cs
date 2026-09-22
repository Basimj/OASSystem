using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class CostCenterConfiguration : IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        builder.ToTable("tbl_CostCenters", "dbo");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.NameAr)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.NameEn)
            .HasMaxLength(150);

        builder.Property(x => x.ParentCostCenterId);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_CostCenters_Code");

        builder.HasIndex(x => x.ParentCostCenterId)
            .HasDatabaseName("IX_CostCenters_ParentCostCenterId");

        builder.HasOne<CostCenter>()
            .WithMany()
            .HasForeignKey(x => x.ParentCostCenterId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CostCenters_ParentCostCenter");
    }
}
