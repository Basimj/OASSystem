using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class OpticalQualityCheckItemConfiguration : IEntityTypeConfiguration<OpticalQualityCheckItem>
{
    public void Configure(EntityTypeBuilder<OpticalQualityCheckItem> builder)
    {
        builder.ToTable("tbl_OpticalQualityCheckItems", "dbo", t => t.HasCheckConstraint("CK_OpticalQualityCheckItems_Sequence", "[Sequence] > 0"));
        builder.HasKey(x => x.Id).HasName("PK_tbl_OpticalQualityCheckItems");
        builder.ConfigureSalesAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.QualityCheckId).IsRequired();
        builder.Property(x => x.CheckCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CheckName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Result).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Sequence).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.QualityCheckId, x.CheckCode }).IsUnique().HasDatabaseName("UQ_tbl_OpticalQualityCheckItems_QualityCheckId_CheckCode");
    }
}
