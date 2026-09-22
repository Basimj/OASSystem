using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class PaymentVoucherLineConfiguration : IEntityTypeConfiguration<PaymentVoucherLine>
{
    public void Configure(EntityTypeBuilder<PaymentVoucherLine> builder)
    {
        builder.ToTable("tbl_PaymentVoucherLines", "accounting");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PaymentVoucherId)
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

        builder.HasIndex(x => x.PaymentVoucherId)
            .HasDatabaseName("IX_PaymentVoucherLines_PaymentVoucherId");

        builder.HasIndex(x => x.AccountId)
            .HasDatabaseName("IX_PaymentVoucherLines_AccountId");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PaymentVoucherLines_Account");
    }
}
