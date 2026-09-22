using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class ReceiptVoucherConfiguration : IEntityTypeConfiguration<ReceiptVoucher>
{
    public void Configure(EntityTypeBuilder<ReceiptVoucher> builder)
    {
        builder.ToTable("tbl_ReceiptVouchers", "dbo");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.VoucherNumber)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.VoucherDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.PartyType)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.CustomerId);

        builder.Property(x => x.ReceivedFrom)
            .HasMaxLength(200);

        builder.Property(x => x.PaymentMethod)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.CashAccountId);

        builder.Property(x => x.BankAccountId);

        builder.Property(x => x.TotalAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.JournalEntryId);
        builder.Property(x => x.PostedBy);

        builder.Property(x => x.PostedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.VoucherNumber)
            .IsUnique()
            .HasDatabaseName("UX_ReceiptVouchers_VoucherNumber");

        builder.HasIndex(x => x.VoucherDate)
            .HasDatabaseName("IX_ReceiptVouchers_VoucherDate");

        builder.HasIndex(x => x.CustomerId)
            .HasDatabaseName("IX_ReceiptVouchers_CustomerId");

        builder.HasIndex(x => x.CashAccountId)
            .HasDatabaseName("IX_ReceiptVouchers_CashAccountId");

        builder.HasIndex(x => x.BankAccountId)
            .HasDatabaseName("IX_ReceiptVouchers_BankAccountId");

        builder.HasIndex(x => x.JournalEntryId)
            .HasDatabaseName("IX_ReceiptVouchers_JournalEntryId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ReceiptVouchers_Status");

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(x => x.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ReceiptVouchers_CashAccount");

        builder.HasOne<BankAccount>()
            .WithMany()
            .HasForeignKey(x => x.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ReceiptVouchers_BankAccount");

        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ReceiptVouchers_JournalEntry");

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.ReceiptVoucherId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ReceiptVoucherLines_ReceiptVoucher");

        builder.Navigation(x => x.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
