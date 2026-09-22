using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class CashAccountConfiguration : IEntityTypeConfiguration<CashAccount>
{
    public void Configure(EntityTypeBuilder<CashAccount> builder)
    {
        builder.ToTable("tbl_CashAccounts", "dbo");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.IsDefault)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_CashAccounts_Code");

        builder.HasIndex(x => x.AccountId)
            .HasDatabaseName("IX_CashAccounts_AccountId");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CashAccounts_Account");
    }
}
