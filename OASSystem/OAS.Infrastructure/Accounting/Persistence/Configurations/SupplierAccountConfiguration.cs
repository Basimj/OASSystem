using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class SupplierAccountConfiguration : IEntityTypeConfiguration<SupplierAccount>
{
    public void Configure(EntityTypeBuilder<SupplierAccount> builder)
    {
        builder.ToTable("tbl_SupplierAccounts", "accounting");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SupplierId)
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

        builder.HasIndex(x => x.SupplierId)
            .IsUnique()
            .HasDatabaseName("UX_SupplierAccounts_SupplierId");

        builder.HasIndex(x => x.AccountId)
            .HasDatabaseName("IX_SupplierAccounts_AccountId");

        builder.HasIndex(x => x.ControlAccountId)
            .HasDatabaseName("IX_SupplierAccounts_ControlAccountId");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierAccounts_Account");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.ControlAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SupplierAccounts_ControlAccount");
    }
}
