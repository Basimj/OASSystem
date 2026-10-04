using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class SalaryComponentConfiguration : IEntityTypeConfiguration<SalaryComponent>
{
    public void Configure(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.ToTable("SalaryComponents", "hr", t => t.HasCheckConstraint("CK_SalaryComponents_DisplayOrder", "[DisplayOrder] >= 0"));
        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ComponentCode).IsRequired().HasMaxLength(32);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.ComponentType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.CalculationMethod).HasConversion<byte>().IsRequired();
        builder.Property(x => x.DebitPostingRole).HasMaxLength(50);
        builder.Property(x => x.CreditPostingRole).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.ComponentCode).IsUnique().HasDatabaseName("UX_SalaryComponents_ComponentCode");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_SalaryComponents_IsActive");
        builder.HasIndex(x => x.IsBasicSalary).IsUnique().HasFilter("[IsBasicSalary] = 1 AND [IsActive] = 1").HasDatabaseName("UX_SalaryComponents_ActiveBasicSalary");
    }
}
