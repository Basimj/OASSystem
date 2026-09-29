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

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Account", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("Code").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NameEn").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<Guid?>("ParentAccountId").HasColumnType("uniqueidentifier");
            b.Property<byte>("Level").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("AccountClass").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("AccountType").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("NormalBalance").IsRequired().HasColumnType("tinyint");
            b.Property<bool>("IsPostingAccount").IsRequired().HasColumnType("bit");
            b.Property<bool>("IsControlAccount").IsRequired().HasColumnType("bit");
            b.Property<bool>("AllowManualPosting").IsRequired().HasColumnType("bit");
            b.Property<bool>("IsSystemAccount").IsRequired().HasColumnType("bit");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<DateOnly?>("EffectiveDate").HasColumnType("date");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_Accounts_Code");
            b.HasIndex("ParentAccountId").HasDatabaseName("IX_Accounts_ParentAccountId");
            b.ToTable("tbl_Accounts", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.AccountingSettings", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("BaseCurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("EmployeeParentAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CashParentAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("BankParentAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("ExchangeGainAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("ExchangeLossAccountId").HasColumnType("uniqueidentifier");
            b.Property<byte>("DefaultExchangeRateType").IsRequired().HasColumnType("tinyint");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.ToTable("tbl_AccountingSettings", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.BankAccount", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("Code").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<string>("BankName").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("AccountName").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("AccountNumber").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("IBAN").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid>("AccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_BankAccounts_Code");
            b.HasIndex("AccountNumber").IsUnique().HasDatabaseName("UX_BankAccounts_AccountNumber");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_BankAccounts_CurrencyId");
            b.HasIndex("AccountId").HasDatabaseName("IX_BankAccounts_AccountId");
            b.ToTable("tbl_BankAccounts", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CashAccount", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("Code").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<string>("Name").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<Guid>("AccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<bool>("IsDefault").IsRequired().HasColumnType("bit");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_CashAccounts_Code");
            b.HasIndex("AccountId").HasDatabaseName("IX_CashAccounts_AccountId");
            b.HasIndex("CurrencyId").IsUnique().HasDatabaseName("UX_CashAccounts_DefaultPerCurrency").HasFilter("[IsDefault] = 1 AND [IsActive] = 1 AND [CurrencyId] IS NOT NULL");
            b.ToTable("tbl_CashAccounts", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CashShift", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("ShiftNumber").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("CashAccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("OpenedBy").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateTime>("OpenedAtUtc").IsRequired().HasColumnType("datetime2(3)");
            b.Property<decimal>("OpeningBalance").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal?>("ExpectedClosingBalance").HasColumnType("decimal(19,4)");
            b.Property<decimal?>("ActualClosingBalance").HasColumnType("decimal(19,4)");
            b.Property<decimal?>("DifferenceAmount").HasColumnType("decimal(19,4)");
            b.Property<Guid?>("ClosedBy").HasColumnType("uniqueidentifier");
            b.Property<DateTime?>("ClosedAtUtc").HasColumnType("datetime2(3)");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("ShiftNumber").IsUnique().HasDatabaseName("UX_CashShifts_ShiftNumber");
            b.HasIndex("CashAccountId").HasDatabaseName("IX_CashShifts_CashAccountId");
            b.HasIndex("OpenedAtUtc").HasDatabaseName("IX_CashShifts_OpenedAtUtc");
            b.HasIndex("Status").HasDatabaseName("IX_CashShifts_Status");
            b.ToTable("tbl_CashShifts", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CostCenter", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("Code").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NameEn").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<Guid?>("ParentCostCenterId").HasColumnType("uniqueidentifier");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_CostCenters_Code");
            b.HasIndex("ParentCostCenterId").HasDatabaseName("IX_CostCenters_ParentCostCenterId");
            b.ToTable("tbl_CostCenters", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Currency", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("Code").IsRequired().HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("NameEn").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Symbol").HasMaxLength(12).HasColumnType("nvarchar(12)");
            b.Property<byte>("DecimalPlaces").IsRequired().HasColumnType("tinyint");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_Currencies_Code");
            b.HasIndex("IsActive").HasDatabaseName("IX_Currencies_IsActive");
            b.ToTable("tbl_Currencies", "dbo", t =>
            {
                t.HasCheckConstraint("CK_Currencies_DecimalPlaces", "[DecimalPlaces] BETWEEN 0 AND 6");
            });
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Customer", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("CustomerCode").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<Guid>("AccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<byte>("EntityType").IsRequired().HasColumnType("tinyint");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NameEn").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("TradeName").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NationalId").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("CommercialRegistrationNo").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("TaxNumber").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<DateOnly?>("DateOfBirth").HasColumnType("date");
            b.Property<byte>("Gender").IsRequired().HasColumnType("tinyint");
            b.Property<bool>("IsCreditAllowed").IsRequired().HasColumnType("bit");
            b.Property<decimal>("CreditLimit").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<int>("PaymentTermDays").IsRequired().HasColumnType("int");
            b.Property<DateOnly?>("CustomerSince").HasColumnType("date");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("CustomerCode").IsUnique().HasDatabaseName("UX_Customers_CustomerCode");
            b.HasIndex("AccountId").IsUnique().HasDatabaseName("UX_Customers_AccountId");
            b.HasIndex("NationalId").IsUnique().HasDatabaseName("UX_Customers_NationalId").HasFilter("[NationalId] IS NOT NULL");
            b.HasIndex("TaxNumber").IsUnique().HasDatabaseName("UX_Customers_TaxNumber").HasFilter("[TaxNumber] IS NOT NULL");
            b.HasIndex("CommercialRegistrationNo").IsUnique().HasDatabaseName("UX_Customers_CommercialRegistrationNo").HasFilter("[CommercialRegistrationNo] IS NOT NULL");
            b.HasIndex("NameAr").HasDatabaseName("IX_Customers_NameAr");
            b.HasIndex("IsActive").HasDatabaseName("IX_Customers_IsActive");
            b.HasIndex("EntityType").HasDatabaseName("IX_Customers_EntityType");
            b.ToTable("tbl_Customers", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.EmployeeAccount", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("EmployeeId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("AccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("EmployeeId").IsUnique().HasDatabaseName("UX_EmployeeAccounts_EmployeeId");
            b.HasIndex("AccountId").IsUnique().HasDatabaseName("UX_EmployeeAccounts_AccountId");
            b.ToTable("tbl_EmployeeAccounts", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ExchangeRate", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("CurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("RateDate").IsRequired().HasColumnType("date");
            b.Property<decimal>("Rate").IsRequired().HasColumnType("decimal(19,8)");
            b.Property<byte>("RateType").IsRequired().HasColumnType("tinyint");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("CurrencyId", "RateDate", "RateType").IsUnique().HasDatabaseName("UX_ExchangeRates_Currency_Date_Type");
            b.ToTable("tbl_ExchangeRates", "dbo", t =>
            {
                t.HasCheckConstraint("CK_ExchangeRates_Rate_Positive", "[Rate] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Expense", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("ExpenseNumber").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<DateOnly>("ExpenseDate").IsRequired().HasColumnType("date");
            b.Property<Guid>("ExpenseTypeId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("ExpenseAccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("Beneficiary").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<decimal>("Amount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<byte>("PaymentMethod").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("CashAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("BankAccountId").HasColumnType("uniqueidentifier");
            b.Property<string>("Description").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier");
            b.Property<DateTime?>("PostedAtUtc").HasColumnType("datetime2(3)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("ExpenseNumber").IsUnique().HasDatabaseName("UX_Expenses_ExpenseNumber");
            b.HasIndex("ExpenseDate").HasDatabaseName("IX_Expenses_ExpenseDate");
            b.HasIndex("ExpenseTypeId").HasDatabaseName("IX_Expenses_ExpenseTypeId");
            b.HasIndex("ExpenseAccountId").HasDatabaseName("IX_Expenses_ExpenseAccountId");
            b.HasIndex("CashAccountId").HasDatabaseName("IX_Expenses_CashAccountId");
            b.HasIndex("BankAccountId").HasDatabaseName("IX_Expenses_BankAccountId");
            b.HasIndex("JournalEntryId").HasDatabaseName("IX_Expenses_JournalEntryId");
            b.HasIndex("Status").HasDatabaseName("IX_Expenses_Status");
            b.ToTable("tbl_Expenses", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ExpenseType", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("Code").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NameEn").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<Guid?>("DefaultExpenseAccountId").HasColumnType("uniqueidentifier");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_ExpenseTypes_Code");
            b.HasIndex("DefaultExpenseAccountId").HasDatabaseName("IX_ExpenseTypes_DefaultExpenseAccountId");
            b.ToTable("tbl_ExpenseTypes", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.FiscalPeriod", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("FiscalYearId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<byte>("PeriodNumber").IsRequired().HasColumnType("tinyint");
            b.Property<string>("Name").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<DateOnly>("StartDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly>("EndDate").IsRequired().HasColumnType("date");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<bool>("SalesLocked").IsRequired().HasColumnType("bit");
            b.Property<bool>("InventoryLocked").IsRequired().HasColumnType("bit");
            b.Property<bool>("AccountingLocked").IsRequired().HasColumnType("bit");
            b.Property<DateTime?>("ClosedAtUtc").HasColumnType("datetime2(3)");
            b.Property<Guid?>("ClosedBy").HasColumnType("uniqueidentifier");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("FiscalYearId", "PeriodNumber").IsUnique().HasDatabaseName("UX_FiscalPeriods_FiscalYearId_PeriodNumber");
            b.ToTable("tbl_FiscalPeriods", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.FiscalYear", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("Code").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<string>("Name").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<DateOnly>("StartDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly>("EndDate").IsRequired().HasColumnType("date");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<DateTime?>("ClosedAtUtc").HasColumnType("datetime2(3)");
            b.Property<Guid?>("ClosedBy").HasColumnType("uniqueidentifier");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_FiscalYears_Code");
            b.ToTable("tbl_FiscalYears", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntry", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("JournalNumber").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<byte>("JournalType").IsRequired().HasColumnType("tinyint");
            b.Property<DateOnly>("PostingDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly>("DocumentDate").IsRequired().HasColumnType("date");
            b.Property<Guid>("FiscalPeriodId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("Description").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("SourceModule").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("SourceDocumentType").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid?>("SourceDocumentId").HasColumnType("uniqueidentifier");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("ReversedJournalId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("BaseCurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("BaseCurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<byte?>("BaseCurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<Guid?>("ApprovedBy").HasColumnType("uniqueidentifier");
            b.Property<DateTime?>("ApprovedAtUtc").HasColumnType("datetime2(3)");
            b.Property<Guid?>("PostedBy").HasColumnType("uniqueidentifier");
            b.Property<DateTime?>("PostedAtUtc").HasColumnType("datetime2(3)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("JournalNumber").IsUnique().HasDatabaseName("UX_JournalEntries_JournalNumber");
            b.HasIndex("FiscalPeriodId").HasDatabaseName("IX_JournalEntries_FiscalPeriodId");
            b.HasIndex("PostingDate").HasDatabaseName("IX_JournalEntries_PostingDate");
            b.HasIndex("Status").HasDatabaseName("IX_JournalEntries_Status");
            b.HasIndex("ReversedJournalId").HasDatabaseName("IX_JournalEntries_ReversedJournalId");
            b.HasIndex("BaseCurrencyId").HasDatabaseName("IX_JournalEntries_BaseCurrencyId");
            b.ToTable("tbl_JournalEntries", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntryLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("JournalEntryId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineNumber").IsRequired().HasColumnType("int");
            b.Property<Guid>("AccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("DebitAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("CreditAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<Guid?>("TransactionCurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("TransactionCurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<byte?>("TransactionCurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<decimal?>("TransactionDebitAmount").HasColumnType("decimal(19,4)");
            b.Property<decimal?>("TransactionCreditAmount").HasColumnType("decimal(19,4)");
            b.Property<decimal?>("ExchangeRate").HasColumnType("decimal(19,8)");
            b.Property<DateOnly?>("ExchangeRateDate").HasColumnType("date");
            b.Property<byte?>("ExchangeRateType").HasColumnType("tinyint");
            b.Property<byte?>("ExchangeRateSource").HasColumnType("tinyint");
            b.Property<Guid?>("SourceDocumentLineId").HasColumnType("uniqueidentifier");
            b.Property<string>("Description").HasMaxLength(300).HasColumnType("nvarchar(300)");
            b.Property<Guid?>("CustomerId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("SupplierId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<string>("PartyNameSnapshot").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<Guid?>("CostCenterId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("ProductVariantId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("WarehouseId").HasColumnType("uniqueidentifier");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("JournalEntryId", "LineNumber").IsUnique().HasDatabaseName("UX_JournalEntryLines_Journal_LineNumber");
            b.HasIndex("AccountId").HasDatabaseName("IX_JournalEntryLines_AccountId");
            b.HasIndex("TransactionCurrencyId").HasDatabaseName("IX_JournalEntryLines_TransactionCurrencyId");
            b.HasIndex("CostCenterId").HasDatabaseName("IX_JournalEntryLines_CostCenterId");
            b.HasIndex("CustomerId").HasDatabaseName("IX_JournalEntryLines_CustomerId");
            b.HasIndex("SupplierId").HasDatabaseName("IX_JournalEntryLines_SupplierId");
            b.HasIndex("EmployeeId").HasDatabaseName("IX_JournalEntryLines_EmployeeId");
            b.HasIndex("SourceDocumentLineId").HasDatabaseName("IX_JournalEntryLines_SourceDocumentLineId");
            b.ToTable("tbl_JournalEntryLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_JournalEntryLines_BaseDirection", "([DebitAmount] > 0 AND [CreditAmount] = 0) OR ([CreditAmount] > 0 AND [DebitAmount] = 0)");
                t.HasCheckConstraint("CK_JournalEntryLines_ExchangeRate", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentAllocation", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<byte>("PaymentSourceType").IsRequired().HasColumnType("tinyint");
            b.Property<Guid>("PaymentSourceId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("ReceiptVoucherLineId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("PaymentVoucherLineId").HasColumnType("uniqueidentifier");
            b.Property<byte>("TargetDocumentType").IsRequired().HasColumnType("tinyint");
            b.Property<Guid>("TargetDocumentId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<decimal>("AllocatedAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal?>("ExchangeRate").HasColumnType("decimal(19,8)");
            b.Property<decimal?>("BaseAllocatedAmount").HasColumnType("decimal(19,4)");
            b.Property<DateTime>("AllocatedAtUtc").IsRequired().HasColumnType("datetime2(3)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PaymentSourceId").HasDatabaseName("IX_PaymentAllocations_PaymentSourceId");
            b.HasIndex("ReceiptVoucherLineId").HasDatabaseName("IX_PaymentAllocations_ReceiptVoucherLineId");
            b.HasIndex("PaymentVoucherLineId").HasDatabaseName("IX_PaymentAllocations_PaymentVoucherLineId");
            b.HasIndex("TargetDocumentId").HasDatabaseName("IX_PaymentAllocations_TargetDocumentId");
            b.HasIndex("TargetDocumentType", "TargetDocumentId").HasDatabaseName("IX_PaymentAllocations_TargetDocument");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_PaymentAllocations_CurrencyId");
            b.ToTable("tbl_PaymentAllocations", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PaymentAllocations_Amount_Positive", "[AllocatedAmount] > 0");
                t.HasCheckConstraint("CK_PaymentAllocations_BaseAmount_Positive", "[BaseAllocatedAmount] IS NULL OR [BaseAllocatedAmount] > 0");
                t.HasCheckConstraint("CK_PaymentAllocations_ExchangeRate_Positive", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
                t.HasCheckConstraint("CK_PaymentAllocations_TypedSource", "([ReceiptVoucherLineId] IS NULL OR [PaymentVoucherLineId] IS NULL)");
            });
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucher", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("VoucherNumber").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<DateOnly>("VoucherDate").IsRequired().HasColumnType("date");
            b.Property<byte>("PartyType").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("SupplierId").HasColumnType("uniqueidentifier");
            b.Property<string>("BeneficiaryName").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<byte>("PaymentMethod").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("CashAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("BankAccountId").HasColumnType("uniqueidentifier");
            b.Property<decimal>("TotalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<Guid?>("BaseCurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("BaseCurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<byte?>("BaseCurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<decimal?>("BaseTotalAmount").HasColumnType("decimal(19,4)");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<string>("Description").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("PostedBy").HasColumnType("uniqueidentifier");
            b.Property<DateTime?>("PostedAtUtc").HasColumnType("datetime2(3)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("VoucherNumber").IsUnique().HasDatabaseName("UX_PaymentVouchers_VoucherNumber");
            b.HasIndex("VoucherDate").HasDatabaseName("IX_PaymentVouchers_VoucherDate");
            b.HasIndex("SupplierId").HasDatabaseName("IX_PaymentVouchers_SupplierId");
            b.HasIndex("CashAccountId").HasDatabaseName("IX_PaymentVouchers_CashAccountId");
            b.HasIndex("BankAccountId").HasDatabaseName("IX_PaymentVouchers_BankAccountId");
            b.HasIndex("BaseCurrencyId").HasDatabaseName("IX_PaymentVouchers_BaseCurrencyId");
            b.HasIndex("JournalEntryId").HasDatabaseName("IX_PaymentVouchers_JournalEntryId");
            b.HasIndex("Status").HasDatabaseName("IX_PaymentVouchers_Status");
            b.ToTable("tbl_PaymentVouchers", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucherLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PaymentVoucherId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineNumber").IsRequired().HasColumnType("int");
            b.Property<Guid>("AccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Amount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<byte?>("PartyType").HasColumnType("tinyint");
            b.Property<Guid?>("CustomerId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("SupplierId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<string>("PartyNameSnapshot").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<Guid?>("CounterpartyAccountId").HasColumnType("uniqueidentifier");
            b.Property<byte?>("PaymentMethod").HasColumnType("tinyint");
            b.Property<Guid?>("CashAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("BankAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("SettlementAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<string>("CurrencySymbolSnapshot").HasMaxLength(12).HasColumnType("nvarchar(12)");
            b.Property<byte?>("CurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<decimal?>("ExchangeRate").HasColumnType("decimal(19,8)");
            b.Property<DateOnly?>("ExchangeRateDate").HasColumnType("date");
            b.Property<byte?>("ExchangeRateType").HasColumnType("tinyint");
            b.Property<byte?>("ExchangeRateSource").HasColumnType("tinyint");
            b.Property<decimal?>("BaseAmount").HasColumnType("decimal(19,4)");
            b.Property<string>("ReferenceNumber").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<DateOnly?>("ReferenceDate").HasColumnType("date");
            b.Property<string>("ReferenceType").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid?>("ReferenceId").HasColumnType("uniqueidentifier");
            b.Property<string>("Description").HasMaxLength(300).HasColumnType("nvarchar(300)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PaymentVoucherId", "LineNumber").IsUnique().HasDatabaseName("UX_PaymentVoucherLines_Voucher_LineNumber");
            b.HasIndex("AccountId").HasDatabaseName("IX_PaymentVoucherLines_AccountId");
            b.HasIndex("CustomerId").HasDatabaseName("IX_PaymentVoucherLines_CustomerId");
            b.HasIndex("SupplierId").HasDatabaseName("IX_PaymentVoucherLines_SupplierId");
            b.HasIndex("EmployeeId").HasDatabaseName("IX_PaymentVoucherLines_EmployeeId");
            b.HasIndex("CounterpartyAccountId").HasDatabaseName("IX_PaymentVoucherLines_CounterpartyAccountId");
            b.HasIndex("SettlementAccountId").HasDatabaseName("IX_PaymentVoucherLines_SettlementAccountId");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_PaymentVoucherLines_CurrencyId");
            b.ToTable("tbl_PaymentVoucherLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PaymentVoucherLines_Amount_Positive", "[Amount] > 0");
                t.HasCheckConstraint("CK_PaymentVoucherLines_ExchangeRate_Positive", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
                t.HasCheckConstraint("CK_PaymentVoucherLines_BaseAmount_Positive", "[BaseAmount] IS NULL OR [BaseAmount] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PostingProfile", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("Code").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<string>("Name").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("Module").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("DocumentType").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_PostingProfiles_Code");
            b.ToTable("tbl_PostingProfiles", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PostingProfileLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PostingProfileId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("AccountRole").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid>("AccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<bool>("IsRequired").IsRequired().HasColumnType("bit");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PostingProfileId").HasDatabaseName("IX_PostingProfileLines_PostingProfileId");
            b.HasIndex("AccountId").HasDatabaseName("IX_PostingProfileLines_AccountId");
            b.ToTable("tbl_PostingProfileLines", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucher", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("VoucherNumber").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<DateOnly>("VoucherDate").IsRequired().HasColumnType("date");
            b.Property<byte>("PartyType").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("CustomerId").HasColumnType("uniqueidentifier");
            b.Property<string>("ReceivedFrom").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<byte>("PaymentMethod").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("CashAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("BankAccountId").HasColumnType("uniqueidentifier");
            b.Property<decimal>("TotalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<Guid?>("BaseCurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("BaseCurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<byte?>("BaseCurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<decimal?>("BaseTotalAmount").HasColumnType("decimal(19,4)");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<string>("Description").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("PostedBy").HasColumnType("uniqueidentifier");
            b.Property<DateTime?>("PostedAtUtc").HasColumnType("datetime2(3)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("VoucherNumber").IsUnique().HasDatabaseName("UX_ReceiptVouchers_VoucherNumber");
            b.HasIndex("VoucherDate").HasDatabaseName("IX_ReceiptVouchers_VoucherDate");
            b.HasIndex("CustomerId").HasDatabaseName("IX_ReceiptVouchers_CustomerId");
            b.HasIndex("CashAccountId").HasDatabaseName("IX_ReceiptVouchers_CashAccountId");
            b.HasIndex("BankAccountId").HasDatabaseName("IX_ReceiptVouchers_BankAccountId");
            b.HasIndex("BaseCurrencyId").HasDatabaseName("IX_ReceiptVouchers_BaseCurrencyId");
            b.HasIndex("JournalEntryId").HasDatabaseName("IX_ReceiptVouchers_JournalEntryId");
            b.HasIndex("Status").HasDatabaseName("IX_ReceiptVouchers_Status");
            b.ToTable("tbl_ReceiptVouchers", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("ReceiptVoucherId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineNumber").IsRequired().HasColumnType("int");
            b.Property<Guid>("AccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Amount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<byte?>("PartyType").HasColumnType("tinyint");
            b.Property<Guid?>("CustomerId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("SupplierId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<string>("PartyNameSnapshot").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<Guid?>("CounterpartyAccountId").HasColumnType("uniqueidentifier");
            b.Property<byte?>("PaymentMethod").HasColumnType("tinyint");
            b.Property<Guid?>("CashAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("BankAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("SettlementAccountId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<string>("CurrencySymbolSnapshot").HasMaxLength(12).HasColumnType("nvarchar(12)");
            b.Property<byte?>("CurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<decimal?>("ExchangeRate").HasColumnType("decimal(19,8)");
            b.Property<DateOnly?>("ExchangeRateDate").HasColumnType("date");
            b.Property<byte?>("ExchangeRateType").HasColumnType("tinyint");
            b.Property<byte?>("ExchangeRateSource").HasColumnType("tinyint");
            b.Property<decimal?>("BaseAmount").HasColumnType("decimal(19,4)");
            b.Property<string>("ReferenceNumber").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<DateOnly?>("ReferenceDate").HasColumnType("date");
            b.Property<string>("ReferenceType").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid?>("ReferenceId").HasColumnType("uniqueidentifier");
            b.Property<string>("Description").HasMaxLength(300).HasColumnType("nvarchar(300)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("ReceiptVoucherId", "LineNumber").IsUnique().HasDatabaseName("UX_ReceiptVoucherLines_Voucher_LineNumber");
            b.HasIndex("AccountId").HasDatabaseName("IX_ReceiptVoucherLines_AccountId");
            b.HasIndex("CustomerId").HasDatabaseName("IX_ReceiptVoucherLines_CustomerId");
            b.HasIndex("SupplierId").HasDatabaseName("IX_ReceiptVoucherLines_SupplierId");
            b.HasIndex("EmployeeId").HasDatabaseName("IX_ReceiptVoucherLines_EmployeeId");
            b.HasIndex("CounterpartyAccountId").HasDatabaseName("IX_ReceiptVoucherLines_CounterpartyAccountId");
            b.HasIndex("SettlementAccountId").HasDatabaseName("IX_ReceiptVoucherLines_SettlementAccountId");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_ReceiptVoucherLines_CurrencyId");
            b.ToTable("tbl_ReceiptVoucherLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_ReceiptVoucherLines_Amount_Positive", "[Amount] > 0");
                t.HasCheckConstraint("CK_ReceiptVoucherLines_ExchangeRate_Positive", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
                t.HasCheckConstraint("CK_ReceiptVoucherLines_BaseAmount_Positive", "[BaseAmount] IS NULL OR [BaseAmount] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Supplier", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("SupplierCode").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<Guid>("AccountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<byte>("EntityType").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("SupplierScope").IsRequired().HasColumnType("tinyint");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NameEn").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("TradeName").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NationalId").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("CommercialRegistrationNo").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("TaxNumber").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<decimal>("CreditLimit").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<int>("PaymentTermDays").IsRequired().HasColumnType("int");
            b.Property<int?>("DefaultLeadTimeDays").HasColumnType("int");
            b.Property<DateOnly?>("SupplierSince").HasColumnType("date");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("SupplierCode").IsUnique().HasDatabaseName("UX_Suppliers_SupplierCode");
            b.HasIndex("AccountId").IsUnique().HasDatabaseName("UX_Suppliers_AccountId");
            b.HasIndex("NationalId").IsUnique().HasDatabaseName("UX_Suppliers_NationalId").HasFilter("[NationalId] IS NOT NULL");
            b.HasIndex("TaxNumber").IsUnique().HasDatabaseName("UX_Suppliers_TaxNumber").HasFilter("[TaxNumber] IS NOT NULL");
            b.HasIndex("CommercialRegistrationNo").IsUnique().HasDatabaseName("UX_Suppliers_CommercialRegistrationNo").HasFilter("[CommercialRegistrationNo] IS NOT NULL");
            b.HasIndex("NameAr").HasDatabaseName("IX_Suppliers_NameAr");
            b.HasIndex("IsActive").HasDatabaseName("IX_Suppliers_IsActive");
            b.HasIndex("EntityType").HasDatabaseName("IX_Suppliers_EntityType");
            b.HasIndex("SupplierScope").HasDatabaseName("IX_Suppliers_SupplierScope");
            b.ToTable("tbl_Suppliers", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.Brand", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Code").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("Name").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_Brands_Code");
            b.HasIndex("Name").IsUnique().HasDatabaseName("UX_Brands_Name");
            b.HasIndex("IsActive").HasDatabaseName("IX_Brands_IsActive");
            b.ToTable("tbl_Brands");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.FrameDetails", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("Model").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Material").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("RimType").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("Gender").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("Shape").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<decimal?>("TempleLength").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("BridgeSize").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("LensWidth").HasColumnType("decimal(6,2)");
            b.HasKey("Id");
            b.HasIndex("ProductId").IsUnique().HasDatabaseName("UX_FrameDetails_ProductId");
            b.ToTable("tbl_FrameDetails");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryBalance", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<Guid>("WarehouseId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("OnHandQuantity").HasColumnType("decimal(18,3)");
            b.Property<decimal>("ReservedQuantity").HasColumnType("decimal(18,3)");
            b.Property<decimal>("OnOrderQuantity").HasColumnType("decimal(18,3)");
            b.Property<decimal>("AverageUnitCost").HasColumnType("decimal(18,2)");
            b.Property<decimal>("InventoryValue").HasColumnType("decimal(18,2)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<DateTimeOffset?>("LastMovementAtUtc").HasColumnType("datetimeoffset");
            b.HasKey("Id");
            b.HasIndex("WarehouseId", "ProductVariantId").IsUnique().HasDatabaseName("UX_InventoryBalances_Warehouse_ProductVariant");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_InventoryBalances_ProductVariantId");
            b.ToTable("tbl_InventoryBalances");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryLedger", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<long>("SequenceNumber").IsRequired().HasColumnType("bigint");
            b.Property<Guid>("TransactionId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("TransactionLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("WarehouseId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("MovementType").IsRequired().HasColumnType("int");
            b.Property<decimal>("QuantityIn").HasColumnType("decimal(18,3)");
            b.Property<decimal>("QuantityOut").HasColumnType("decimal(18,3)");
            b.Property<decimal>("BalanceAfter").HasColumnType("decimal(18,3)");
            b.Property<decimal>("UnitCost").HasColumnType("decimal(18,2)");
            b.Property<decimal>("AverageCostAfter").HasColumnType("decimal(18,2)");
            b.Property<decimal>("InventoryValueAfter").HasColumnType("decimal(18,2)");
            b.Property<DateTimeOffset>("MovementDate").IsRequired().HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.HasKey("Id");
            b.HasIndex("SequenceNumber").IsUnique().HasDatabaseName("UX_InventoryLedger_SequenceNumber");
            b.HasIndex("WarehouseId", "ProductVariantId", "MovementDate").HasDatabaseName("IX_InventoryLedger_Warehouse_ProductVariant_Date");
            b.HasIndex("TransactionId").HasDatabaseName("IX_InventoryLedger_TransactionId");
            b.HasIndex("TransactionLineId").HasDatabaseName("IX_InventoryLedger_TransactionLineId");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_InventoryLedger_ProductVariantId");
            b.ToTable("tbl_InventoryLedger");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryTransaction", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("TransactionNumber").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<int>("TransactionType").IsRequired().HasColumnType("int");
            b.Property<Guid?>("SourceWarehouseId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("DestinationWarehouseId").HasColumnType("uniqueidentifier");
            b.Property<int>("Status").IsRequired().HasColumnType("int");
            b.Property<DateTimeOffset>("TransactionDate").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("ReferenceType").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<Guid?>("ReferenceId").HasColumnType("uniqueidentifier");
            b.Property<string>("Reason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<DateTimeOffset?>("PostedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("PostedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id");
            b.HasIndex("TransactionNumber").IsUnique().HasDatabaseName("UX_InventoryTransactions_TransactionNumber");
            b.HasIndex("TransactionDate").HasDatabaseName("IX_InventoryTransactions_TransactionDate");
            b.HasIndex("Status").HasDatabaseName("IX_InventoryTransactions_Status");
            b.HasIndex("SourceWarehouseId").HasDatabaseName("IX_InventoryTransactions_SourceWarehouseId");
            b.HasIndex("DestinationWarehouseId").HasDatabaseName("IX_InventoryTransactions_DestinationWarehouseId");
            b.HasIndex("ReferenceType", "ReferenceId").HasDatabaseName("IX_InventoryTransactions_Reference");
            b.ToTable("tbl_InventoryTransactions");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryTransactionLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<Guid>("TransactionId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Quantity").HasColumnType("decimal(18,3)");
            b.Property<decimal>("UnitCost").HasColumnType("decimal(18,2)");
            b.Property<decimal>("TotalCost").HasColumnType("decimal(18,2)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.HasKey("Id");
            b.HasIndex("TransactionId").HasDatabaseName("IX_InventoryTransactionLines_TransactionId");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_InventoryTransactionLines_ProductVariantId");
            b.ToTable("tbl_InventoryTransactionLines");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.LensDetails", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("LensType").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Material").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Coating").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<decimal?>("RefractiveIndex").HasColumnType("decimal(5,3)");
            b.Property<decimal?>("SphereMin").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("SphereMax").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("CylinderMin").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("CylinderMax").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("AddMin").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("AddMax").HasColumnType("decimal(6,2)");
            b.Property<bool>("IsPrescriptionLens").IsRequired().HasColumnType("bit");
            b.HasKey("Id");
            b.HasIndex("ProductId").IsUnique().HasDatabaseName("UX_LensDetails_ProductId");
            b.ToTable("tbl_LensDetails");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.ProductCategory", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Code").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("NameEn").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<Guid?>("ParentCategoryId").HasColumnType("uniqueidentifier");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_ProductCategories_Code");
            b.HasIndex("ParentCategoryId").HasDatabaseName("IX_ProductCategories_ParentCategoryId");
            b.HasIndex("IsActive").HasDatabaseName("IX_ProductCategories_IsActive");
            b.ToTable("tbl_ProductCategories");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.Product", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ProductCode").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<string>("NameEn").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<Guid>("CategoryId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("BrandId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductTypeId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("Description").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<bool>("IsStockItem").IsRequired().HasColumnType("bit");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id");
            b.HasIndex("ProductCode").IsUnique().HasDatabaseName("UX_Products_ProductCode");
            b.HasIndex("CategoryId").HasDatabaseName("IX_Products_CategoryId");
            b.HasIndex("BrandId").HasDatabaseName("IX_Products_BrandId");
            b.HasIndex("ProductTypeId").HasDatabaseName("IX_Products_ProductTypeId");
            b.HasIndex("IsActive").HasDatabaseName("IX_Products_IsActive");
            b.ToTable("tbl_Products");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.ProductType", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Code").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("NameEn").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("SystemKey").HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_productTypes_Code");
            b.HasIndex("IsActive").HasDatabaseName("IX_productTypes_IsActive");
            b.HasIndex("SystemKey").IsUnique().HasDatabaseName("UX_productTypes_SystemKey").HasFilter("[SystemKey] IS NOT NULL");
            b.ToTable("tbl_productTypes");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.ProductVariant", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<Guid>("ProductId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("SKU").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Barcode").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("VariantName").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Color").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("Size").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid?>("UnitId").HasColumnType("uniqueidentifier");
            b.Property<decimal>("PurchasePrice").HasColumnType("decimal(18,2)");
            b.Property<decimal>("SellingPrice").HasColumnType("decimal(18,2)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id");
            b.HasIndex("SKU").IsUnique().HasDatabaseName("UX_ProductVariants_SKU");
            b.HasIndex("Barcode").IsUnique().HasDatabaseName("UX_ProductVariants_Barcode").HasFilter("[Barcode] IS NOT NULL");
            b.HasIndex("ProductId").HasDatabaseName("IX_ProductVariants_ProductId");
            b.HasIndex("IsActive").HasDatabaseName("IX_ProductVariants_IsActive");
            b.ToTable("tbl_ProductVariants");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.StockCount", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CountNumber").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<Guid>("WarehouseId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("Status").IsRequired().HasColumnType("int");
            b.Property<DateOnly>("CountDate").IsRequired().HasColumnType("date");
            b.Property<DateTimeOffset?>("StartedAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("CompletedAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("PostedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("PostedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id");
            b.HasIndex("CountNumber").IsUnique().HasDatabaseName("UX_StockCounts_CountNumber");
            b.HasIndex("WarehouseId").HasDatabaseName("IX_StockCounts_WarehouseId");
            b.HasIndex("Status").HasDatabaseName("IX_StockCounts_Status");
            b.HasIndex("CountDate").HasDatabaseName("IX_StockCounts_CountDate");
            b.ToTable("tbl_StockCounts");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.StockCountLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<Guid>("StockCountId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("SystemQuantity").HasColumnType("decimal(18,3)");
            b.Property<decimal>("CountedQuantity").HasColumnType("decimal(18,3)");
            b.Property<decimal>("DifferenceQuantity").HasColumnType("decimal(18,3)");
            b.Property<decimal>("AverageCostSnapshot").HasColumnType("decimal(18,2)");
            b.Property<decimal>("VarianceValue").HasColumnType("decimal(18,2)");
            b.Property<DateTimeOffset?>("CountedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CountedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id");
            b.HasIndex("StockCountId", "ProductVariantId").IsUnique().HasDatabaseName("UX_StockCountLines_StockCount_ProductVariant");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_StockCountLines_ProductVariantId");
            b.ToTable("tbl_StockCountLines");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.StockReservation", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("WarehouseId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Quantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<string>("SourceModule").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("SourceDocumentType").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid>("SourceDocumentId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("SourceLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<DateTimeOffset>("ReservedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("ReleasedAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("ConsumedAtUtc").HasColumnType("datetimeoffset");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("WarehouseId", "ProductVariantId", "Status").HasDatabaseName("IX_StockReservations_Warehouse_Product_Status");
            b.HasIndex("SourceModule", "SourceDocumentType", "SourceDocumentId").HasDatabaseName("IX_StockReservations_Source");
            b.HasIndex("SourceLineId").HasDatabaseName("IX_StockReservations_SourceLineId");
            b.ToTable("tbl_StockReservations", "dbo", t =>
            {
                t.HasCheckConstraint("CK_StockReservations_Quantity_Positive", "[Quantity] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.Unit", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Code").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("NameEn").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_Units_Code");
            b.HasIndex("IsActive").HasDatabaseName("IX_Units_IsActive");
            b.ToTable("tbl_Units");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.Warehouse", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Code").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("NameEn").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Description").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<bool>("IsDefault").IsRequired().HasColumnType("bit");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_Warehouses_Code");
            b.HasIndex("IsActive").HasDatabaseName("IX_Warehouses_IsActive");
            b.HasIndex("IsDefault").IsUnique().HasDatabaseName("UX_Warehouses_ActiveDefault").HasFilter("[IsDefault] = 1 AND [IsActive] = 1");
            b.ToTable("tbl_Warehouses");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerOrder", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("OrderCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("CustomerId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("PrescriptionRevisionId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("OrderDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly?>("RequiredDate").HasColumnType("date");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<Guid>("CurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencyCodeSnapshot").IsRequired().HasMaxLength(10).HasColumnType("nvarchar(10)");
            b.Property<string>("CurrencySymbolSnapshot").HasMaxLength(10).HasColumnType("nvarchar(10)");
            b.Property<byte>("CurrencyDecimalPlacesSnapshot").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("ExchangeRate").IsRequired().HasColumnType("decimal(19,8)");
            b.Property<DateOnly>("ExchangeRateDate").IsRequired().HasColumnType("date");
            b.Property<byte>("ExchangeRateType").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("ExchangeRateSource").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("TaxCalculationMode").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("PaymentTermType").IsRequired().HasColumnType("tinyint");
            b.Property<int>("PaymentTermDaysSnapshot").IsRequired().HasColumnType("int");
            b.Property<decimal>("Subtotal").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("DiscountAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TotalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<DateTimeOffset?>("ConfirmedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ConfirmedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("CancelledAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("OrderCode").IsUnique().HasDatabaseName("UX_CustomerOrders_OrderCode");
            b.HasIndex("CustomerId").HasDatabaseName("IX_CustomerOrders_CustomerId");
            b.HasIndex("OrderDate").HasDatabaseName("IX_CustomerOrders_OrderDate");
            b.HasIndex("Status").HasDatabaseName("IX_CustomerOrders_Status");
            b.HasIndex("PrescriptionRevisionId").HasDatabaseName("IX_CustomerOrders_PrescriptionRevisionId");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_CustomerOrders_CurrencyId");
            b.ToTable("tbl_CustomerOrders", "dbo", t =>
            {
                t.HasCheckConstraint("CK_CustomerOrders_ExchangeRate_Positive", "[ExchangeRate] > 0");
                t.HasCheckConstraint("CK_CustomerOrders_PaymentTermDays_NonNegative", "[PaymentTermDaysSnapshot] >= 0");
                t.HasCheckConstraint("CK_CustomerOrders_Totals_NonNegative", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerOrderLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("CustomerOrderId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineNumber").IsRequired().HasColumnType("int");
            b.Property<Guid?>("GroupId").HasColumnType("uniqueidentifier");
            b.Property<byte>("LineType").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("ProductVariantId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("WarehouseId").HasColumnType("uniqueidentifier");
            b.Property<string>("DescriptionSnapshot").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<decimal>("Quantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("BaseUnitPrice").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("ActualUnitPrice").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<byte>("DiscountType").IsRequired().HasColumnType("tinyint");
            b.Property<decimal?>("DiscountValue").HasColumnType("decimal(19,4)");
            b.Property<decimal>("DiscountAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal?>("TaxRate").HasColumnType("decimal(9,4)");
            b.Property<decimal>("TaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("NetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("FinalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<Guid?>("PrescriptionRevisionId").HasColumnType("uniqueidentifier");
            b.Property<byte?>("PrescriptionEye").HasColumnType("tinyint");
            b.Property<bool>("RequiresProduction").IsRequired().HasColumnType("bit");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("CustomerOrderId", "LineNumber").IsUnique().HasDatabaseName("UX_CustomerOrderLines_Order_LineNumber");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_CustomerOrderLines_ProductVariantId");
            b.HasIndex("WarehouseId").HasDatabaseName("IX_CustomerOrderLines_WarehouseId");
            b.HasIndex("PrescriptionRevisionId").HasDatabaseName("IX_CustomerOrderLines_PrescriptionRevisionId");
            b.HasIndex("GroupId").HasDatabaseName("IX_CustomerOrderLines_GroupId");
            b.ToTable("tbl_CustomerOrderLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_CustomerOrderLines_Quantity_Positive", "[Quantity] > 0");
                t.HasCheckConstraint("CK_CustomerOrderLines_BaseUnitPrice_NonNegative", "[BaseUnitPrice] >= 0");
                t.HasCheckConstraint("CK_CustomerOrderLines_ActualUnitPrice_NonNegative", "[ActualUnitPrice] >= 0");
                t.HasCheckConstraint("CK_CustomerOrderLines_DiscountAmount_NonNegative", "[DiscountAmount] >= 0");
                t.HasCheckConstraint("CK_CustomerOrderLines_DiscountWithinGross", "[DiscountAmount] <= ([Quantity] * [ActualUnitPrice])");
                t.HasCheckConstraint("CK_CustomerOrderLines_TaxAmount_NonNegative", "[TaxAmount] >= 0");
                t.HasCheckConstraint("CK_CustomerOrderLines_NetAmount_NonNegative", "[NetAmount] >= 0");
                t.HasCheckConstraint("CK_CustomerOrderLines_FinalAmount_NonNegative", "[FinalAmount] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.Prescription", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("PrescriptionCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("CustomerId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("PrescriptionDate").IsRequired().HasColumnType("date");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<string>("PrescribedBy").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("ClinicName").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PrescriptionCode").IsUnique().HasDatabaseName("UX_Prescriptions_PrescriptionCode");
            b.HasIndex("CustomerId").HasDatabaseName("IX_Prescriptions_CustomerId");
            b.HasIndex("PrescriptionDate").HasDatabaseName("IX_Prescriptions_PrescriptionDate");
            b.HasIndex("Status").HasDatabaseName("IX_Prescriptions_Status");
            b.ToTable("tbl_Prescriptions", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.PrescriptionEyeDetail", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PrescriptionRevisionId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<byte>("Eye").IsRequired().HasColumnType("tinyint");
            b.Property<decimal?>("SPH").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("CYL").HasColumnType("decimal(6,2)");
            b.Property<short?>("Axis").HasColumnType("smallint");
            b.Property<decimal?>("ADD").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("Prism").HasColumnType("decimal(6,2)");
            b.Property<byte?>("PrismBase").HasColumnType("tinyint");
            b.Property<decimal?>("PD").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("MonocularPD").HasColumnType("decimal(6,2)");
            b.Property<string>("VA").HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<decimal?>("FittingHeight").HasColumnType("decimal(6,2)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PrescriptionRevisionId", "Eye").IsUnique().HasDatabaseName("UX_PrescriptionEyeDetails_Revision_Eye");
            b.ToTable("tbl_PrescriptionEyeDetails", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PrescriptionEyeDetails_Axis", "[Axis] IS NULL OR ([Axis] >= 0 AND [Axis] <= 180)");
                t.HasCheckConstraint("CK_PrescriptionEyeDetails_ADD", "[ADD] IS NULL OR [ADD] >= 0");
                t.HasCheckConstraint("CK_PrescriptionEyeDetails_Prism", "[Prism] IS NULL OR [Prism] >= 0");
                t.HasCheckConstraint("CK_PrescriptionEyeDetails_PD", "[PD] IS NULL OR [PD] > 0");
                t.HasCheckConstraint("CK_PrescriptionEyeDetails_MonocularPD", "[MonocularPD] IS NULL OR [MonocularPD] > 0");
                t.HasCheckConstraint("CK_PrescriptionEyeDetails_FittingHeight", "[FittingHeight] IS NULL OR [FittingHeight] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.PrescriptionRevision", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PrescriptionId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("RevisionNumber").IsRequired().HasColumnType("int");
            b.Property<DateOnly>("EffectiveDate").IsRequired().HasColumnType("date");
            b.Property<string>("Reason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<bool>("IsCurrent").IsRequired().HasColumnType("bit");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PrescriptionId", "RevisionNumber").IsUnique().HasDatabaseName("UX_PrescriptionRevisions_Prescription_Revision");
            b.HasIndex("PrescriptionId").IsUnique().HasDatabaseName("UX_PrescriptionRevisions_Current").HasFilter("[IsCurrent] = 1");
            b.ToTable("tbl_PrescriptionRevisions", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PrescriptionRevisions_RevisionNumber_Positive", "[RevisionNumber] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoice", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("InvoiceCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("CustomerId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CustomerOrderId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("PrescriptionRevisionId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("InvoiceDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly>("PostingDate").IsRequired().HasColumnType("date");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<Guid>("CurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencyCodeSnapshot").IsRequired().HasMaxLength(10).HasColumnType("nvarchar(10)");
            b.Property<string>("CurrencySymbolSnapshot").HasMaxLength(10).HasColumnType("nvarchar(10)");
            b.Property<byte>("CurrencyDecimalPlacesSnapshot").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("ExchangeRate").IsRequired().HasColumnType("decimal(19,8)");
            b.Property<DateOnly>("ExchangeRateDate").IsRequired().HasColumnType("date");
            b.Property<byte>("ExchangeRateType").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("ExchangeRateSource").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("TaxCalculationMode").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("PaymentTermType").IsRequired().HasColumnType("tinyint");
            b.Property<int>("PaymentTermDaysSnapshot").IsRequired().HasColumnType("int");
            b.Property<DateOnly?>("DueDate").HasColumnType("date");
            b.Property<Guid>("BaseCurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("BaseCurrencyCodeSnapshot").IsRequired().HasMaxLength(10).HasColumnType("nvarchar(10)");
            b.Property<byte>("BaseCurrencyDecimalPlacesSnapshot").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("Subtotal").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("DiscountAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TotalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseSubtotal").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseDiscountAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseTaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseTotalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<string>("Description").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("ConfirmedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ConfirmedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("PostedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("PostedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("CancelledAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("InvoiceCode").IsUnique().HasDatabaseName("UX_SalesInvoices_InvoiceCode");
            b.HasIndex("CustomerId").HasDatabaseName("IX_SalesInvoices_CustomerId");
            b.HasIndex("InvoiceDate").HasDatabaseName("IX_SalesInvoices_InvoiceDate");
            b.HasIndex("PostingDate").HasDatabaseName("IX_SalesInvoices_PostingDate");
            b.HasIndex("Status").HasDatabaseName("IX_SalesInvoices_Status");
            b.HasIndex("CustomerOrderId").HasDatabaseName("IX_SalesInvoices_CustomerOrderId");
            b.HasIndex("JournalEntryId").HasDatabaseName("IX_SalesInvoices_JournalEntryId");
            b.HasIndex("PrescriptionRevisionId").HasDatabaseName("IX_SalesInvoices_PrescriptionRevisionId");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_SalesInvoices_CurrencyId");
            b.HasIndex("BaseCurrencyId").HasDatabaseName("IX_SalesInvoices_BaseCurrencyId");
            b.ToTable("tbl_SalesInvoices", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SalesInvoices_ExchangeRate_Positive", "[ExchangeRate] > 0");
                t.HasCheckConstraint("CK_SalesInvoices_PaymentTermDays_NonNegative", "[PaymentTermDaysSnapshot] >= 0");
                t.HasCheckConstraint("CK_SalesInvoices_Totals_NonNegative", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0");
                t.HasCheckConstraint("CK_SalesInvoices_BaseTotals_NonNegative", "[BaseSubtotal] >= 0 AND [BaseDiscountAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoiceLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("SalesInvoiceId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineNumber").IsRequired().HasColumnType("int");
            b.Property<Guid?>("CustomerOrderLineId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("GroupId").HasColumnType("uniqueidentifier");
            b.Property<byte>("LineType").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("ProductVariantId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("WarehouseId").HasColumnType("uniqueidentifier");
            b.Property<string>("ProductCodeSnapshot").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("ProductNameSnapshot").IsRequired().HasMaxLength(250).HasColumnType("nvarchar(250)");
            b.Property<string>("DescriptionSnapshot").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("UnitSnapshot").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<decimal>("Quantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("BaseUnitPrice").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("ActualUnitPrice").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<byte>("DiscountType").IsRequired().HasColumnType("tinyint");
            b.Property<decimal?>("DiscountValue").HasColumnType("decimal(19,4)");
            b.Property<decimal>("DiscountAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal?>("TaxRate").HasColumnType("decimal(9,4)");
            b.Property<decimal>("TaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("NetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("FinalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseNetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseTaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseFinalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal?>("UnitCostSnapshot").HasColumnType("decimal(19,4)");
            b.Property<decimal?>("TotalCostSnapshot").HasColumnType("decimal(19,4)");
            b.Property<Guid?>("PrescriptionRevisionId").HasColumnType("uniqueidentifier");
            b.Property<byte?>("PrescriptionEye").HasColumnType("tinyint");
            b.Property<bool>("RequiresProduction").IsRequired().HasColumnType("bit");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("SalesInvoiceId", "LineNumber").IsUnique().HasDatabaseName("UX_SalesInvoiceLines_Invoice_LineNumber");
            b.HasIndex("SalesInvoiceId").HasDatabaseName("IX_SalesInvoiceLines_SalesInvoiceId");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_SalesInvoiceLines_ProductVariantId");
            b.HasIndex("WarehouseId").HasDatabaseName("IX_SalesInvoiceLines_WarehouseId");
            b.HasIndex("CustomerOrderLineId").HasDatabaseName("IX_SalesInvoiceLines_CustomerOrderLineId");
            b.HasIndex("PrescriptionRevisionId").HasDatabaseName("IX_SalesInvoiceLines_PrescriptionRevisionId");
            b.HasIndex("GroupId").HasDatabaseName("IX_SalesInvoiceLines_GroupId");
            b.ToTable("tbl_SalesInvoiceLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SalesInvoiceLines_Quantity_Positive", "[Quantity] > 0");
                t.HasCheckConstraint("CK_SalesInvoiceLines_BaseUnitPrice_NonNegative", "[BaseUnitPrice] >= 0");
                t.HasCheckConstraint("CK_SalesInvoiceLines_ActualUnitPrice_NonNegative", "[ActualUnitPrice] >= 0");
                t.HasCheckConstraint("CK_SalesInvoiceLines_DiscountAmount_NonNegative", "[DiscountAmount] >= 0");
                t.HasCheckConstraint("CK_SalesInvoiceLines_DiscountWithinGross", "[DiscountAmount] <= ([Quantity] * [ActualUnitPrice])");
                t.HasCheckConstraint("CK_SalesInvoiceLines_TaxAmount_NonNegative", "[TaxAmount] >= 0");
                t.HasCheckConstraint("CK_SalesInvoiceLines_NetAmount_NonNegative", "[NetAmount] >= 0");
                t.HasCheckConstraint("CK_SalesInvoiceLines_FinalAmount_NonNegative", "[FinalAmount] >= 0");
                t.HasCheckConstraint("CK_SalesInvoiceLines_BaseAmounts_NonNegative", "[BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseFinalAmount] >= 0");
                t.HasCheckConstraint("CK_SalesInvoiceLines_CostSnapshots_NonNegative", "([UnitCostSnapshot] IS NULL OR [UnitCostSnapshot] >= 0) AND ([TotalCostSnapshot] IS NULL OR [TotalCostSnapshot] >= 0)");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoiceLinePrescriptionSnapshot", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("SalesInvoiceLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("PrescriptionRevisionId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<byte>("Eye").IsRequired().HasColumnType("tinyint");
            b.Property<decimal?>("SPH").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("CYL").HasColumnType("decimal(6,2)");
            b.Property<short?>("Axis").HasColumnType("smallint");
            b.Property<decimal?>("ADD").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("Prism").HasColumnType("decimal(6,2)");
            b.Property<byte?>("PrismBase").HasColumnType("tinyint");
            b.Property<decimal?>("PD").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("MonocularPD").HasColumnType("decimal(6,2)");
            b.Property<string>("VA").HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<decimal?>("FittingHeight").HasColumnType("decimal(6,2)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("SalesInvoiceLineId").IsUnique().HasDatabaseName("UX_SalesInvoicePrescriptionSnapshots_LineId");
            b.HasIndex("PrescriptionRevisionId").HasDatabaseName("IX_SalesInvoicePrescriptionSnapshots_PrescriptionRevisionId");
            b.ToTable("tbl_SalesInvoiceLinePrescriptionSnapshots", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_Axis", "[Axis] IS NULL OR ([Axis] >= 0 AND [Axis] <= 180)");
                t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_ADD", "[ADD] IS NULL OR [ADD] >= 0");
                t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_Prism", "[Prism] IS NULL OR [Prism] >= 0");
                t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_PD", "[PD] IS NULL OR [PD] > 0");
                t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_MonocularPD", "[MonocularPD] IS NULL OR [MonocularPD] > 0");
                t.HasCheckConstraint("CK_SalesInvoicePrescriptionSnapshots_FittingHeight", "[FittingHeight] IS NULL OR [FittingHeight] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesPriceOverride", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("SalesInvoiceId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("SalesInvoiceLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("OriginalPrice").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("OverridePrice").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<string>("Reason").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<string>("RequestedBy").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset>("RequestedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("SalesInvoiceId").HasDatabaseName("IX_SalesPriceOverrides_SalesInvoiceId");
            b.HasIndex("SalesInvoiceLineId").HasDatabaseName("IX_SalesPriceOverrides_SalesInvoiceLineId");
            b.HasIndex("SalesInvoiceLineId", "Status").HasDatabaseName("IX_SalesPriceOverrides_Line_Status");
            b.ToTable("tbl_SalesPriceOverrides", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SalesPriceOverrides_Prices_NonNegative", "[OriginalPrice] >= 0 AND [OverridePrice] >= 0");
                t.HasCheckConstraint("CK_SalesPriceOverrides_Prices_Different", "[OriginalPrice] <> [OverridePrice]");
            });
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

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Account", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("ParentAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Accounts_ParentAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.AccountingSettings", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("BaseCurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_AccountingSettings_BaseCurrency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.AccountingSettings", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("EmployeeParentAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AccountingSettings_EmployeeParentAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.AccountingSettings", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("CashParentAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AccountingSettings_CashParentAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.AccountingSettings", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("BankParentAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AccountingSettings_BankParentAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.AccountingSettings", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("ExchangeGainAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AccountingSettings_ExchangeGainAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.AccountingSettings", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("ExchangeLossAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AccountingSettings_ExchangeLossAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.BankAccount", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_BankAccounts_Account");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.BankAccount", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_BankAccounts_Currency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CashAccount", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_CashAccounts_Account");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CashAccount", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CashAccounts_Currency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CashShift", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.CashAccount", null).WithMany()
                .HasForeignKey("CashAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_CashShifts_CashAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CostCenter", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.CostCenter", null).WithMany()
                .HasForeignKey("ParentCostCenterId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CostCenters_ParentCostCenter");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Customer", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_Customers_Account");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.EmployeeAccount", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany()
                .HasForeignKey("EmployeeId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_EmployeeAccounts_Employee");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.EmployeeAccount", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_EmployeeAccounts_Account");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ExchangeRate", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_ExchangeRates_Currency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Expense", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.ExpenseType", null).WithMany()
                .HasForeignKey("ExpenseTypeId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_Expenses_ExpenseType");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Expense", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("ExpenseAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_Expenses_ExpenseAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Expense", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.CashAccount", null).WithMany()
                .HasForeignKey("CashAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Expenses_CashAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Expense", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.BankAccount", null).WithMany()
                .HasForeignKey("BankAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Expenses_BankAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Expense", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany()
                .HasForeignKey("JournalEntryId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Expenses_JournalEntry");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ExpenseType", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("DefaultExpenseAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ExpenseTypes_DefaultExpenseAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.FiscalPeriod", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.FiscalYear", null).WithMany()
                .HasForeignKey("FiscalYearId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_FiscalPeriods_FiscalYear");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntry", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.FiscalPeriod", null).WithMany()
                .HasForeignKey("FiscalPeriodId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_JournalEntries_FiscalPeriod");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntry", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("BaseCurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_JournalEntries_BaseCurrency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntry", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany()
                .HasForeignKey("ReversedJournalId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_JournalEntries_ReversedJournal");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntryLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_JournalEntryLines_Account");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntryLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("TransactionCurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_JournalEntryLines_TransactionCurrency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntryLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_JournalEntryLines_Customer");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntryLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null).WithMany()
                .HasForeignKey("SupplierId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_JournalEntryLines_Supplier");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntryLine", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany()
                .HasForeignKey("EmployeeId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_JournalEntryLines_Employee");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.JournalEntryLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.CostCenter", null).WithMany()
                .HasForeignKey("CostCenterId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_JournalEntryLines_CostCenter");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentAllocation", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", null).WithMany()
                .HasForeignKey("ReceiptVoucherLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentAllocations_ReceiptVoucherLine");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentAllocation", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.PaymentVoucherLine", null).WithMany()
                .HasForeignKey("PaymentVoucherLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentAllocations_PaymentVoucherLine");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentAllocation", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentAllocations_Currency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucher", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null).WithMany()
                .HasForeignKey("SupplierId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVouchers_Supplier");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucher", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.CashAccount", null).WithMany()
                .HasForeignKey("CashAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVouchers_CashAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucher", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.BankAccount", null).WithMany()
                .HasForeignKey("BankAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVouchers_BankAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucher", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("BaseCurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVouchers_BaseCurrency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucher", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany()
                .HasForeignKey("JournalEntryId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVouchers_JournalEntry");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PaymentVoucherLines_Account");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("CounterpartyAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVoucherLines_CounterpartyAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("SettlementAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVoucherLines_SettlementAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVoucherLines_Customer");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null).WithMany()
                .HasForeignKey("SupplierId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVoucherLines_Supplier");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany()
                .HasForeignKey("EmployeeId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVoucherLines_Employee");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.CashAccount", null).WithMany()
                .HasForeignKey("CashAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVoucherLines_CashAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.BankAccount", null).WithMany()
                .HasForeignKey("BankAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVoucherLines_BankAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVoucherLines_Currency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PostingProfileLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PostingProfileLines_Account");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucher", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVouchers_Customer");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucher", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.CashAccount", null).WithMany()
                .HasForeignKey("CashAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVouchers_CashAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucher", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.BankAccount", null).WithMany()
                .HasForeignKey("BankAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVouchers_BankAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucher", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("BaseCurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVouchers_BaseCurrency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucher", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany()
                .HasForeignKey("JournalEntryId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVouchers_JournalEntry");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_ReceiptVoucherLines_Account");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("CounterpartyAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVoucherLines_CounterpartyAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("SettlementAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVoucherLines_SettlementAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVoucherLines_Customer");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null).WithMany()
                .HasForeignKey("SupplierId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVoucherLines_Supplier");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany()
                .HasForeignKey("EmployeeId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVoucherLines_Employee");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.CashAccount", null).WithMany()
                .HasForeignKey("CashAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVoucherLines_CashAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.BankAccount", null).WithMany()
                .HasForeignKey("BankAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVoucherLines_BankAccount");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVoucherLines_Currency");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Supplier", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_Suppliers_Account");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.FrameDetails", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Product", null).WithMany()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("FK_FrameDetails_Products_ProductId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryBalance", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("WarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_InventoryBalances_Warehouses_WarehouseId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryBalance", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_InventoryBalances_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryLedger", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.InventoryTransaction", null).WithMany()
                .HasForeignKey("TransactionId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_InventoryLedger_Transactions_TransactionId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryLedger", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.InventoryTransactionLine", null).WithMany()
                .HasForeignKey("TransactionLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_InventoryLedger_TransactionLines_TransactionLineId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryLedger", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("WarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_InventoryLedger_Warehouses_WarehouseId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryLedger", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_InventoryLedger_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryTransaction", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("SourceWarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_InventoryTransactions_SourceWarehouse");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryTransaction", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("DestinationWarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_InventoryTransactions_DestinationWarehouse");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryTransactionLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.InventoryTransaction", null).WithMany()
                .HasForeignKey("TransactionId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("FK_InventoryTransactionLines_Transactions_TransactionId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.InventoryTransactionLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_InventoryTransactionLines_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.LensDetails", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Product", null).WithMany()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("FK_LensDetails_Products_ProductId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.ProductCategory", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductCategory", null).WithMany()
                .HasForeignKey("ParentCategoryId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ProductCategories_ParentCategory");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.Product", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductCategory", null).WithMany()
                .HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_Products_ProductCategories_CategoryId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.Product", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Brand", null).WithMany()
                .HasForeignKey("BrandId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Products_Brands_BrandId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.Product", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductType", null).WithMany()
                .HasForeignKey("ProductTypeId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_Products_ProductTypes_ProductTypeId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.ProductVariant", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Product", null).WithMany()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("FK_ProductVariants_Products_ProductId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.ProductVariant", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Unit", null).WithMany()
                .HasForeignKey("UnitId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ProductVariants_Units_UnitId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.StockCount", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("WarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_StockCounts_Warehouses_WarehouseId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.StockCountLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.StockCount", null).WithMany()
                .HasForeignKey("StockCountId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("FK_StockCountLines_StockCounts_StockCountId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.StockCountLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_StockCountLines_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.StockReservation", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_StockReservations_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Entities.Inventory.StockReservation", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("WarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_StockReservations_Warehouses_WarehouseId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerOrder", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_CustomerOrders_Customers_CustomerId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerOrder", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.PrescriptionRevision", null).WithMany()
                .HasForeignKey("PrescriptionRevisionId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CustomerOrders_PrescriptionRevisions_PrescriptionRevisionId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerOrder", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_CustomerOrders_Currencies_CurrencyId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerOrderLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CustomerOrderLines_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerOrderLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("WarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CustomerOrderLines_Warehouses_WarehouseId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerOrderLine", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.PrescriptionRevision", null).WithMany()
                .HasForeignKey("PrescriptionRevisionId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CustomerOrderLines_PrescriptionRevisions_PrescriptionRevisionId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.Prescription", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_Prescriptions_Customers_CustomerId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoice", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SalesInvoices_Customers_CustomerId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoice", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.CustomerOrder", null).WithMany()
                .HasForeignKey("CustomerOrderId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SalesInvoices_CustomerOrders_CustomerOrderId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoice", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.PrescriptionRevision", null).WithMany()
                .HasForeignKey("PrescriptionRevisionId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SalesInvoices_PrescriptionRevisions_PrescriptionRevisionId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoice", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SalesInvoices_Currencies_CurrencyId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoice", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("BaseCurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SalesInvoices_BaseCurrencies_BaseCurrencyId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoice", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany()
                .HasForeignKey("JournalEntryId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SalesInvoices_JournalEntries_JournalEntryId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoiceLine", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.CustomerOrderLine", null).WithMany()
                .HasForeignKey("CustomerOrderLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SalesInvoiceLines_CustomerOrderLines_CustomerOrderLineId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoiceLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SalesInvoiceLines_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoiceLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("WarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SalesInvoiceLines_Warehouses_WarehouseId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoiceLine", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.PrescriptionRevision", null).WithMany()
                .HasForeignKey("PrescriptionRevisionId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SalesInvoiceLines_PrescriptionRevisions_PrescriptionRevisionId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoiceLinePrescriptionSnapshot", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoiceLine", null).WithOne("PrescriptionSnapshot")
                .HasForeignKey("OAS.Domain.Sales.Entities.SalesInvoiceLinePrescriptionSnapshot", "SalesInvoiceLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SalesInvoicePrescriptionSnapshots_Lines_SalesInvoiceLineId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoiceLinePrescriptionSnapshot", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.PrescriptionRevision", null).WithMany()
                .HasForeignKey("PrescriptionRevisionId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SalesInvoicePrescriptionSnapshots_Revisions_PrescriptionRevisionId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesPriceOverride", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoice", null).WithMany()
                .HasForeignKey("SalesInvoiceId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SalesPriceOverrides_Invoices_SalesInvoiceId");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesPriceOverride", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoiceLine", null).WithMany()
                .HasForeignKey("SalesInvoiceLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SalesPriceOverrides_Lines_SalesInvoiceLineId");
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Customer", b =>
        {
            b.OwnsOne("OAS.Domain.Accounting.ValueObjects.PartyContactInfo", "ContactInfo", b1 =>
            {
                b1.Property<Guid>("CustomerId").HasColumnType("uniqueidentifier");
                b1.Property<string>("ContactPersonName").HasMaxLength(150).HasColumnType("nvarchar(150)").HasColumnName("ContactPersonName");
                b1.Property<string>("ContactPersonTitle").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("ContactPersonTitle");
                b1.Property<string>("Phone").HasMaxLength(32).HasColumnType("nvarchar(32)").HasColumnName("Phone");
                b1.Property<string>("Mobile").HasMaxLength(32).HasColumnType("nvarchar(32)").HasColumnName("Mobile");
                b1.Property<string>("AlternatePhone").HasMaxLength(32).HasColumnType("nvarchar(32)").HasColumnName("AlternatePhone");
                b1.Property<string>("WhatsAppNumber").HasMaxLength(32).HasColumnType("nvarchar(32)").HasColumnName("WhatsAppNumber");
                b1.Property<string>("Email").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("Email");
                b1.Property<string>("Website").HasMaxLength(300).HasColumnType("nvarchar(300)").HasColumnName("Website");
                b1.Property<byte>("PreferredContactMethod").IsRequired().HasColumnType("tinyint").HasColumnName("PreferredContactMethod");
                b1.HasKey("CustomerId");
                b1.HasIndex("Mobile").HasDatabaseName("IX_Customers_Mobile");
                b1.ToTable("tbl_Customers", "dbo");
                b1.WithOwner().HasForeignKey("CustomerId");

                b1.OwnsOne("OAS.Domain.Accounting.ValueObjects.PartyAddress", "Address", b2 =>
                {
                    b2.Property<Guid>("CustomerId").HasColumnType("uniqueidentifier");
                    b2.Property<string>("Country").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("Country");
                    b2.Property<string>("Governorate").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("Governorate");
                    b2.Property<string>("City").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("City");
                    b2.Property<string>("District").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("District");
                    b2.Property<string>("Street").HasMaxLength(150).HasColumnType("nvarchar(150)").HasColumnName("Street");
                    b2.Property<string>("Building").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("Building");
                    b2.Property<string>("PostalCode").HasMaxLength(24).HasColumnType("nvarchar(24)").HasColumnName("PostalCode");
                    b2.Property<string>("AddressDetails").HasMaxLength(300).HasColumnType("nvarchar(300)").HasColumnName("AddressDetails");
                    b2.HasKey("CustomerId");
                    b2.ToTable("tbl_Customers", "dbo");
                    b2.WithOwner().HasForeignKey("CustomerId");
                });
            });
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.Supplier", b =>
        {
            b.OwnsOne("OAS.Domain.Accounting.ValueObjects.PartyContactInfo", "ContactInfo", b1 =>
            {
                b1.Property<Guid>("SupplierId").HasColumnType("uniqueidentifier");
                b1.Property<string>("ContactPersonName").HasMaxLength(150).HasColumnType("nvarchar(150)").HasColumnName("ContactPersonName");
                b1.Property<string>("ContactPersonTitle").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("ContactPersonTitle");
                b1.Property<string>("Phone").HasMaxLength(32).HasColumnType("nvarchar(32)").HasColumnName("Phone");
                b1.Property<string>("Mobile").HasMaxLength(32).HasColumnType("nvarchar(32)").HasColumnName("Mobile");
                b1.Property<string>("AlternatePhone").HasMaxLength(32).HasColumnType("nvarchar(32)").HasColumnName("AlternatePhone");
                b1.Property<string>("WhatsAppNumber").HasMaxLength(32).HasColumnType("nvarchar(32)").HasColumnName("WhatsAppNumber");
                b1.Property<string>("Email").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("Email");
                b1.Property<string>("Website").HasMaxLength(300).HasColumnType("nvarchar(300)").HasColumnName("Website");
                b1.Property<byte>("PreferredContactMethod").IsRequired().HasColumnType("tinyint").HasColumnName("PreferredContactMethod");
                b1.HasKey("SupplierId");
                b1.HasIndex("Mobile").HasDatabaseName("IX_Suppliers_Mobile");
                b1.ToTable("tbl_Suppliers", "dbo");
                b1.WithOwner().HasForeignKey("SupplierId");

                b1.OwnsOne("OAS.Domain.Accounting.ValueObjects.PartyAddress", "Address", b2 =>
                {
                    b2.Property<Guid>("SupplierId").HasColumnType("uniqueidentifier");
                    b2.Property<string>("Country").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("Country");
                    b2.Property<string>("Governorate").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("Governorate");
                    b2.Property<string>("City").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("City");
                    b2.Property<string>("District").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("District");
                    b2.Property<string>("Street").HasMaxLength(150).HasColumnType("nvarchar(150)").HasColumnName("Street");
                    b2.Property<string>("Building").HasMaxLength(100).HasColumnType("nvarchar(100)").HasColumnName("Building");
                    b2.Property<string>("PostalCode").HasMaxLength(24).HasColumnType("nvarchar(24)").HasColumnName("PostalCode");
                    b2.Property<string>("AddressDetails").HasMaxLength(300).HasColumnType("nvarchar(300)").HasColumnName("AddressDetails");
                    b2.HasKey("SupplierId");
                    b2.ToTable("tbl_Suppliers", "dbo");
                    b2.WithOwner().HasForeignKey("SupplierId");
                });
            });
        });

#pragma warning restore 612, 618
    }
}
