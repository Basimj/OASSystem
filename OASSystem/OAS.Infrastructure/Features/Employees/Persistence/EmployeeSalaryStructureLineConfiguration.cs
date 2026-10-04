using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class EmployeeSalaryStructureLineConfiguration : IEntityTypeConfiguration<EmployeeSalaryStructureLine>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryStructureLine> builder)
    {
        builder.ToTable("EmployeeSalaryStructureLines", "hr", t =>
        {
            t.HasCheckConstraint("CK_EmployeeSalaryStructureLines_Amount", "[Amount] >= 0");
            t.HasCheckConstraint("CK_EmployeeSalaryStructureLines_Percentage", "[Percentage] IS NULL OR [Percentage] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Amount).HasColumnType("decimal(19,4)").IsRequired();
        builder.Property(x => x.Percentage).HasColumnType("decimal(9,6)");
        builder.Property(x => x.ComponentCodeSnapshot).IsRequired().HasMaxLength(32);
        builder.Property(x => x.ComponentNameSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(x => x.ComponentTypeSnapshot).HasConversion<byte>().IsRequired();
        builder.Property(x => x.CalculationMethodSnapshot).HasConversion<byte>().IsRequired();
        builder.Property(x => x.DebitPostingRoleSnapshot).HasMaxLength(50);
        builder.Property(x => x.CreditPostingRoleSnapshot).HasMaxLength(50);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.EmployeeSalaryStructureId).HasDatabaseName("IX_EmployeeSalaryStructureLines_StructureId");
        builder.HasIndex(x => new { x.EmployeeSalaryStructureId, x.SalaryComponentId }).IsUnique().HasDatabaseName("UX_EmployeeSalaryStructureLines_Structure_Component");
        builder.HasOne<SalaryComponent>().WithMany().HasForeignKey(x => x.SalaryComponentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeSalaryStructureLines_Components_ComponentId");
    }
}
