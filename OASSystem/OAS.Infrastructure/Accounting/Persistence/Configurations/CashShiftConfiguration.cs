using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class CashShiftConfiguration : IEntityTypeConfiguration<CashShift>
{
    public void Configure(EntityTypeBuilder<CashShift> builder)
    {
        builder.ToTable("tbl_CashShifts", "accounting");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ShiftNumber)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.CashAccountId)
            .IsRequired();

        builder.Property(x => x.OpenedBy)
            .IsRequired();

        builder.Property(x => x.OpenedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.OpeningBalance)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(x => x.ExpectedClosingBalance)
            .HasPrecision(19, 4);

        builder.Property(x => x.ActualClosingBalance)
            .HasPrecision(19, 4);

        builder.Property(x => x.DifferenceAmount)
            .HasPrecision(19, 4);

        builder.Property(x => x.ClosedBy);

        builder.Property(x => x.ClosedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.ShiftNumber)
            .IsUnique()
            .HasDatabaseName("UX_CashShifts_ShiftNumber");

        builder.HasIndex(x => x.CashAccountId)
            .HasDatabaseName("IX_CashShifts_CashAccountId");

        builder.HasIndex(x => x.OpenedAtUtc)
            .HasDatabaseName("IX_CashShifts_OpenedAtUtc");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_CashShifts_Status");

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(x => x.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CashShifts_CashAccount");
    }
}
