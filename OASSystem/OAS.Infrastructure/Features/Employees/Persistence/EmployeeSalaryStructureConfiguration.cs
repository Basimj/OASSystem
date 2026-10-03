using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class EmployeeSalaryStructureConfiguration : IEntityTypeConfiguration<EmployeeSalaryStructure>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryStructure> builder)
    {
        builder.ToTable("EmployeeSalaryStructures", "hr", t => t.HasCheckConstraint("CK_EmployeeSalaryStructures_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.StructureCode).IsRequired().HasMaxLength(40);
        builder.Property(x => x.EffectiveFrom).HasColumnType("date").IsRequired();
        builder.Property(x => x.EffectiveTo).HasColumnType("date");
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.ApprovedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.StructureCode).IsUnique().HasDatabaseName("UX_EmployeeSalaryStructures_StructureCode");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_EmployeeSalaryStructures_EmployeeId");
        builder.HasIndex(x => new { x.EmployeeId, x.Status }).IsUnique().HasFilter($"[Status] = {(byte)SalaryStructureStatus.Active}").HasDatabaseName("UX_EmployeeSalaryStructures_Employee_Active");
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom, x.EffectiveTo }).HasDatabaseName("IX_EmployeeSalaryStructures_EffectiveDates");
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeSalaryStructures_Employees_EmployeeId");
        builder.HasOne<EmployeeContract>().WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeSalaryStructures_Contracts_ContractId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeSalaryStructures_Currencies_CurrencyId");
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.EmployeeSalaryStructureId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_EmployeeSalaryStructureLines_Structures_StructureId");
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
