using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class ReceiptVoucherLineConfiguration : IEntityTypeConfiguration<ReceiptVoucherLine>
{
    public void Configure(EntityTypeBuilder<ReceiptVoucherLine> builder)
    {
        builder.ToTable("tbl_ReceiptVoucherLines", "dbo");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ReceiptVoucherId)
            .IsRequired();

        builder.Property(x => x.LineNumber)
            .IsRequired();

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(50);

        builder.Property(x => x.ReferenceId);

        builder.Property(x => x.Description)
            .HasMaxLength(300);

        builder.HasIndex(x => x.ReceiptVoucherId)
            .HasDatabaseName("IX_ReceiptVoucherLines_ReceiptVoucherId");

        builder.HasIndex(x => x.AccountId)
            .HasDatabaseName("IX_ReceiptVoucherLines_AccountId");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ReceiptVoucherLines_Account");
    }
}
