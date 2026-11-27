using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class CashTransferConfiguration : IEntityTypeConfiguration<CashTransfer>
{
    public void Configure(EntityTypeBuilder<CashTransfer> b)
    {
        b.ToTable("tbl_CashTransfers", "dbo");
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.ConfigureAccountingAudit();
        b.Property(x => x.TransferNumber).HasMaxLength(40).IsRequired();
        b.Property(x => x.TransferDate).HasColumnType("date");
        b.Property(x => x.ReversalDate).HasColumnType("date");
        b.Property(x => x.ApprovedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.SourceName).HasMaxLength(300).IsRequired();
        b.Property(x => x.DestinationName).HasMaxLength(300).IsRequired();
        b.Property(x => x.SourceCurrencyCode).HasMaxLength(16).IsRequired();
        b.Property(x => x.DestinationCurrencyCode).HasMaxLength(16).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(400).IsRequired();
        b.Property(x => x.ReversalReason).HasMaxLength(400);
        b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.RowVersion).IsRowVersion();
        foreach (var property in new[] { nameof(CashTransfer.SourceAmount), nameof(CashTransfer.DestinationAmount), nameof(CashTransfer.FeeAmount),
            nameof(CashTransfer.SourceBaseAmount), nameof(CashTransfer.DestinationBaseAmount), nameof(CashTransfer.FeeBaseAmount), nameof(CashTransfer.FxDifferenceBase) })
            b.Property(property).HasPrecision(19, 4);
        b.HasIndex(x => x.TransferNumber).IsUnique().HasDatabaseName("UX_CashTransfers_Number");
        b.HasIndex(x => x.TransferDate).HasDatabaseName("IX_CashTransfers_Date");
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.SourceAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.DestinationAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.FeeAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Currency>().WithMany().HasForeignKey(x => x.SourceCurrencyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Currency>().WithMany().HasForeignKey(x => x.DestinationCurrencyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.ReversalJournalEntryId).OnDelete(DeleteBehavior.Restrict);
    }
}
