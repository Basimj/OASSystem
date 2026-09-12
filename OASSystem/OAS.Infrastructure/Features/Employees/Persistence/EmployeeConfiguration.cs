using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class EmployeeConfiguration
    : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", "hr");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.EmployeeCode)
            .IsRequired()
            .HasMaxLength(32)
            .ValueGeneratedNever();

        builder.Ignore(x => x.DisplayName);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.JobTitleId)
            .IsRequired();

        builder.Property(x => x.HireDate)
            .HasColumnType("date");

        builder.Property(x => x.IsCommissionEligible)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.UserAccountId);

        builder.Property(x => x.Photo)
            .HasMaxLength(512);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(64);

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(64);

        builder.OwnsOne(x => x.ContactInfo, contact =>
        {
            contact.Property(x => x.Phone)
                .HasColumnName("Phone")
                .HasMaxLength(32);

            contact.Property(x => x.Email)
                .HasColumnName("Email")
                .HasMaxLength(256);

            contact.HasIndex(x => x.Phone)
                .HasDatabaseName("IX_Employees_Phone")
                .HasFilter("[Phone] IS NOT NULL");

            contact.HasIndex(x => x.Email)
                .HasDatabaseName("IX_Employees_Email")
                .HasFilter("[Email] IS NOT NULL");

            contact.OwnsOne(x => x.Address, address =>
            {
                address.Property(x => x.Country)
                    .HasColumnName("Country")
                    .HasMaxLength(100);

                address.Property(x => x.Governorate)
                    .HasColumnName("Governorate")
                    .HasMaxLength(100);

                address.Property(x => x.City)
                    .HasColumnName("City")
                    .HasMaxLength(100);

                address.Property(x => x.PostalCode)
                    .HasColumnName("PostalCode")
                    .HasMaxLength(24);

                address.Property(x => x.ResidentialAddress)
                    .HasColumnName("ResidentialAddress")
                    .HasMaxLength(300);
            });

            contact.Navigation(x => x.Address)
                .IsRequired();
        });

        builder.Navigation(x => x.ContactInfo)
            .IsRequired();

        builder.HasIndex(x => x.EmployeeCode)
            .IsUnique()
            .HasDatabaseName("UX_Employees_EmployeeCode");

        builder.HasIndex(x => x.JobTitleId)
            .HasDatabaseName("IX_Employees_JobTitleId");

        builder.HasIndex(x => x.UserAccountId)
            .IsUnique()
            .HasDatabaseName("UX_Employees_UserAccountId")
            .HasFilter("[UserAccountId] IS NOT NULL");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_Employees_IsActive");

        builder.HasIndex(x => new { x.LastName, x.FirstName })
            .HasDatabaseName("IX_Employees_LastName_FirstName");

        builder.HasOne<JobTitle>()
            .WithMany()
            .HasForeignKey(x => x.JobTitleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Employees_JobTitles_JobTitleId");

        builder.HasOne<OAS.Domain.Identity.Entities.UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Employees_Users_UserAccountId");
    }
}