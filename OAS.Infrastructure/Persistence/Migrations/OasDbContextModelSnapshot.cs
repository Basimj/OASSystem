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
            b.Property<string>("PasswordHash").IsRequired().HasMaxLength(512).HasColumnType("nvarchar(512)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("UserName").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.HasKey("Id");
            b.HasIndex("NormalizedEmail").IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
            b.HasIndex("NormalizedUserName").IsUnique();
            b.ToTable("Users", "security");
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
