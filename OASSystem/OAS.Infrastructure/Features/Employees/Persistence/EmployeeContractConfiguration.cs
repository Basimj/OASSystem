using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class EmployeeContractConfiguration : IEntityTypeConfiguration<EmployeeContract>
{
    public void Configure(EntityTypeBuilder<EmployeeContract> builder)
    {
        builder.ToTable("EmployeeContracts", "hr", t =>
        {
            t.HasCheckConstraint("CK_EmployeeContracts_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
            t.HasCheckConstraint("CK_EmployeeContracts_Probation", "[ProbationEndDate] IS NULL OR ([ProbationEndDate] >= [StartDate] AND ([EndDate] IS NULL OR [ProbationEndDate] <= [EndDate]))");
            t.HasCheckConstraint("CK_EmployeeContracts_WorkingHours", "[WorkingHoursPerDay] IS NULL OR ([WorkingHoursPerDay] > 0 AND [WorkingHoursPerDay] <= 24)");
            t.HasCheckConstraint("CK_EmployeeContracts_WorkingDays", "[WorkingDaysPerWeek] IS NULL OR ([WorkingDaysPerWeek] > 0 AND [WorkingDaysPerWeek] <= 7)");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContractCode).IsRequired().HasMaxLength(40);
        builder.Property(x => x.ContractType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.StartDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnType("date");
        builder.Property(x => x.ProbationEndDate).HasColumnType("date");
        builder.Property(x => x.TerminationEffectiveDate).HasColumnType("date");
        builder.Property(x => x.WorkingHoursPerDay).HasColumnType("decimal(6,2)");
        builder.Property(x => x.WorkingDaysPerWeek).HasColumnType("decimal(4,2)");
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.ActivatedBy).HasMaxLength(64);
        builder.Property(x => x.TerminatedBy).HasMaxLength(64);
        builder.Property(x => x.TerminationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.ContractCode).IsUnique().HasDatabaseName("UX_EmployeeContracts_ContractCode");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_EmployeeContracts_EmployeeId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_EmployeeContracts_Status");
        builder.HasIndex(x => new { x.EmployeeId, x.Status }).IsUnique().HasFilter($"[Status] = {(byte)EmploymentContractStatus.Active}").HasDatabaseName("UX_EmployeeContracts_Employee_Active");
        builder.HasIndex(x => new { x.StartDate, x.EndDate }).HasDatabaseName("IX_EmployeeContracts_StartDate_EndDate");
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeContracts_Employees_EmployeeId");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeContracts_Currencies_CurrencyId");
    }
}
