using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class ExpenseTypeConfiguration : IEntityTypeConfiguration<ExpenseType>
{
    public void Configure(EntityTypeBuilder<ExpenseType> builder)
    {
        builder.ToTable("ExpenseTypes", "accounting");

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

        builder.Property(x => x.DefaultExpenseAccountId);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_ExpenseTypes_Code");

        builder.HasIndex(x => x.DefaultExpenseAccountId)
            .HasDatabaseName("IX_ExpenseTypes_DefaultExpenseAccountId");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.DefaultExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ExpenseTypes_DefaultExpenseAccount");
    }
}
