using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class SupplierOpeningBatchConfiguration : IEntityTypeConfiguration<SupplierOpeningBatch>
{
    public void Configure(EntityTypeBuilder<SupplierOpeningBatch> builder)
    {
        builder.ToTable("tbl_SupplierOpeningBatches", "dbo");
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BatchCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.AsOfDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.BaseCurrencyId).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.JournalEntryId);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.PostedBy).HasMaxLength(64);
        builder.Property(x => x.PostedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.BatchCode).IsUnique().HasDatabaseName("UX_SupplierOpeningBatches_BatchCode");
        builder.HasIndex(x => x.JournalEntryId).IsUnique().HasFilter("[JournalEntryId] IS NOT NULL")
            .HasDatabaseName("UX_SupplierOpeningBatches_JournalEntryId");
        builder.HasIndex(x => new { x.AsOfDate, x.Status }).HasDatabaseName("IX_SupplierOpeningBatches_AsOf_Status");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.BaseCurrencyId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningBatches_Currencies_BaseCurrencyId");
        builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningBatches_JournalEntries_JournalEntryId");
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierOpeningItems_Batches_BatchId");
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
