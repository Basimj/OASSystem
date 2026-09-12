using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
sealed partial class OasDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "9.0.8").HasAnnotation("Relational:MaxIdentifierLength", 128);
        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("OAS.Domain.Identity.Entities.Role", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("DisplayName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<bool>("IsSystem").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Name").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("NormalizedName").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.HasKey("Id");
            b.HasIndex("NormalizedName").IsUnique();
            b.ToTable("Roles", "security");
        });

        modelBuilder.Entity("OAS.Domain.Identity.Entities.UserAccount", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<int>("AccessFailedCount").HasColumnType("int");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Email").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("FirstName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<bool>("IsSuperAdmin").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastLoginAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("LastName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<DateTimeOffset?>("LockoutEndUtc").HasColumnType("datetimeoffset");
            b.Property<bool>("MustChangePassword").HasColumnType("bit");
            b.Property<string>("NormalizedEmail").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("NormalizedUserName").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("PhoneNumber").HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("PasswordHash").IsRequired().HasMaxLength(512).HasColumnType("nvarchar(512)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("UserName").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.HasKey("Id");
            b.HasIndex("NormalizedEmail").IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
            b.HasIndex("NormalizedUserName").IsUnique();
            b.ToTable("Users", "security");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.JobTitle", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Name").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id");
            b.HasIndex("Name").IsUnique().HasDatabaseName("UX_JobTitles_Name");
            b.HasIndex("IsActive").HasDatabaseName("IX_JobTitles_IsActive");
            b.ToTable("JobTitles", "hr");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.Employee", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("EmployeeCode")
      .IsRequired()
      .HasMaxLength(32)
      .HasColumnType("nvarchar(32)");
            b.Property<string>("FirstName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<DateOnly?>("HireDate").HasColumnType("date");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<bool>("IsCommissionEligible").HasColumnType("bit");
            b.Property<Guid>("JobTitleId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("LastName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Photo").HasMaxLength(512).HasColumnType("nvarchar(512)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<Guid?>("UserAccountId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("EmployeeCode")
                .IsUnique()
                .HasDatabaseName("UX_Employees_EmployeeCode");
            b.HasIndex("IsActive").HasDatabaseName("IX_Employees_IsActive");
            b.HasIndex("JobTitleId").HasDatabaseName("IX_Employees_JobTitleId");
            b.HasIndex("LastName", "FirstName").HasDatabaseName("IX_Employees_LastName_FirstName");
            b.HasIndex("UserAccountId").IsUnique().HasDatabaseName("UX_Employees_UserAccountId").HasFilter("[UserAccountId] IS NOT NULL");
            b.ToTable("Employees", "hr");
        });

        modelBuilder.Entity("OAS.Domain.Identity.Entities.UserPasswordHistory", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("PasswordHash").IsRequired().HasMaxLength(512).HasColumnType("nvarchar(512)");
            b.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("UserId", "CreatedAtUtc");
            b.ToTable("UserPasswordHistory", "security");
        });

        modelBuilder.Entity("OAS.Domain.Identity.Entities.UserRole", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<Guid>("RoleId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("RoleId");
            b.HasIndex("UserId", "RoleId").IsUnique();
            b.ToTable("UserRoles", "security");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.Employee", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.JobTitle", null).WithMany().HasForeignKey("JobTitleId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_Employees_JobTitles_JobTitleId");
            b.HasOne("OAS.Domain.Identity.Entities.UserAccount", null).WithMany().HasForeignKey("UserAccountId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Employees_Users_UserAccountId");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.Employee", b =>
        {
            b.OwnsOne("OAS.Domain.Features.Employees.ValueObjects.ContactInfo", "ContactInfo", b1 =>
            {
                b1.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
                b1.Property<string>("Email").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("Email");
                b1.Property<string>("Phone").HasMaxLength(32).HasColumnType("nvarchar(32)").HasColumnName("Phone");
                b1.HasKey("EmployeeId");
                b1.HasIndex("Email").HasDatabaseName("IX_Employees_Email").HasFilter("[Email] IS NOT NULL");
                b1.HasIndex("Phone").HasDatabaseName("IX_Employees_Phone").HasFilter("[Phone] IS NOT NULL");
                b1.ToTable("Employees", "hr");
                b1.WithOwner().HasForeignKey("EmployeeId");

                b1.OwnsOne("OAS.Domain.Features.Employees.ValueObjects.Address", "Address", b2 =>
                {
                    b2.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
                    b2.Property<string>("City").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("City");
                    b2.Property<string>("Country").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("Country");
                    b2.Property<string>("Governorate").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("Governorate");
                    b2.Property<string>("PostalCode").HasMaxLength(24).HasColumnType("nvarchar(24)").HasColumnName("PostalCode");
                    b2.Property<string>("ResidentialAddress").HasMaxLength(300).HasColumnType("nvarchar(300)").HasColumnName("ResidentialAddress");
                    b2.HasKey("EmployeeId");
                    b2.ToTable("Employees", "hr");
                    b2.WithOwner().HasForeignKey("EmployeeId");
                });

                b1.Navigation("Address").IsRequired();
            });

            b.Navigation("ContactInfo").IsRequired();
        });

        modelBuilder.Entity("OAS.Domain.Identity.Entities.UserPasswordHistory", b =>
        {
            b.HasOne("OAS.Domain.Identity.Entities.UserAccount", null).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("OAS.Domain.Identity.Entities.UserRole", b =>
        {
            b.HasOne("OAS.Domain.Identity.Entities.Role", null).WithMany().HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.HasOne("OAS.Domain.Identity.Entities.UserAccount", null).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });
#pragma warning restore 612, 618
    }
}
