using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class CustomerAdvanceApplicationConfiguration : IEntityTypeConfiguration<CustomerAdvanceApplication>
{
    public void Configure(EntityTypeBuilder<CustomerAdvanceApplication> builder)
    {
        builder.ToTable("tbl_CustomerAdvanceApplications", "dbo", table =>
        {
            table.HasCheckConstraint("CK_CustomerAdvanceApplications_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_CustomerAdvanceApplications_BaseAmount", "[BaseAmount] > 0");
            table.HasCheckConstraint("CK_CustomerAdvanceApplications_TargetBaseAmount", "[TargetBaseAmount] > 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_tbl_CustomerAdvanceApplications");
        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CustomerAdvanceId).IsRequired();
        builder.Property(x => x.SalesInvoiceId).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.BaseAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.TargetBaseAmount).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.JournalEntryId).IsRequired();
        builder.Property(x => x.AppliedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.AppliedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.CustomerAdvanceId).HasDatabaseName("IX_CustomerAdvanceApplications_AdvanceId");
        builder.HasIndex(x => x.SalesInvoiceId).HasDatabaseName("IX_CustomerAdvanceApplications_SalesInvoiceId");
        builder.HasIndex(x => x.JournalEntryId).HasDatabaseName("IX_CustomerAdvanceApplications_JournalEntryId");

        builder.HasOne<CustomerAdvance>().WithMany().HasForeignKey(x => x.CustomerAdvanceId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerAdvanceApplications_CustomerAdvances_CustomerAdvanceId");
        builder.HasOne<SalesInvoice>().WithMany().HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerAdvanceApplications_SalesInvoices_SalesInvoiceId");
        builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CustomerAdvanceApplications_JournalEntries_JournalEntryId");
    }
}
