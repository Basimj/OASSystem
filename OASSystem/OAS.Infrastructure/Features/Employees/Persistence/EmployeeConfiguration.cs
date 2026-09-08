using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", "hr");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.EmployeeCode)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.NormalizedEmployeeCode)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Phone)
            .HasMaxLength(32);

        builder.Property(x => x.JobTitle)
            .HasMaxLength(100);

        builder.Property(x => x.HireDate)
            .HasColumnType("date");

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.IsSalesperson)
            .IsRequired();

        builder.Property(x => x.IsTechnician)
            .IsRequired();

        builder.Property(x => x.IsCommissionEligible)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.UserAccountId);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(64);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(64);

        builder.HasIndex(x => x.NormalizedEmployeeCode)
            .IsUnique()
            .HasDatabaseName("UX_Employees_NormalizedEmployeeCode");

        builder.HasIndex(x => x.UserAccountId)
            .IsUnique()
            .HasDatabaseName("UX_Employees_UserAccountId")
            .HasFilter("[UserAccountId] IS NOT NULL");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_Employees_IsActive");

        builder.HasIndex(x => new
        {
            x.LastName,
            x.FirstName
        })
        .HasDatabaseName("IX_Employees_LastName_FirstName");

        builder.HasIndex(x => x.Phone)
            .HasDatabaseName("IX_Employees_Phone")
            .HasFilter("[Phone] IS NOT NULL");

        builder.HasOne<OAS.Domain.Identity.Entities.UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_Employees_Users_UserAccountId");
    }
}