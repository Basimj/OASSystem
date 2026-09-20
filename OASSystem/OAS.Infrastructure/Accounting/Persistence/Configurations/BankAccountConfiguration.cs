using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts", "accounting");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.BankName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.AccountName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.AccountNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IBAN)
            .HasMaxLength(50);

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_BankAccounts_Code");

        builder.HasIndex(x => x.AccountId)
            .HasDatabaseName("IX_BankAccounts_AccountId");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_BankAccounts_Account");
    }
}
