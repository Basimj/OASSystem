using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", "hr", t => t.HasCheckConstraint("CK_Departments_NotSelfParent", "[ParentDepartmentId] IS NULL OR [ParentDepartmentId] <> [Id]"));
        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DepartmentCode).IsRequired().HasMaxLength(32);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.DepartmentCode).IsUnique().HasDatabaseName("UX_Departments_DepartmentCode");
        builder.HasIndex(x => x.ParentDepartmentId).HasDatabaseName("IX_Departments_ParentDepartmentId");
        builder.HasIndex(x => x.ManagerEmployeeId).HasDatabaseName("IX_Departments_ManagerEmployeeId");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_Departments_IsActive");
        builder.HasIndex(x => x.NameAr).HasDatabaseName("IX_Departments_NameAr");
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.ParentDepartmentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Departments_Departments_ParentDepartmentId");
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.ManagerEmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Departments_Employees_ManagerEmployeeId");
    }
}
