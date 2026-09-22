using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("tbl_JournalEntries", "accounting");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.JournalNumber)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.JournalType)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.PostingDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.DocumentDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.FiscalPeriodId)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.SourceModule)
            .HasMaxLength(50);

        builder.Property(x => x.SourceDocumentType)
            .HasMaxLength(50);

        builder.Property(x => x.SourceDocumentId);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.ReversedJournalId);
        builder.Property(x => x.ApprovedBy);

        builder.Property(x => x.ApprovedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.PostedBy);

        builder.Property(x => x.PostedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.JournalNumber)
            .IsUnique()
            .HasDatabaseName("UX_JournalEntries_JournalNumber");

        builder.HasIndex(x => x.FiscalPeriodId)
            .HasDatabaseName("IX_JournalEntries_FiscalPeriodId");

        builder.HasIndex(x => x.PostingDate)
            .HasDatabaseName("IX_JournalEntries_PostingDate");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_JournalEntries_Status");

        builder.HasIndex(x => x.ReversedJournalId)
            .HasDatabaseName("IX_JournalEntries_ReversedJournalId");

        builder.HasOne<FiscalPeriod>()
            .WithMany()
            .HasForeignKey(x => x.FiscalPeriodId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_JournalEntries_FiscalPeriod");

        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(x => x.ReversedJournalId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_JournalEntries_ReversedJournal");

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_JournalEntryLines_JournalEntry");

        builder.Navigation(x => x.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
