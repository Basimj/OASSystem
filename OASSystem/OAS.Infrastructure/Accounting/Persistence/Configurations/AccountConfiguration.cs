using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", "accounting");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.NameAr)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.NameEn)
            .HasMaxLength(150);

        builder.Property(x => x.ParentAccountId);

        builder.Property(x => x.Level)
            .IsRequired();

        builder.Property(x => x.AccountClass)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.AccountType)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.NormalBalance)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.IsPostingAccount)
            .IsRequired();

        builder.Property(x => x.IsControlAccount)
            .IsRequired();

        builder.Property(x => x.AllowManualPosting)
            .IsRequired();

        builder.Property(x => x.IsSystemAccount)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.EffectiveDate)
            .HasColumnType("date");

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_Accounts_Code");

        builder.HasIndex(x => x.ParentAccountId)
            .HasDatabaseName("IX_Accounts_ParentAccountId");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Accounts_ParentAccount");
    }
}
