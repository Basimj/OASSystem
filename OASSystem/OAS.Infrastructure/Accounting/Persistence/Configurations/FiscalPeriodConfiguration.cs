using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        builder.ToTable("FiscalPeriods", "accounting");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FiscalYearId)
            .IsRequired();

        builder.Property(x => x.PeriodNumber)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.StartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.EndDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.SalesLocked)
            .IsRequired();

        builder.Property(x => x.InventoryLocked)
            .IsRequired();

        builder.Property(x => x.AccountingLocked)
            .IsRequired();

        builder.Property(x => x.ClosedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.ClosedBy);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.FiscalYearId, x.PeriodNumber })
            .IsUnique()
            .HasDatabaseName("UX_FiscalPeriods_FiscalYearId_PeriodNumber");

        builder.HasOne<FiscalYear>()
            .WithMany()
            .HasForeignKey(x => x.FiscalYearId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FiscalPeriods_FiscalYear");
    }
}
