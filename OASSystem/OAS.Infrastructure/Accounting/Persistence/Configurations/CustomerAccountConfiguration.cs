using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class CustomerAccountConfiguration : IEntityTypeConfiguration<CustomerAccount>
{
    public void Configure(EntityTypeBuilder<CustomerAccount> builder)
    {
        builder.ToTable("tbl_CustomerAccounts", "accounting");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.ControlAccountId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.CustomerId)
            .IsUnique()
            .HasDatabaseName("UX_CustomerAccounts_CustomerId");

        builder.HasIndex(x => x.AccountId)
            .HasDatabaseName("IX_CustomerAccounts_AccountId");

        builder.HasIndex(x => x.ControlAccountId)
            .HasDatabaseName("IX_CustomerAccounts_ControlAccountId");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CustomerAccounts_Account");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.ControlAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CustomerAccounts_ControlAccount");
    }
}
