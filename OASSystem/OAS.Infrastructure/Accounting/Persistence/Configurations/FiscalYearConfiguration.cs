using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.ToTable("tbl_FiscalYears", "dbo");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.StartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.EndDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.ClosedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.ClosedBy);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_FiscalYears_Code");
    }
}
