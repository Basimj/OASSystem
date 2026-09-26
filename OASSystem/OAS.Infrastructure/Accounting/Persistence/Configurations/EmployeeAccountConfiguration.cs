using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class EmployeeAccountConfiguration : IEntityTypeConfiguration<EmployeeAccount>
{
    public void Configure(EntityTypeBuilder<EmployeeAccount> builder)
    {
        builder.ToTable("tbl_EmployeeAccounts", "dbo");
        builder.HasKey(x => x.Id);
        builder.ConfigureAccountingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EmployeeId).IsRequired();
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.EmployeeId).IsUnique().HasDatabaseName("UX_EmployeeAccounts_EmployeeId");
        builder.HasIndex(x => x.AccountId).IsUnique().HasDatabaseName("UX_EmployeeAccounts_AccountId");
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeAccounts_Employee");
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeAccounts_Account");
    }
}
