using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines", "accounting");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.JournalEntryId)
            .IsRequired();

        builder.Property(x => x.LineNumber)
            .IsRequired();

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.DebitAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(x => x.CreditAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(x => x.Description)
            .HasMaxLength(300);

        builder.Property(x => x.CustomerId);

        builder.Property(x => x.SupplierId);

        builder.Property(x => x.CostCenterId);

        builder.Property(x => x.ProductVariantId);

        builder.Property(x => x.WarehouseId);

        builder.HasIndex(x => x.JournalEntryId)
            .HasDatabaseName("IX_JournalEntryLines_JournalEntryId");

        builder.HasIndex(x => x.AccountId)
            .HasDatabaseName("IX_JournalEntryLines_AccountId");

        builder.HasIndex(x => x.CostCenterId)
            .HasDatabaseName("IX_JournalEntryLines_CostCenterId");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_JournalEntryLines_Account");

        builder.HasOne<CostCenter>()
            .WithMany()
            .HasForeignKey(x => x.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_JournalEntryLines_CostCenter");
    }
}
