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

        modelBuilder.HasSequence<long>("DepartmentCodeSequence", "hr");
        modelBuilder.HasSequence<long>("EmployeeContractCodeSequence", "hr");
        modelBuilder.HasSequence<long>("SalaryComponentCodeSequence", "hr");
        modelBuilder.HasSequence<long>("SalaryStructureCodeSequence", "hr");
        modelBuilder.HasSequence<long>("EmployeeDocumentCodeSequence", "hr");
        modelBuilder.HasSequence<long>("WorkShiftCodeSequence", "hr");
        modelBuilder.HasSequence<long>("HolidayCodeSequence", "hr");
        modelBuilder.HasSequence<long>("LeaveTypeCodeSequence", "hr");
        modelBuilder.HasSequence<long>("LeaveRequestCodeSequence", "hr");
        modelBuilder.HasSequence<long>("OvertimeCodeSequence", "hr");
        modelBuilder.HasSequence<long>("EmployeeLoanCodeSequence", "hr");
        modelBuilder.HasSequence<long>("EmployeeAdjustmentCodeSequence", "hr");
        modelBuilder.HasSequence<long>("PayrollPolicyCodeSequence", "hr");
        modelBuilder.HasSequence<long>("PayrollRunCodeSequence", "hr");
        modelBuilder.HasSequence<long>("EndOfServiceCodeSequence", "hr");
        modelBuilder.HasSequence<long>("PurchaseRequestCodeSequence", "dbo");
        modelBuilder.HasSequence<long>("PurchaseOrderCodeSequence", "dbo");
        modelBuilder.HasSequence<long>("PurchaseReceiptCodeSequence", "dbo");
        modelBuilder.HasSequence<long>("PurchaseInvoiceCodeSequence", "dbo");
        modelBuilder.HasSequence<long>("PurchaseReturnCodeSequence", "dbo");
        modelBuilder.HasSequence<long>("SalesReturnCodeSequence", "dbo");
        modelBuilder.HasSequence<long>("CommissionStatementCodeSequence", "dbo");
        modelBuilder.HasSequence<long>("OpticalProductionJobCodeSequence", "dbo");

        modelBuilder.Entity("OAS.Domain.Identity.Entities.Role", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("DisplayName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<bool>("IsSystem").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
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
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
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
            b.Property<string>("EmployeeCode").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("FirstName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<Guid?>("DepartmentId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("ManagerEmployeeId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly?>("HireDate").HasColumnType("date");
            b.Property<bool>("IsSalesperson").HasColumnType("bit");
            b.Property<bool>("IsTechnician").HasColumnType("bit");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<bool>("IsCommissionEligible").HasColumnType("bit");
            b.Property<Guid>("JobTitleId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("LastName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Photo").HasMaxLength(512).HasColumnType("nvarchar(512)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<Guid?>("UserAccountId").HasColumnType("uniqueidentifier");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("DepartmentId").HasDatabaseName("IX_Employees_DepartmentId");
            b.HasIndex("EmployeeCode").IsUnique().HasDatabaseName("UX_Employees_EmployeeCode");
            b.HasIndex("IsActive").HasDatabaseName("IX_Employees_IsActive");
            b.HasIndex("JobTitleId").HasDatabaseName("IX_Employees_JobTitleId");
            b.HasIndex("LastName", "FirstName").HasDatabaseName("IX_Employees_LastName_FirstName");
            b.HasIndex("ManagerEmployeeId").HasDatabaseName("IX_Employees_ManagerEmployeeId");
            b.HasIndex("UserAccountId").IsUnique().HasDatabaseName("UX_Employees_UserAccountId").HasFilter("[UserAccountId] IS NOT NULL");
            b.ToTable("Employees", "hr", t => t.HasCheckConstraint("CK_Employees_NotSelfManager", "[ManagerEmployeeId] IS NULL OR [ManagerEmployeeId] <> [Id]"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.Department", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("DepartmentCode").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<Guid?>("ManagerEmployeeId").HasColumnType("uniqueidentifier");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NameEn").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<Guid?>("ParentDepartmentId").HasColumnType("uniqueidentifier");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("DepartmentCode").IsUnique().HasDatabaseName("UX_Departments_DepartmentCode");
            b.HasIndex("IsActive").HasDatabaseName("IX_Departments_IsActive");
            b.HasIndex("ManagerEmployeeId").HasDatabaseName("IX_Departments_ManagerEmployeeId");
            b.HasIndex("NameAr").HasDatabaseName("IX_Departments_NameAr");
            b.HasIndex("ParentDepartmentId").HasDatabaseName("IX_Departments_ParentDepartmentId");
            b.ToTable("Departments", "hr", t => t.HasCheckConstraint("CK_Departments_NotSelfParent", "[ParentDepartmentId] IS NULL OR [ParentDepartmentId] <> [Id]"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.EmployeeContract", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<string>("ActivatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ActivatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ContractCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<byte>("ContractType").HasColumnType("tinyint");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<Guid>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly?>("EndDate").HasColumnType("date");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<DateOnly?>("ProbationEndDate").HasColumnType("date");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<DateOnly>("StartDate").HasColumnType("date");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<DateTimeOffset?>("TerminatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateOnly?>("TerminationEffectiveDate").HasColumnType("date");
            b.Property<string>("TerminatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("TerminationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<decimal?>("WorkingDaysPerWeek").HasColumnType("decimal(4,2)");
            b.Property<decimal?>("WorkingHoursPerDay").HasColumnType("decimal(6,2)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("ContractCode").IsUnique().HasDatabaseName("UX_EmployeeContracts_ContractCode");
            b.HasIndex("EmployeeId").HasDatabaseName("IX_EmployeeContracts_EmployeeId");
            b.HasIndex("EmployeeId", "Status").IsUnique().HasFilter("[Status] = 2").HasDatabaseName("UX_EmployeeContracts_Employee_Active");
            b.HasIndex("StartDate", "EndDate").HasDatabaseName("IX_EmployeeContracts_StartDate_EndDate");
            b.HasIndex("Status").HasDatabaseName("IX_EmployeeContracts_Status");
            b.ToTable("EmployeeContracts", "hr", t =>
            {
                t.HasCheckConstraint("CK_EmployeeContracts_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
                t.HasCheckConstraint("CK_EmployeeContracts_Probation", "[ProbationEndDate] IS NULL OR ([ProbationEndDate] >= [StartDate] AND ([EndDate] IS NULL OR [ProbationEndDate] <= [EndDate]))");
                t.HasCheckConstraint("CK_EmployeeContracts_WorkingHours", "[WorkingHoursPerDay] IS NULL OR ([WorkingHoursPerDay] > 0 AND [WorkingHoursPerDay] <= 24)");
                t.HasCheckConstraint("CK_EmployeeContracts_WorkingDays", "[WorkingDaysPerWeek] IS NULL OR ([WorkingDaysPerWeek] > 0 AND [WorkingDaysPerWeek] <= 7)");
            });
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.SalaryComponent", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<byte>("CalculationMethod").HasColumnType("tinyint");
            b.Property<string>("ComponentCode").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<byte>("ComponentType").HasColumnType("tinyint");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreditPostingRole").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("DebitPostingRole").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<int>("DisplayOrder").HasColumnType("int");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<bool>("IsBasicSalary").HasColumnType("bit");
            b.Property<bool>("IsRecurring").HasColumnType("bit");
            b.Property<bool>("IsTaxable").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NameEn").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("ComponentCode").IsUnique().HasDatabaseName("UX_SalaryComponents_ComponentCode");
            b.HasIndex("IsActive").HasDatabaseName("IX_SalaryComponents_IsActive");
            b.HasIndex("IsBasicSalary").IsUnique().HasFilter("[IsBasicSalary] = 1 AND [IsActive] = 1").HasDatabaseName("UX_SalaryComponents_ActiveBasicSalary");
            b.ToTable("SalaryComponents", "hr", t => t.HasCheckConstraint("CK_SalaryComponents_DisplayOrder", "[DisplayOrder] >= 0"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.EmployeeSalaryStructure", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset");
            b.Property<Guid?>("ContractId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<Guid>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("EffectiveFrom").HasColumnType("date");
            b.Property<DateOnly?>("EffectiveTo").HasColumnType("date");
            b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<string>("StructureCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("EmployeeId").HasDatabaseName("IX_EmployeeSalaryStructures_EmployeeId");
            b.HasIndex("EmployeeId", "EffectiveFrom", "EffectiveTo").HasDatabaseName("IX_EmployeeSalaryStructures_EffectiveDates");
            b.HasIndex("EmployeeId", "Status").IsUnique().HasFilter("[Status] = 2").HasDatabaseName("UX_EmployeeSalaryStructures_Employee_Active");
            b.HasIndex("StructureCode").IsUnique().HasDatabaseName("UX_EmployeeSalaryStructures_StructureCode");
            b.ToTable("EmployeeSalaryStructures", "hr", t => t.HasCheckConstraint("CK_EmployeeSalaryStructures_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.EmployeeSalaryStructureLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Amount").HasColumnType("decimal(19,4)");
            b.Property<byte>("CalculationMethodSnapshot").HasColumnType("tinyint");
            b.Property<string>("ComponentCodeSnapshot").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("ComponentNameSnapshot").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<byte>("ComponentTypeSnapshot").HasColumnType("tinyint");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreditPostingRoleSnapshot").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("DebitPostingRoleSnapshot").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid>("EmployeeSalaryStructureId").HasColumnType("uniqueidentifier");
            b.Property<bool>("IsBasicSalarySnapshot").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<decimal?>("Percentage").HasColumnType("decimal(9,6)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<Guid>("SalaryComponentId").HasColumnType("uniqueidentifier");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("EmployeeSalaryStructureId").HasDatabaseName("IX_EmployeeSalaryStructureLines_StructureId");
            b.HasIndex("EmployeeSalaryStructureId", "SalaryComponentId").IsUnique().HasDatabaseName("UX_EmployeeSalaryStructureLines_Structure_Component");
            b.ToTable("EmployeeSalaryStructureLines", "hr", t =>
            {
                t.HasCheckConstraint("CK_EmployeeSalaryStructureLines_Amount", "[Amount] >= 0");
                t.HasCheckConstraint("CK_EmployeeSalaryStructureLines_Percentage", "[Percentage] IS NULL OR [Percentage] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.EmployeeDocument", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<string>("ContentType").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("DocumentCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<byte>("DocumentType").HasColumnType("tinyint");
            b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly?>("ExpiryDate").HasColumnType("date");
            b.Property<long>("FileSize").HasColumnType("bigint");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<DateOnly?>("IssueDate").HasColumnType("date");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("OriginalFileName").IsRequired().HasMaxLength(260).HasColumnType("nvarchar(260)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("Sha256Hash").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("StorageKey").IsRequired().HasMaxLength(512).HasColumnType("nvarchar(512)");
            b.Property<string>("Title").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("DocumentCode").IsUnique().HasDatabaseName("UX_EmployeeDocuments_DocumentCode");
            b.HasIndex("EmployeeId").HasDatabaseName("IX_EmployeeDocuments_EmployeeId");
            b.HasIndex("EmployeeId", "IsActive").HasDatabaseName("IX_EmployeeDocuments_Employee_Status");
            b.HasIndex("ExpiryDate").HasDatabaseName("IX_EmployeeDocuments_ExpiryDate");
            b.ToTable("EmployeeDocuments", "hr", t => t.HasCheckConstraint("CK_EmployeeDocuments_FileSize", "[FileSize] > 0"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Settings.HrSettings", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<byte>("LeaveYearStartMonth").HasColumnType("tinyint");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<bool>("RequireAttendanceApproval").HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("TimeZoneId").HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.ToTable("HrSettings", "hr", t => t.HasCheckConstraint("CK_HrSettings_LeaveYearStartMonth", "[LeaveYearStartMonth] BETWEEN 1 AND 12"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Time.WorkShift", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<int>("BreakMinutes").HasColumnType("int");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<TimeOnly>("EndTime").HasColumnType("time");
            b.Property<int>("GraceEarlyLeaveMinutes").HasColumnType("int");
            b.Property<int>("GraceLateMinutes").HasColumnType("int");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<bool>("IsFlexible").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NameEn").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("ShiftCode").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<TimeOnly>("StartTime").HasColumnType("time");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<byte>("WorkingDaysMask").HasColumnType("tinyint");
            b.HasKey("Id");
            b.HasIndex("IsActive").HasDatabaseName("IX_WorkShifts_IsActive");
            b.HasIndex("ShiftCode").IsUnique().HasDatabaseName("UX_WorkShifts_ShiftCode");
            b.ToTable("WorkShifts", "hr", t =>
            {
                t.HasCheckConstraint("CK_WorkShifts_Minutes", "[BreakMinutes] >= 0 AND [GraceLateMinutes] >= 0 AND [GraceEarlyLeaveMinutes] >= 0");
                t.HasCheckConstraint("CK_WorkShifts_Times", "[StartTime] <> [EndTime]");
                t.HasCheckConstraint("CK_WorkShifts_WorkingDaysMask", "[WorkingDaysMask] BETWEEN 1 AND 127");
            });
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Time.EmployeeShiftAssignment", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateOnly>("EffectiveFrom").HasColumnType("date");
            b.Property<DateOnly?>("EffectiveTo").HasColumnType("date");
            b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<Guid>("WorkShiftId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("EmployeeId", "EffectiveFrom", "EffectiveTo").HasDatabaseName("IX_ShiftAssignments_Employee_Dates");
            b.HasIndex("WorkShiftId").HasDatabaseName("IX_ShiftAssignments_ShiftId");
            b.ToTable("EmployeeShiftAssignments", "hr", t => t.HasCheckConstraint("CK_EmployeeShiftAssignments_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Time.Holiday", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateOnly>("EndDate").HasColumnType("date");
            b.Property<string>("HolidayCode").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<bool>("IsPaid").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NameEn").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<DateOnly>("StartDate").HasColumnType("date");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("HolidayCode").IsUnique().HasDatabaseName("UX_Holidays_HolidayCode");
            b.HasIndex("StartDate", "EndDate", "IsActive").HasDatabaseName("IX_Holidays_Dates");
            b.ToTable("Holidays", "hr", t => t.HasCheckConstraint("CK_Holidays_Dates", "[EndDate] >= [StartDate]"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Leave.LeaveType", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<byte>("AccrualMethod").HasColumnType("tinyint");
            b.Property<decimal>("AnnualEntitlementDays").HasColumnType("decimal(8,2)");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<byte>("DayCountingMethod").HasColumnType("tinyint");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<bool>("IsPaid").HasColumnType("bit");
            b.Property<bool>("IsEncashableOnTermination").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("LeaveTypeCode").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<decimal>("MaximumCarryForwardDays").HasColumnType("decimal(8,2)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("NameEn").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<bool>("ProrateOnHire").HasColumnType("bit");
            b.Property<bool>("RequiresBalance").HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("IsActive").HasDatabaseName("IX_LeaveTypes_IsActive");
            b.HasIndex("LeaveTypeCode").IsUnique().HasDatabaseName("UX_LeaveTypes_Code");
            b.ToTable("LeaveTypes", "hr", t => t.HasCheckConstraint("CK_LeaveTypes_Amounts", "[AnnualEntitlementDays]>=0 AND [MaximumCarryForwardDays]>=0"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Leave.EmployeeLeaveBalance", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<decimal>("AccruedDays").HasColumnType("decimal(8,2)");
            b.Property<decimal>("AdjustmentDays").HasColumnType("decimal(8,2)");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("LeaveTypeId").HasColumnType("uniqueidentifier");
            b.Property<short>("LeaveYear").HasColumnType("smallint");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<decimal>("OpeningBalanceDays").HasColumnType("decimal(8,2)");
            b.Property<decimal>("SettledDays").HasColumnType("decimal(8,2)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<decimal>("UsedDays").HasColumnType("decimal(8,2)");
            b.HasKey("Id");
            b.HasIndex("EmployeeId", "LeaveTypeId", "LeaveYear").IsUnique().HasDatabaseName("UX_LeaveBalances_Employee_Type_Year");
            b.HasIndex("LeaveTypeId").HasDatabaseName("IX_LeaveBalances_LeaveTypeId");
            b.ToTable("EmployeeLeaveBalances", "hr", t => t.HasCheckConstraint("CK_LeaveBalances_NonNegative", "[OpeningBalanceDays]>=0 AND [AccruedDays]>=0 AND [UsedDays]>=0 AND [SettledDays]>=0"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Leave.LeaveRequest", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<decimal?>("ApprovedDays").HasColumnType("decimal(8,2)");
            b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("BalanceOverrideReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<bool>("BalanceOverrideUsed").HasColumnType("bit");
            b.Property<DateTimeOffset?>("CancelledAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CancellationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<byte>("DayCountingMethodSnapshot").HasColumnType("tinyint");
            b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("EndDate").HasColumnType("date");
            b.Property<bool>("IsPaidSnapshot").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("LeaveRequestCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<string>("LeaveTypeCodeSnapshot").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<Guid>("LeaveTypeId").HasColumnType("uniqueidentifier");
            b.Property<string>("LeaveTypeNameSnapshot").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<string>("Reason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTimeOffset?>("RejectedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("RejectedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("RejectionReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<bool>("RequiresBalanceSnapshot").HasColumnType("bit");
            b.Property<decimal>("RequestedDays").HasColumnType("decimal(8,2)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<DateOnly>("StartDate").HasColumnType("date");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<DateTimeOffset?>("SubmittedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("SubmittedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("EmployeeId").HasDatabaseName("IX_LeaveRequests_Employee");
            b.HasIndex("LeaveRequestCode").IsUnique().HasDatabaseName("UX_LeaveRequests_Code");
            b.HasIndex("StartDate", "EndDate").HasDatabaseName("IX_LeaveRequests_Dates");
            b.HasIndex("Status").HasDatabaseName("IX_LeaveRequests_Status");
            b.HasIndex("LeaveTypeId").HasDatabaseName("IX_LeaveRequests_LeaveTypeId");
            b.ToTable("LeaveRequests", "hr", t =>
            {
                t.HasCheckConstraint("CK_LeaveRequests_Dates", "[EndDate] >= [StartDate]");
                t.HasCheckConstraint("CK_LeaveRequests_Days", "[RequestedDays]>0 AND ([ApprovedDays] IS NULL OR [ApprovedDays]>=0)");
            });
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Time.AttendanceRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte>("ApprovalStatus").HasColumnType("tinyint");
            b.Property<DateOnly>("AttendanceDate").HasColumnType("date");
            b.Property<byte>("AttendanceStatus").HasColumnType("tinyint");
            b.Property<int>("BreakMinutesSnapshot").HasColumnType("int");
            b.Property<DateTimeOffset?>("CheckInAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("CheckOutAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<int>("EarlyLeaveMinutes").HasColumnType("int");
            b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("EmployeePayrollId").HasColumnType("uniqueidentifier");
            b.Property<int>("GraceEarlyLeaveMinutesSnapshot").HasColumnType("int");
            b.Property<int>("GraceLateMinutesSnapshot").HasColumnType("int");
            b.Property<bool>("IsFlexibleSnapshot").HasColumnType("bit");
            b.Property<DateTimeOffset?>("LastCorrectedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastCorrectedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("LastCorrectionReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<int>("LateMinutes").HasColumnType("int");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<int>("OvertimeMinutes").HasColumnType("int");
            b.Property<DateTimeOffset?>("RejectedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("RejectedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("RejectionReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<DateTimeOffset?>("ScheduledEndAtUtc").HasColumnType("datetimeoffset");
            b.Property<int>("ScheduledMinutes").HasColumnType("int");
            b.Property<DateTimeOffset?>("ScheduledStartAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ShiftCodeSnapshot").HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("ShiftNameSnapshot").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<byte>("Source").HasColumnType("tinyint");
            b.Property<Guid?>("SourceHolidayId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("SourceLeaveRequestId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("SubmittedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("SubmittedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("TimeZoneIdSnapshot").HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<Guid?>("WorkShiftId").HasColumnType("uniqueidentifier");
            b.Property<int>("WorkedMinutes").HasColumnType("int");
            b.HasKey("Id");
            b.HasIndex("ApprovalStatus").HasDatabaseName("IX_Attendance_ApprovalStatus");
            b.HasIndex("AttendanceDate").HasDatabaseName("IX_Attendance_Date");
            b.HasIndex("AttendanceStatus").HasDatabaseName("IX_Attendance_Status");
            b.HasIndex("WorkShiftId").HasDatabaseName("IX_Attendance_WorkShiftId");
            b.HasIndex("SourceLeaveRequestId").HasDatabaseName("IX_Attendance_SourceLeaveRequestId");
            b.HasIndex("SourceHolidayId").HasDatabaseName("IX_Attendance_SourceHolidayId");
            b.HasIndex("EmployeeId", "AttendanceDate").IsUnique().HasDatabaseName("UX_Attendance_Employee_Date");
            b.HasIndex("EmployeePayrollId").HasDatabaseName("IX_Attendance_EmployeePayrollId");
            b.ToTable("AttendanceRecords", "hr", t =>
            {
                t.HasCheckConstraint("CK_Attendance_CheckTimes", "[CheckOutAtUtc] IS NULL OR [CheckInAtUtc] IS NULL OR [CheckOutAtUtc] >= [CheckInAtUtc]");
                t.HasCheckConstraint("CK_Attendance_Minutes", "[ScheduledMinutes]>=0 AND [WorkedMinutes]>=0 AND [LateMinutes]>=0 AND [EarlyLeaveMinutes]>=0 AND [OvertimeMinutes]>=0 AND [BreakMinutesSnapshot]>=0 AND [GraceLateMinutesSnapshot]>=0 AND [GraceEarlyLeaveMinutesSnapshot]>=0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Overtime.OvertimeRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<int>("ApprovedMinutes").HasColumnType("int");
            b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<Guid?>("AttendanceRecordId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("CancelledAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CancellationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("EmployeePayrollId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("OvertimeCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<decimal>("RateMultiplier").HasColumnType("decimal(9,4)");
            b.Property<string>("Reason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTimeOffset?>("RejectedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("RejectedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("RejectionReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<int>("RequestedMinutes").HasColumnType("int");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<DateTimeOffset?>("SubmittedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("SubmittedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateOnly>("WorkDate").HasColumnType("date");
            b.HasKey("Id");
            b.HasIndex("AttendanceRecordId").IsUnique().HasFilter("[AttendanceRecordId] IS NOT NULL").HasDatabaseName("UX_Overtime_AttendanceRecord");
            b.HasIndex("EmployeePayrollId").HasDatabaseName("IX_Overtime_EmployeePayrollId");
            b.HasIndex("EmployeeId", "WorkDate").HasDatabaseName("IX_Overtime_Employee_Date");
            b.HasIndex("OvertimeCode").IsUnique().HasDatabaseName("UX_Overtime_Code");
            b.HasIndex("Status").HasDatabaseName("IX_Overtime_Status");
            b.ToTable("OvertimeRecords", "hr", t =>
            {
                t.HasCheckConstraint("CK_Overtime_Minutes", "[RequestedMinutes]>0 AND [ApprovedMinutes]>=0 AND [ApprovedMinutes]<=[RequestedMinutes]");
                t.HasCheckConstraint("CK_Overtime_Multiplier", "[RateMultiplier]>0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Loans.EmployeeLoan", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("CancelledAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CancellationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<Guid?>("ContractId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("CurrencyCodeSnapshot").IsRequired().HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<byte>("CurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<Guid>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencySymbolSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<DateTimeOffset?>("DisbursedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("DisbursedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("FirstInstallmentDate").HasColumnType("date");
            b.Property<int>("InstallmentCount").HasColumnType("int");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("LoanCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<DateOnly>("LoanDate").HasColumnType("date");
            b.Property<Guid?>("PaymentVoucherId").HasColumnType("uniqueidentifier");
            b.Property<decimal>("PrincipalAmount").HasColumnType("decimal(19,4)");
            b.Property<string>("Reason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTimeOffset?>("RejectedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("RejectedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("RejectionReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte>("RepaymentMode").HasColumnType("tinyint");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<Guid?>("SalaryStructureId").HasColumnType("uniqueidentifier");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<DateTimeOffset?>("SubmittedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("SubmittedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("EmployeeId", "Status").HasDatabaseName("IX_EmployeeLoans_Employee_Status");
            b.HasIndex("LoanCode").IsUnique().HasDatabaseName("UX_EmployeeLoans_LoanCode");
            b.HasIndex("PaymentVoucherId").IsUnique().HasFilter("[PaymentVoucherId] IS NOT NULL").HasDatabaseName("UX_EmployeeLoans_PaymentVoucher");
            b.HasIndex("ContractId").HasDatabaseName("IX_EmployeeLoans_ContractId");
            b.HasIndex("SalaryStructureId").HasDatabaseName("IX_EmployeeLoans_SalaryStructureId");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_EmployeeLoans_CurrencyId");
            b.ToTable("EmployeeLoans", "hr", t =>
            {
                t.HasCheckConstraint("CK_EmployeeLoans_Count", "[InstallmentCount]>0");
                t.HasCheckConstraint("CK_EmployeeLoans_Principal", "[PrincipalAmount]>0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Loans.EmployeeLoanInstallment", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Amount").HasColumnType("decimal(19,4)");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateTimeOffset?>("DeductedAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateOnly>("DueDate").HasColumnType("date");
            b.Property<Guid>("EmployeeLoanId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("EmployeePayrollId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("EndOfServiceSettlementId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("ExternallyPaidAtUtc").HasColumnType("datetimeoffset");
            b.Property<int>("InstallmentSequence").HasColumnType("int");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<Guid?>("ReceiptVoucherId").HasColumnType("uniqueidentifier");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("DueDate", "Status").HasDatabaseName("IX_LoanInstallments_DueDate_Status");
            b.HasIndex("EmployeeLoanId", "InstallmentSequence").IsUnique().HasDatabaseName("UX_LoanInstallments_Loan_Sequence");
            b.HasIndex("ReceiptVoucherId").HasDatabaseName("IX_LoanInstallments_ReceiptVoucherId");
            b.HasIndex("EmployeePayrollId").HasDatabaseName("IX_LoanInstallments_EmployeePayrollId");
            b.HasIndex("EndOfServiceSettlementId").HasDatabaseName("IX_LoanInstallments_EndOfServiceSettlementId");
            b.ToTable("EmployeeLoanInstallments", "hr", t => t.HasCheckConstraint("CK_LoanInstallments_Amount", "[Amount]>0"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Adjustments.EmployeeAdjustment", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<string>("AdjustmentCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<byte>("AdjustmentType").HasColumnType("tinyint");
            b.Property<decimal>("Amount").HasColumnType("decimal(19,4)");
            b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("CancelledAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CancellationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("ComponentCodeSnapshot").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("ComponentNameSnapshot").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<byte>("ComponentTypeSnapshot").HasColumnType("tinyint");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("CreditPostingRoleSnapshot").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("CurrencyCodeSnapshot").IsRequired().HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<byte>("CurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<Guid>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencySymbolSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<string>("DebitPostingRoleSnapshot").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<DateOnly>("EffectiveDate").HasColumnType("date");
            b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("EmployeePayrollId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Reason").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTimeOffset?>("RejectedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("RejectedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("RejectionReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<Guid>("SalaryComponentId").HasColumnType("uniqueidentifier");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<DateTimeOffset?>("SubmittedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("SubmittedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id");
            b.HasIndex("AdjustmentCode").IsUnique().HasDatabaseName("UX_EmployeeAdjustments_Code");
            b.HasIndex("EmployeeId", "EffectiveDate").HasDatabaseName("IX_Adjustments_Employee_EffectiveDate");
            b.HasIndex("Status").HasDatabaseName("IX_Adjustments_Status");
            b.HasIndex("SalaryComponentId").HasDatabaseName("IX_Adjustments_SalaryComponentId");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_Adjustments_CurrencyId");
            b.HasIndex("EmployeePayrollId").HasDatabaseName("IX_Adjustments_EmployeePayrollId");
            b.ToTable("EmployeeAdjustments", "hr", t => t.HasCheckConstraint("CK_EmployeeAdjustments_Amount", "[Amount]>0"));
        });


        modelBuilder.Entity("OAS.Domain.Features.Employees.Payroll.PayrollPolicy", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("PolicyCode").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("NameAr").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<DateOnly>("EffectiveFrom").HasColumnType("date");
            b.Property<DateOnly?>("EffectiveTo").HasColumnType("date");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<byte>("ProrationMethod").HasColumnType("tinyint");
            b.Property<byte>("DailyRateMethod").HasColumnType("tinyint");
            b.Property<byte>("HourlyRateMethod").HasColumnType("tinyint");
            b.Property<bool>("RequireApprovedAttendance").HasColumnType("bit");
            b.Property<bool>("RequireFullPaymentBeforeRunClose").HasColumnType("bit");
            b.Property<Guid?>("AbsenceDeductionComponentId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("LateDeductionComponentId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("EarlyLeaveDeductionComponentId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("UnpaidLeaveComponentId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("OvertimeComponentId").HasColumnType("uniqueidentifier");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("ActivatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ActivatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id");
            b.HasIndex("PolicyCode").IsUnique().HasDatabaseName("UX_PayrollPolicies_Code"); b.HasIndex("Status").IsUnique().HasFilter("[Status] = 2").HasDatabaseName("UX_PayrollPolicies_OneActive");
            b.HasIndex("Status", "EffectiveFrom", "EffectiveTo").HasDatabaseName("IX_PayrollPolicies_Status_Dates");
            b.HasIndex("AbsenceDeductionComponentId"); b.HasIndex("LateDeductionComponentId"); b.HasIndex("EarlyLeaveDeductionComponentId"); b.HasIndex("UnpaidLeaveComponentId"); b.HasIndex("OvertimeComponentId");
            b.ToTable("PayrollPolicies", "hr", t => t.HasCheckConstraint("CK_PayrollPolicies_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
            b.HasOne("OAS.Domain.Features.Employees.Entities.SalaryComponent", null).WithMany().HasForeignKey("AbsenceDeductionComponentId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollPolicies_AbsenceComponent");
            b.HasOne("OAS.Domain.Features.Employees.Entities.SalaryComponent", null).WithMany().HasForeignKey("LateDeductionComponentId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollPolicies_LateComponent");
            b.HasOne("OAS.Domain.Features.Employees.Entities.SalaryComponent", null).WithMany().HasForeignKey("EarlyLeaveDeductionComponentId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollPolicies_EarlyLeaveComponent");
            b.HasOne("OAS.Domain.Features.Employees.Entities.SalaryComponent", null).WithMany().HasForeignKey("UnpaidLeaveComponentId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollPolicies_UnpaidLeaveComponent");
            b.HasOne("OAS.Domain.Features.Employees.Entities.SalaryComponent", null).WithMany().HasForeignKey("OvertimeComponentId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollPolicies_OvertimeComponent");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Payroll.PayrollPeriod", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("PeriodCode").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<short>("Year").HasColumnType("smallint");
            b.Property<byte>("Month").HasColumnType("tinyint");
            b.Property<DateOnly>("StartDate").HasColumnType("date");
            b.Property<DateOnly>("EndDate").HasColumnType("date");
            b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<string>("LockedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LockedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ClosedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ClosedAtUtc").HasColumnType("datetimeoffset");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id");
            b.HasIndex("PeriodCode").IsUnique().HasDatabaseName("UX_PayrollPeriods_Code");
            b.HasIndex("Year", "Month").IsUnique().HasDatabaseName("UX_PayrollPeriods_Year_Month");
            b.ToTable("PayrollPeriods", "hr", t => t.HasCheckConstraint("CK_PayrollPeriods_Dates", "[EndDate] >= [StartDate] AND [Month] BETWEEN 1 AND 12"));
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Payroll.PayrollRun", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("PayrollRunCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("PayrollPeriodId").HasColumnType("uniqueidentifier");
            b.Property<byte>("RunType").HasColumnType("tinyint"); b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<DateOnly>("CalculationDate").HasColumnType("date"); b.Property<DateOnly?>("PostingDate").HasColumnType("date"); b.Property<Guid>("PayrollPolicyId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("CurrencyId").HasColumnType("uniqueidentifier"); b.Property<string>("CurrencyCodeSnapshot").IsRequired().HasMaxLength(8).HasColumnType("nvarchar(8)"); b.Property<string>("CurrencySymbolSnapshot").HasMaxLength(12).HasColumnType("nvarchar(12)"); b.Property<byte>("CurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<Guid?>("BaseCurrencyId").HasColumnType("uniqueidentifier"); b.Property<string>("BaseCurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)"); b.Property<byte?>("BaseCurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<decimal?>("PostingExchangeRate").HasColumnType("decimal(19,8)"); b.Property<DateOnly?>("PostingExchangeRateDate").HasColumnType("date"); b.Property<byte?>("PostingExchangeRateType").HasColumnType("tinyint"); b.Property<byte?>("PostingExchangeRateSource").HasColumnType("tinyint");
            b.Property<decimal>("TotalGrossEarnings").HasColumnType("decimal(19,4)"); b.Property<decimal>("TotalDeductions").HasColumnType("decimal(19,4)"); b.Property<decimal>("TotalEmployerContributions").HasColumnType("decimal(19,4)"); b.Property<decimal>("TotalNetPay").HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseGrossEarnings").HasColumnType("decimal(19,4)"); b.Property<decimal>("BaseDeductions").HasColumnType("decimal(19,4)"); b.Property<decimal>("BaseEmployerContributions").HasColumnType("decimal(19,4)"); b.Property<decimal>("BaseNetPay").HasColumnType("decimal(19,4)");
            b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier");
            b.Property<string>("CalculatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("CalculatedAtUtc").HasColumnType("datetimeoffset"); b.Property<string>("ReviewedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("ReviewedAtUtc").HasColumnType("datetimeoffset"); b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset"); b.Property<string>("PostedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("PostedAtUtc").HasColumnType("datetimeoffset"); b.Property<string>("ClosedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("ClosedAtUtc").HasColumnType("datetimeoffset"); b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id"); b.HasIndex("PayrollRunCode").IsUnique().HasDatabaseName("UX_PayrollRuns_Code"); b.HasIndex("PayrollPeriodId", "CurrencyId", "RunType").IsUnique().HasFilter("[Status] <> 7").HasDatabaseName("UX_PayrollRuns_Period_Currency_Type_Active"); b.HasIndex("PayrollPeriodId", "Status").HasDatabaseName("IX_PayrollRuns_Period_Status"); b.HasIndex("CurrencyId").HasDatabaseName("IX_PayrollRuns_Currency"); b.HasIndex("PayrollPolicyId"); b.HasIndex("BaseCurrencyId"); b.HasIndex("JournalEntryId");
            b.ToTable("PayrollRuns", "hr", t => { t.HasCheckConstraint("CK_PayrollRuns_Totals", "[TotalGrossEarnings]>=0 AND [TotalDeductions]>=0 AND [TotalEmployerContributions]>=0 AND [TotalNetPay]>=0 AND [BaseGrossEarnings]>=0 AND [BaseDeductions]>=0 AND [BaseEmployerContributions]>=0 AND [BaseNetPay]>=0"); t.HasCheckConstraint("CK_PayrollRuns_PostingRate", "[PostingExchangeRate] IS NULL OR [PostingExchangeRate] > 0"); });
            b.HasOne("OAS.Domain.Features.Employees.Payroll.PayrollPeriod", null).WithMany().HasForeignKey("PayrollPeriodId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PayrollRuns_Period");
            b.HasOne("OAS.Domain.Features.Employees.Payroll.PayrollPolicy", null).WithMany().HasForeignKey("PayrollPolicyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PayrollRuns_Policy");
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("CurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PayrollRuns_Currency");
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("BaseCurrencyId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollRuns_BaseCurrency");
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany().HasForeignKey("JournalEntryId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollRuns_Journal");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Payroll.EmployeePayroll", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<Guid>("PayrollRunId").HasColumnType("uniqueidentifier"); b.Property<Guid>("PayrollPeriodId").HasColumnType("uniqueidentifier"); b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier"); b.Property<DateOnly>("CoverageFrom").HasColumnType("date"); b.Property<DateOnly>("CoverageTo").HasColumnType("date");
            b.Property<string>("EmployeeCodeSnapshot").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)"); b.Property<string>("EmployeeNameSnapshot").IsRequired().HasMaxLength(220).HasColumnType("nvarchar(220)"); b.Property<string>("JobTitleSnapshot").HasMaxLength(150).HasColumnType("nvarchar(150)"); b.Property<string>("DepartmentSnapshot").HasMaxLength(150).HasColumnType("nvarchar(150)"); b.Property<Guid?>("ContractId").HasColumnType("uniqueidentifier"); b.Property<string>("ContractCodeSnapshot").HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("CurrencyId").HasColumnType("uniqueidentifier"); b.Property<string>("CurrencyCodeSnapshot").IsRequired().HasMaxLength(8).HasColumnType("nvarchar(8)"); b.Property<string>("CurrencySymbolSnapshot").HasMaxLength(12).HasColumnType("nvarchar(12)"); b.Property<byte>("CurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<decimal>("ConfiguredBasicSalarySnapshot").HasColumnType("decimal(19,4)"); b.Property<decimal>("CalculatedBasicSalary").HasColumnType("decimal(19,4)"); b.Property<decimal>("GrossEarnings").HasColumnType("decimal(19,4)"); b.Property<decimal>("TotalDeductions").HasColumnType("decimal(19,4)"); b.Property<decimal>("TotalEmployerContributions").HasColumnType("decimal(19,4)"); b.Property<decimal>("NetPay").HasColumnType("decimal(19,4)"); b.Property<decimal>("BaseGrossEarnings").HasColumnType("decimal(19,4)"); b.Property<decimal>("BaseDeductions").HasColumnType("decimal(19,4)"); b.Property<decimal>("BaseEmployerContributions").HasColumnType("decimal(19,4)"); b.Property<decimal>("BaseNetPay").HasColumnType("decimal(19,4)"); b.Property<byte>("Status").HasColumnType("tinyint");
            b.Property<string>("ReviewedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("ReviewedAtUtc").HasColumnType("datetimeoffset"); b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id"); b.HasIndex("PayrollRunId", "EmployeeId").IsUnique().HasDatabaseName("UX_EmployeePayroll_Run_Employee"); b.HasIndex("PayrollPeriodId", "EmployeeId").IsUnique().HasFilter("[Status] <> 5").HasDatabaseName("UX_EmployeePayroll_Period_Employee"); b.HasIndex("EmployeeId").HasDatabaseName("IX_EmployeePayroll_Employee"); b.HasIndex("Status").HasDatabaseName("IX_EmployeePayroll_Status"); b.HasIndex("ContractId"); b.HasIndex("CurrencyId");
            b.ToTable("EmployeePayrolls", "hr", t => { t.HasCheckConstraint("CK_EmployeePayrolls_Coverage", "[CoverageTo] >= [CoverageFrom]"); t.HasCheckConstraint("CK_EmployeePayrolls_Totals", "[ConfiguredBasicSalarySnapshot]>=0 AND [CalculatedBasicSalary]>=0 AND [GrossEarnings]>=0 AND [TotalDeductions]>=0 AND [TotalEmployerContributions]>=0 AND [NetPay]>=0 AND [BaseGrossEarnings]>=0 AND [BaseDeductions]>=0 AND [BaseEmployerContributions]>=0 AND [BaseNetPay]>=0"); });
            b.HasOne("OAS.Domain.Features.Employees.Payroll.PayrollRun", null).WithMany().HasForeignKey("PayrollRunId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeePayrolls_Run"); b.HasOne("OAS.Domain.Features.Employees.Payroll.PayrollPeriod", null).WithMany().HasForeignKey("PayrollPeriodId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeePayrolls_Period"); b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeePayrolls_Employee"); b.HasOne("OAS.Domain.Features.Employees.Entities.EmployeeContract", null).WithMany().HasForeignKey("ContractId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrolls_Contract"); b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("CurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeePayrolls_Currency");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Payroll.EmployeePayrollSalarySegment", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<Guid>("EmployeePayrollId").HasColumnType("uniqueidentifier"); b.Property<Guid?>("ContractId").HasColumnType("uniqueidentifier"); b.Property<string>("ContractCodeSnapshot").HasMaxLength(40).HasColumnType("nvarchar(40)"); b.Property<Guid>("SalaryStructureId").HasColumnType("uniqueidentifier"); b.Property<string>("SalaryStructureCodeSnapshot").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)"); b.Property<DateOnly>("EffectiveFrom").HasColumnType("date"); b.Property<DateOnly>("EffectiveTo").HasColumnType("date"); b.Property<decimal>("BasicSalaryRateSnapshot").HasColumnType("decimal(19,4)"); b.Property<decimal>("ProrationFactor").HasColumnType("decimal(18,8)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id"); b.HasIndex("EmployeePayrollId").HasDatabaseName("IX_PayrollSalarySegments_Payroll"); b.HasIndex("ContractId"); b.HasIndex("SalaryStructureId"); b.ToTable("EmployeePayrollSalarySegments", "hr", t => { t.HasCheckConstraint("CK_PayrollSalarySegments_Dates", "[EffectiveTo] >= [EffectiveFrom]"); t.HasCheckConstraint("CK_PayrollSalarySegments_Amounts", "[BasicSalaryRateSnapshot]>=0 AND [ProrationFactor]>=0 AND [ProrationFactor]<=1"); }); b.HasOne("OAS.Domain.Features.Employees.Payroll.EmployeePayroll", null).WithMany().HasForeignKey("EmployeePayrollId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PayrollSalarySegments_Payroll"); b.HasOne("OAS.Domain.Features.Employees.Entities.EmployeeContract", null).WithMany().HasForeignKey("ContractId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollSalarySegments_Contract"); b.HasOne("OAS.Domain.Features.Employees.Entities.EmployeeSalaryStructure", null).WithMany().HasForeignKey("SalaryStructureId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PayrollSalarySegments_Structure");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Payroll.EmployeePayrollLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<Guid>("EmployeePayrollId").HasColumnType("uniqueidentifier"); b.Property<int>("LineSequence").HasColumnType("int"); b.Property<Guid?>("SalaryStructureId").HasColumnType("uniqueidentifier"); b.Property<Guid?>("SalaryComponentId").HasColumnType("uniqueidentifier"); b.Property<string>("ComponentCodeSnapshot").HasMaxLength(32).HasColumnType("nvarchar(32)"); b.Property<string>("ComponentNameSnapshot").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)"); b.Property<byte>("ComponentType").HasColumnType("tinyint"); b.Property<byte>("SourceType").HasColumnType("tinyint"); b.Property<string>("SourceModule").HasMaxLength(50).HasColumnType("nvarchar(50)"); b.Property<string>("SourceDocumentType").HasMaxLength(80).HasColumnType("nvarchar(80)"); b.Property<Guid?>("SourceDocumentId").HasColumnType("uniqueidentifier"); b.Property<DateOnly?>("SourceDate").HasColumnType("date"); b.Property<decimal?>("Quantity").HasColumnType("decimal(18,4)"); b.Property<decimal?>("Rate").HasColumnType("decimal(19,6)"); b.Property<decimal>("Amount").HasColumnType("decimal(19,4)"); b.Property<string>("DebitPostingRole").HasMaxLength(50).HasColumnType("nvarchar(50)"); b.Property<string>("CreditPostingRole").HasMaxLength(50).HasColumnType("nvarchar(50)"); b.Property<string>("Description").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id"); b.HasIndex("EmployeePayrollId", "LineSequence").IsUnique().HasDatabaseName("UX_EmployeePayrollLines_Payroll_Sequence"); b.HasIndex("SourceType", "SourceDocumentId").HasDatabaseName("IX_EmployeePayrollLines_Source"); b.HasIndex("SalaryStructureId"); b.HasIndex("SalaryComponentId"); b.ToTable("EmployeePayrollLines", "hr", t => t.HasCheckConstraint("CK_EmployeePayrollLines_Amount", "[Amount]>=0")); b.HasOne("OAS.Domain.Features.Employees.Payroll.EmployeePayroll", null).WithMany().HasForeignKey("EmployeePayrollId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeePayrollLines_Payroll"); b.HasOne("OAS.Domain.Features.Employees.Entities.EmployeeSalaryStructure", null).WithMany().HasForeignKey("SalaryStructureId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrollLines_Structure"); b.HasOne("OAS.Domain.Features.Employees.Entities.SalaryComponent", null).WithMany().HasForeignKey("SalaryComponentId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrollLines_Component");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.EndOfService.EndOfServiceSettlement", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("SettlementCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)"); b.Property<Guid>("EmployeeId").HasColumnType("uniqueidentifier"); b.Property<Guid?>("ContractId").HasColumnType("uniqueidentifier"); b.Property<Guid?>("FinalEmployeePayrollId").HasColumnType("uniqueidentifier"); b.Property<DateOnly>("LastWorkingDate").HasColumnType("date"); b.Property<byte>("Status").HasColumnType("tinyint"); b.Property<Guid>("CurrencyId").HasColumnType("uniqueidentifier"); b.Property<string>("CurrencyCodeSnapshot").IsRequired().HasMaxLength(8).HasColumnType("nvarchar(8)"); b.Property<string>("CurrencySymbolSnapshot").HasMaxLength(12).HasColumnType("nvarchar(12)"); b.Property<byte>("CurrencyDecimalPlacesSnapshot").HasColumnType("tinyint");
            b.Property<decimal>("OutstandingPayrollAmountSnapshot").HasColumnType("decimal(19,4)"); b.Property<decimal>("LeaveSettlementAmount").HasColumnType("decimal(19,4)"); b.Property<decimal>("EndOfServiceBenefitAmount").HasColumnType("decimal(19,4)"); b.Property<decimal>("OtherEarningsAmount").HasColumnType("decimal(19,4)"); b.Property<decimal>("LoanDeductionAmount").HasColumnType("decimal(19,4)"); b.Property<decimal>("OtherDeductionsAmount").HasColumnType("decimal(19,4)"); b.Property<decimal>("GrossSettlementAmount").HasColumnType("decimal(19,4)"); b.Property<decimal>("NetSettlementAmount").HasColumnType("decimal(19,4)"); b.Property<decimal>("BaseGrossSettlementAmount").HasColumnType("decimal(19,4)"); b.Property<decimal>("BaseNetSettlementAmount").HasColumnType("decimal(19,4)"); b.Property<decimal?>("PostingExchangeRate").HasColumnType("decimal(19,8)"); b.Property<DateOnly?>("PostingExchangeRateDate").HasColumnType("date"); b.Property<byte?>("PostingExchangeRateType").HasColumnType("tinyint"); b.Property<byte?>("PostingExchangeRateSource").HasColumnType("tinyint"); b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier"); b.Property<string>("Reason").HasMaxLength(500).HasColumnType("nvarchar(500)"); b.Property<string>("CalculatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("CalculatedAtUtc").HasColumnType("datetimeoffset"); b.Property<string>("ReviewedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("ReviewedAtUtc").HasColumnType("datetimeoffset"); b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("ApprovedAtUtc").HasColumnType("datetimeoffset"); b.Property<string>("PostedBy").HasMaxLength(64).HasColumnType("nvarchar(64)"); b.Property<DateTimeOffset?>("PostedAtUtc").HasColumnType("datetimeoffset");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id"); b.HasIndex("SettlementCode").IsUnique().HasDatabaseName("UX_EndOfService_SettlementCode"); b.HasIndex("EmployeeId").IsUnique().HasFilter("[Status] <> 7").HasDatabaseName("UX_EndOfService_Employee_Active"); b.HasIndex("ContractId"); b.HasIndex("FinalEmployeePayrollId"); b.HasIndex("CurrencyId"); b.HasIndex("JournalEntryId"); b.ToTable("EndOfServiceSettlements", "hr", t => { t.HasCheckConstraint("CK_EndOfService_Amounts", "[OutstandingPayrollAmountSnapshot]>=0 AND [LeaveSettlementAmount]>=0 AND [EndOfServiceBenefitAmount]>=0 AND [OtherEarningsAmount]>=0 AND [LoanDeductionAmount]>=0 AND [OtherDeductionsAmount]>=0 AND [GrossSettlementAmount]>=0 AND [NetSettlementAmount]>=0 AND [BaseGrossSettlementAmount]>=0 AND [BaseNetSettlementAmount]>=0"); t.HasCheckConstraint("CK_EndOfService_Rate", "[PostingExchangeRate] IS NULL OR [PostingExchangeRate]>0"); }); b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EndOfService_Employee"); b.HasOne("OAS.Domain.Features.Employees.Entities.EmployeeContract", null).WithMany().HasForeignKey("ContractId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EndOfService_Contract"); b.HasOne("OAS.Domain.Features.Employees.Payroll.EmployeePayroll", null).WithMany().HasForeignKey("FinalEmployeePayrollId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EndOfService_FinalPayroll"); b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("CurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EndOfService_Currency"); b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany().HasForeignKey("JournalEntryId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EndOfService_Journal");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.EndOfService.EndOfServiceSettlementLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<Guid>("EndOfServiceSettlementId").HasColumnType("uniqueidentifier"); b.Property<int>("LineSequence").HasColumnType("int"); b.Property<byte>("LineType").HasColumnType("tinyint"); b.Property<string>("SourceDocumentType").HasMaxLength(80).HasColumnType("nvarchar(80)"); b.Property<Guid?>("SourceDocumentId").HasColumnType("uniqueidentifier"); b.Property<string>("Description").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)"); b.Property<decimal?>("Quantity").HasColumnType("decimal(18,4)"); b.Property<decimal?>("Rate").HasColumnType("decimal(19,6)"); b.Property<decimal>("Amount").HasColumnType("decimal(19,4)"); b.Property<string>("DebitPostingRole").HasMaxLength(50).HasColumnType("nvarchar(50)"); b.Property<string>("CreditPostingRole").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey("Id"); b.HasIndex("EndOfServiceSettlementId", "LineSequence").IsUnique().HasDatabaseName("UX_EndOfServiceLines_Settlement_Sequence"); b.ToTable("EndOfServiceSettlementLines", "hr", t => t.HasCheckConstraint("CK_EndOfServiceLines_Amount", "[Amount]>=0")); b.HasOne("OAS.Domain.Features.Employees.EndOfService.EndOfServiceSettlement", null).WithMany().HasForeignKey("EndOfServiceSettlementId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EndOfServiceLines_Settlement");
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
            b.Property<Guid?>("RetainedEarningsAccountId").HasColumnType("uniqueidentifier");
            b.Property<byte>("DefaultExchangeRateType").IsRequired().HasColumnType("tinyint");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("RetainedEarningsAccountId");
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
            b.Property<Guid?>("CustomerAdvanceApplicationId").HasColumnType("uniqueidentifier");
            b.Property<byte>("TargetDocumentType").IsRequired().HasColumnType("tinyint");
            b.Property<Guid>("TargetDocumentId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CurrencyId").HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencyCodeSnapshot").HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<decimal>("AllocatedAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal?>("ExchangeRate").HasColumnType("decimal(19,8)");
            b.Property<decimal?>("BaseAllocatedAmount").HasColumnType("decimal(19,4)");
            b.Property<decimal?>("TargetBaseAllocatedAmount").HasColumnType("decimal(19,4)");
            b.Property<DateTime>("AllocatedAtUtc").IsRequired().HasColumnType("datetime2(3)");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PaymentSourceId").HasDatabaseName("IX_PaymentAllocations_PaymentSourceId");
            b.HasIndex("ReceiptVoucherLineId").HasDatabaseName("IX_PaymentAllocations_ReceiptVoucherLineId");
            b.HasIndex("PaymentVoucherLineId").HasDatabaseName("IX_PaymentAllocations_PaymentVoucherLineId");
            b.HasIndex("CustomerAdvanceApplicationId").HasDatabaseName("IX_PaymentAllocations_CustomerAdvanceApplicationId");
            b.HasIndex("TargetDocumentId").HasDatabaseName("IX_PaymentAllocations_TargetDocumentId");
            b.HasIndex("TargetDocumentType", "TargetDocumentId").HasDatabaseName("IX_PaymentAllocations_TargetDocument");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_PaymentAllocations_CurrencyId");
            b.ToTable("tbl_PaymentAllocations", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PaymentAllocations_Amount_Positive", "[AllocatedAmount] > 0");
                t.HasCheckConstraint("CK_PaymentAllocations_BaseAmount_Positive", "[BaseAllocatedAmount] IS NULL OR [BaseAllocatedAmount] > 0");
                t.HasCheckConstraint("CK_PaymentAllocations_TargetBaseAmount_Positive", "[TargetBaseAllocatedAmount] IS NULL OR [TargetBaseAllocatedAmount] > 0");
                t.HasCheckConstraint("CK_PaymentAllocations_ExchangeRate_Positive", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
                t.HasCheckConstraint("CK_PaymentAllocations_TypedSource", "(CASE WHEN [ReceiptVoucherLineId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [PaymentVoucherLineId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [CustomerAdvanceApplicationId] IS NULL THEN 0 ELSE 1 END) = 1");
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
            b.Property<decimal?>("CounterpartyBaseAmount").HasColumnType("decimal(19,4)");
            b.Property<decimal?>("RealizedExchangeDifferenceBase").HasColumnType("decimal(19,4)");
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
                t.HasCheckConstraint("CK_PaymentVoucherLines_CounterpartyBaseAmount_Positive", "[CounterpartyBaseAmount] IS NULL OR [CounterpartyBaseAmount] > 0");
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
            b.Property<byte>("PaymentPlan").IsRequired().HasColumnType("tinyint");
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
            b.HasIndex("PaymentPlan").HasDatabaseName("IX_CustomerOrders_PaymentPlan");
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
            b.Property<Guid?>("PrescriptionRevisionId").HasColumnType("uniqueidentifier");
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
            b.Property<Guid?>("SalesEmployeeId").HasColumnType("uniqueidentifier");
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
            b.Property<byte>("PaymentPlan").IsRequired().HasColumnType("tinyint");
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
            b.HasIndex("PaymentPlan").HasDatabaseName("IX_SalesInvoices_PaymentPlan");
            b.HasIndex("SalesEmployeeId").HasDatabaseName("IX_SalesInvoices_SalesEmployeeId");
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
            b.Property<decimal>("ReturnedQuantity").IsRequired().HasDefaultValue(0m).HasColumnType("decimal(18,3)");
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
                t.HasCheckConstraint("CK_SalesInvoiceLines_ReturnedQuantity_Valid", "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]");
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
            b.Property<Guid?>("PrescriptionRevisionId").HasColumnType("uniqueidentifier");
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

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.Department", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Department", null).WithMany().HasForeignKey("ParentDepartmentId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Departments_Departments_ParentDepartmentId");
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("ManagerEmployeeId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Departments_Employees_ManagerEmployeeId");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.Employee", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.JobTitle", null).WithMany().HasForeignKey("JobTitleId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_Employees_JobTitles_JobTitleId");
            b.HasOne("OAS.Domain.Features.Employees.Entities.Department", null).WithMany().HasForeignKey("DepartmentId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Employees_Departments_DepartmentId");
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("ManagerEmployeeId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Employees_Employees_ManagerEmployeeId");
            b.HasOne("OAS.Domain.Identity.Entities.UserAccount", null).WithMany().HasForeignKey("UserAccountId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Employees_Users_UserAccountId");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.EmployeeContract", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeContracts_Employees_EmployeeId");
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("CurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeContracts_Currencies_CurrencyId");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.EmployeeSalaryStructure", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeSalaryStructures_Employees_EmployeeId");
            b.HasOne("OAS.Domain.Features.Employees.Entities.EmployeeContract", null).WithMany().HasForeignKey("ContractId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeSalaryStructures_Contracts_ContractId");
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("CurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeSalaryStructures_Currencies_CurrencyId");
            b.Navigation("Lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.EmployeeSalaryStructureLine", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.EmployeeSalaryStructure", null).WithMany("Lines").HasForeignKey("EmployeeSalaryStructureId").OnDelete(DeleteBehavior.Cascade).IsRequired().HasConstraintName("FK_EmployeeSalaryStructureLines_Structures_StructureId");
            b.HasOne("OAS.Domain.Features.Employees.Entities.SalaryComponent", null).WithMany().HasForeignKey("SalaryComponentId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeSalaryStructureLines_Components_ComponentId");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Entities.EmployeeDocument", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeDocuments_Employees_EmployeeId");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Time.EmployeeShiftAssignment", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeShiftAssignments_Employees");
            b.HasOne("OAS.Domain.Features.Employees.Time.WorkShift", null).WithMany().HasForeignKey("WorkShiftId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeShiftAssignments_WorkShifts");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Leave.EmployeeLeaveBalance", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_LeaveBalances_Employees");
            b.HasOne("OAS.Domain.Features.Employees.Leave.LeaveType", null).WithMany().HasForeignKey("LeaveTypeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_LeaveBalances_Types");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Leave.LeaveRequest", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_LeaveRequests_Employees");
            b.HasOne("OAS.Domain.Features.Employees.Leave.LeaveType", null).WithMany().HasForeignKey("LeaveTypeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_LeaveRequests_Types");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Time.AttendanceRecord", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_Attendance_Employees");
            b.HasOne("OAS.Domain.Features.Employees.Time.Holiday", null).WithMany().HasForeignKey("SourceHolidayId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Attendance_Holidays");
            b.HasOne("OAS.Domain.Features.Employees.Leave.LeaveRequest", null).WithMany().HasForeignKey("SourceLeaveRequestId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Attendance_LeaveRequests");
            b.HasOne("OAS.Domain.Features.Employees.Time.WorkShift", null).WithMany().HasForeignKey("WorkShiftId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Attendance_WorkShifts");
            b.HasOne("OAS.Domain.Features.Employees.Payroll.EmployeePayroll", null).WithMany().HasForeignKey("EmployeePayrollId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Attendance_EmployeePayrolls");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Overtime.OvertimeRecord", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Time.AttendanceRecord", null).WithMany().HasForeignKey("AttendanceRecordId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Overtime_Attendance");
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_Overtime_Employees");
            b.HasOne("OAS.Domain.Features.Employees.Payroll.EmployeePayroll", null).WithMany().HasForeignKey("EmployeePayrollId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Overtime_EmployeePayrolls");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Loans.EmployeeLoan", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.EmployeeContract", null).WithMany().HasForeignKey("ContractId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeLoans_Contracts");
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("CurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeLoans_Currencies");
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeLoans_Employees");
            b.HasOne("OAS.Domain.Accounting.Entities.PaymentVoucher", null).WithMany().HasForeignKey("PaymentVoucherId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeLoans_PaymentVouchers");
            b.HasOne("OAS.Domain.Features.Employees.Entities.EmployeeSalaryStructure", null).WithMany().HasForeignKey("SalaryStructureId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeLoans_SalaryStructures");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Loans.EmployeeLoanInstallment", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Loans.EmployeeLoan", null).WithMany().HasForeignKey("EmployeeLoanId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_LoanInstallments_Loans");
            b.HasOne("OAS.Domain.Features.Employees.Payroll.EmployeePayroll", null).WithMany().HasForeignKey("EmployeePayrollId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LoanInstallments_EmployeePayroll");
            b.HasOne("OAS.Domain.Features.Employees.EndOfService.EndOfServiceSettlement", null).WithMany().HasForeignKey("EndOfServiceSettlementId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LoanInstallments_EndOfServiceSettlement");
            b.HasOne("OAS.Domain.Accounting.Entities.ReceiptVoucher", null).WithMany().HasForeignKey("ReceiptVoucherId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LoanInstallments_ReceiptVouchers");
        });

        modelBuilder.Entity("OAS.Domain.Features.Employees.Adjustments.EmployeeAdjustment", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("CurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeAdjustments_Currencies");
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeAdjustments_Employees");
            b.HasOne("OAS.Domain.Features.Employees.Entities.SalaryComponent", null).WithMany().HasForeignKey("SalaryComponentId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_EmployeeAdjustments_Components");
            b.HasOne("OAS.Domain.Features.Employees.Payroll.EmployeePayroll", null).WithMany().HasForeignKey("EmployeePayrollId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeAdjustments_EmployeePayroll");
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

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.SupplierCatalogItem", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("SupplierId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("SupplierProductCode").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("SupplierProductName").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<Guid>("PurchaseUnitId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("UnitConversionFactor").IsRequired().HasColumnType("decimal(18,6)");
            b.Property<int?>("LeadTimeDays").HasColumnType("int");
            b.Property<decimal>("MinimumOrderQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<bool>("IsPreferred").IsRequired().HasColumnType("bit");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("SupplierId", "ProductVariantId", "PurchaseUnitId").IsUnique().HasDatabaseName("UX_SupplierCatalogItems_Supplier_ProductVariant_PurchaseUnit");
            b.HasIndex("SupplierId").HasDatabaseName("IX_SupplierCatalogItems_SupplierId");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_SupplierCatalogItems_ProductVariantId");
            b.HasIndex("PurchaseUnitId").HasDatabaseName("IX_SupplierCatalogItems_PurchaseUnitId");
            b.HasIndex("IsActive").HasDatabaseName("IX_SupplierCatalogItems_IsActive");
            b.ToTable("tbl_SupplierCatalogItems", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SupplierCatalogItems_UnitConversionFactor_Positive", "[UnitConversionFactor] > 0");
                t.HasCheckConstraint("CK_SupplierCatalogItems_MinimumOrderQuantity_NonNegative", "[MinimumOrderQuantity] >= 0");
                t.HasCheckConstraint("CK_SupplierCatalogItems_LeadTimeDays_NonNegative", "[LeadTimeDays] IS NULL OR [LeadTimeDays] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.SupplierPriceHistory", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("SupplierCatalogItemId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("CurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("UnitPrice").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<DateOnly>("EffectiveFrom").IsRequired().HasColumnType("date");
            b.Property<DateOnly?>("EffectiveTo").HasColumnType("date");
            b.Property<bool>("IsCurrent").IsRequired().HasColumnType("bit");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("SupplierCatalogItemId").HasDatabaseName("IX_SupplierPriceHistory_CatalogItemId");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_SupplierPriceHistory_CurrencyId");
            b.HasIndex("SupplierCatalogItemId", "CurrencyId").IsUnique().HasDatabaseName("UX_SupplierPriceHistory_Current_Catalog_Currency").HasFilter("[IsCurrent] = 1");
            b.ToTable("tbl_SupplierPriceHistory", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SupplierPriceHistory_UnitPrice_NonNegative", "[UnitPrice] >= 0");
                t.HasCheckConstraint("CK_SupplierPriceHistory_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseRequest", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("RequestCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<byte>("RequestType").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<Guid>("WarehouseId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CustomerOrderId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("RequestDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly?>("RequiredDate").HasColumnType("date");
            b.Property<string>("Reason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<string>("RequestedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("SubmittedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("SubmittedAt").HasColumnType("datetimeoffset");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ApprovedAt").HasColumnType("datetimeoffset");
            b.Property<string>("RejectedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("RejectedAt").HasColumnType("datetimeoffset");
            b.Property<string>("RejectionReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("CancelledAt").HasColumnType("datetimeoffset");
            b.Property<string>("CancellationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("RequestCode").IsUnique().HasDatabaseName("UX_PurchaseRequests_RequestCode");
            b.HasIndex("Status").HasDatabaseName("IX_PurchaseRequests_Status");
            b.HasIndex("RequestDate").HasDatabaseName("IX_PurchaseRequests_RequestDate");
            b.HasIndex("WarehouseId").HasDatabaseName("IX_PurchaseRequests_WarehouseId");
            b.HasIndex("CustomerOrderId").HasDatabaseName("IX_PurchaseRequests_CustomerOrderId");
            b.ToTable("tbl_PurchaseRequests", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseRequestLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PurchaseRequestId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineSequence").IsRequired().HasColumnType("int");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("RequestedQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<DateOnly?>("RequiredDate").HasColumnType("date");
            b.Property<Guid?>("CustomerOrderLineId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("PreferredSupplierId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("ScheduledOrderAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PurchaseRequestId", "LineSequence").IsUnique().HasDatabaseName("UX_PurchaseRequestLines_Request_LineSequence");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_PurchaseRequestLines_ProductVariantId");
            b.HasIndex("ScheduledOrderAtUtc").HasDatabaseName("IX_PurchaseRequestLines_ScheduledOrderAtUtc");
            b.HasIndex("CustomerOrderLineId", "ProductVariantId").HasDatabaseName("IX_PurchaseRequestLines_CustomerOrderLine_ProductVariant");
            b.HasIndex("CustomerOrderLineId").HasDatabaseName("IX_PurchaseRequestLines_CustomerOrderLineId");
            b.HasIndex("PreferredSupplierId").HasDatabaseName("IX_PurchaseRequestLines_PreferredSupplierId");
            b.ToTable("tbl_PurchaseRequestLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseRequestLines_RequestedQuantity_Positive", "[RequestedQuantity] > 0");
                t.HasCheckConstraint("CK_PurchaseRequestLines_LineSequence_Positive", "[LineSequence] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrder", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("PurchaseOrderCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("SupplierId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("DestinationWarehouseId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("OrderDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly?>("ExpectedDeliveryDate").HasColumnType("date");
            b.Property<Guid>("CurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("ExchangeRate").IsRequired().HasColumnType("decimal(19,8)");
            b.Property<DateOnly>("ExchangeRateDate").IsRequired().HasColumnType("date");
            b.Property<byte>("TaxCalculationMode").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("Subtotal").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("DiscountAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TotalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<int>("PaymentTermDays").IsRequired().HasColumnType("int");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<string>("SubmittedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("SubmittedAt").HasColumnType("datetimeoffset");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ApprovedAt").HasColumnType("datetimeoffset");
            b.Property<string>("RejectedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("RejectedAt").HasColumnType("datetimeoffset");
            b.Property<string>("RejectionReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("SentBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("SentAt").HasColumnType("datetimeoffset");
            b.Property<string>("ClosedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ClosedAt").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("CancelledAt").HasColumnType("datetimeoffset");
            b.Property<string>("CancellationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PurchaseOrderCode").IsUnique().HasDatabaseName("UX_PurchaseOrders_PurchaseOrderCode");
            b.HasIndex("SupplierId").HasDatabaseName("IX_PurchaseOrders_SupplierId");
            b.HasIndex("Status").HasDatabaseName("IX_PurchaseOrders_Status");
            b.HasIndex("OrderDate").HasDatabaseName("IX_PurchaseOrders_OrderDate");
            b.HasIndex("DestinationWarehouseId").HasDatabaseName("IX_PurchaseOrders_DestinationWarehouseId");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_PurchaseOrders_CurrencyId");
            b.ToTable("tbl_PurchaseOrders", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseOrders_ExchangeRate_Positive", "[ExchangeRate] > 0");
                t.HasCheckConstraint("CK_PurchaseOrders_PaymentTermDays_NonNegative", "[PaymentTermDays] >= 0");
                t.HasCheckConstraint("CK_PurchaseOrders_Totals_NonNegative", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrderLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PurchaseOrderId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineSequence").IsRequired().HasColumnType("int");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("SupplierCatalogItemId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("PurchaseUnitId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("UnitConversionFactor").IsRequired().HasColumnType("decimal(18,6)");
            b.Property<string>("ProductCodeSnapshot").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ProductNameSnapshot").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<string>("UnitNameSnapshot").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<decimal>("OrderedQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("BaseQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("UnitPrice").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("DiscountAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("NetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TaxRate").IsRequired().HasColumnType("decimal(9,6)");
            b.Property<decimal>("TaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("FinalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<DateOnly?>("ExpectedDeliveryDate").HasColumnType("date");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PurchaseOrderId", "LineSequence").IsUnique().HasDatabaseName("UX_PurchaseOrderLines_Order_LineSequence");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_PurchaseOrderLines_ProductVariantId");
            b.HasIndex("SupplierCatalogItemId").HasDatabaseName("IX_PurchaseOrderLines_SupplierCatalogItemId");
            b.HasIndex("PurchaseUnitId").HasDatabaseName("IX_PurchaseOrderLines_PurchaseUnitId");
            b.ToTable("tbl_PurchaseOrderLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseOrderLines_Quantities_Positive", "[OrderedQuantity] > 0 AND [BaseQuantity] > 0 AND [UnitConversionFactor] > 0");
                t.HasCheckConstraint("CK_PurchaseOrderLines_Amounts_NonNegative", "[UnitPrice] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] >= 0 AND [TaxAmount] >= 0 AND [FinalAmount] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrderLineSource", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PurchaseOrderLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("PurchaseRequestLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("AllocatedQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PurchaseOrderLineId", "PurchaseRequestLineId").IsUnique().HasDatabaseName("UX_PurchaseOrderLineSources_OrderLine_RequestLine");
            b.HasIndex("PurchaseRequestLineId").HasDatabaseName("IX_PurchaseOrderLineSources_RequestLineId");
            b.ToTable("tbl_PurchaseOrderLineSources", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseOrderLineSources_AllocatedQuantity_Positive", "[AllocatedQuantity] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceipt", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("ReceiptCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("PurchaseOrderId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("SupplierId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("WarehouseId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("ReceiptDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly>("PostingDate").IsRequired().HasColumnType("date");
            b.Property<string>("SupplierDeliveryCode").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("InventoryTransactionId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<string>("ConfirmedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ConfirmedAt").HasColumnType("datetimeoffset");
            b.Property<string>("PostedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("PostedAt").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("CancelledAt").HasColumnType("datetimeoffset");
            b.Property<string>("CancellationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("ReceiptCode").IsUnique().HasDatabaseName("UX_PurchaseReceipts_ReceiptCode");
            b.HasIndex("PurchaseOrderId").HasDatabaseName("IX_PurchaseReceipts_PurchaseOrderId");
            b.HasIndex("SupplierId").HasDatabaseName("IX_PurchaseReceipts_SupplierId");
            b.HasIndex("WarehouseId").HasDatabaseName("IX_PurchaseReceipts_WarehouseId");
            b.HasIndex("PostingDate").HasDatabaseName("IX_PurchaseReceipts_PostingDate");
            b.HasIndex("Status").HasDatabaseName("IX_PurchaseReceipts_Status");
            b.HasIndex("InventoryTransactionId").HasDatabaseName("IX_PurchaseReceipts_InventoryTransactionId");
            b.HasIndex("JournalEntryId").HasDatabaseName("IX_PurchaseReceipts_JournalEntryId");
            b.ToTable("tbl_PurchaseReceipts", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceiptLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PurchaseReceiptId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("PurchaseOrderLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineSequence").IsRequired().HasColumnType("int");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("OrderedQuantitySnapshot").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("PreviouslyReceivedQty").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("ReceivedQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("AcceptedQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("RejectedQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("ReturnedQuantity").IsRequired().HasDefaultValue(0m).HasColumnType("decimal(18,3)");
            b.Property<decimal>("BaseAcceptedQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("ActualUnitCost").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TotalAcceptedCost").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<DateOnly?>("ExpiryDate").HasColumnType("date");
            b.Property<string>("BatchCode").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PurchaseReceiptId", "LineSequence").IsUnique().HasDatabaseName("UX_PurchaseReceiptLines_Receipt_LineSequence");
            b.HasIndex("PurchaseOrderLineId").HasDatabaseName("IX_PurchaseReceiptLines_PurchaseOrderLineId");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_PurchaseReceiptLines_ProductVariantId");
            b.ToTable("tbl_PurchaseReceiptLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseReceiptLines_ReceivedQuantity_Positive", "[ReceivedQuantity] > 0");
                t.HasCheckConstraint("CK_PurchaseReceiptLines_Quantities_NonNegative", "[AcceptedQuantity] >= 0 AND [RejectedQuantity] >= 0 AND [BaseAcceptedQuantity] >= 0");
                t.HasCheckConstraint("CK_PurchaseReceiptLines_ActualUnitCost_NonNegative", "[ActualUnitCost] >= 0");
                t.HasCheckConstraint("CK_PurchaseReceiptLines_ReturnedQuantity_Valid", "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [AcceptedQuantity]");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoice", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("PurchaseInvoiceCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<string>("SupplierInvoiceCode").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<Guid>("SupplierId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("InvoiceDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly>("PostingDate").IsRequired().HasColumnType("date");
            b.Property<Guid>("CurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("ExchangeRate").IsRequired().HasColumnType("decimal(19,8)");
            b.Property<DateOnly>("ExchangeRateDate").IsRequired().HasColumnType("date");
            b.Property<byte>("TaxCalculationMode").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("Subtotal").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("DiscountAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TotalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseSubtotal").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseTaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseTotalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<string>("ConfirmedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ConfirmedAt").HasColumnType("datetimeoffset");
            b.Property<string>("PostedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("PostedAt").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("CancelledAt").HasColumnType("datetimeoffset");
            b.Property<string>("CancellationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PurchaseInvoiceCode").IsUnique().HasDatabaseName("UX_PurchaseInvoices_PurchaseInvoiceCode");
            b.HasIndex("SupplierId", "SupplierInvoiceCode").IsUnique().HasDatabaseName("UX_PurchaseInvoices_Supplier_SupplierInvoiceCode").HasFilter("[SupplierInvoiceCode] IS NOT NULL");
            b.HasIndex("SupplierId").HasDatabaseName("IX_PurchaseInvoices_SupplierId");
            b.HasIndex("InvoiceDate").HasDatabaseName("IX_PurchaseInvoices_InvoiceDate");
            b.HasIndex("PostingDate").HasDatabaseName("IX_PurchaseInvoices_PostingDate");
            b.HasIndex("Status").HasDatabaseName("IX_PurchaseInvoices_Status");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_PurchaseInvoices_CurrencyId");
            b.HasIndex("JournalEntryId").HasDatabaseName("IX_PurchaseInvoices_JournalEntryId");
            b.ToTable("tbl_PurchaseInvoices", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseInvoices_ExchangeRate_Positive", "[ExchangeRate] > 0");
                t.HasCheckConstraint("CK_PurchaseInvoices_Totals_NonNegative", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0 AND [BaseSubtotal] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoiceLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PurchaseInvoiceId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineSequence").IsRequired().HasColumnType("int");
            b.Property<Guid?>("PurchaseOrderLineId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("ProductCodeSnapshot").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("DescriptionSnapshot").IsRequired().HasMaxLength(250).HasColumnType("nvarchar(250)");
            b.Property<decimal>("Quantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("UnitPrice").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("GrossAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("DiscountAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("NetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TaxRate").IsRequired().HasColumnType("decimal(9,6)");
            b.Property<decimal>("TaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("FinalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseNetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseTaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseFinalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PurchaseInvoiceId", "LineSequence").IsUnique().HasDatabaseName("UX_PurchaseInvoiceLines_Invoice_LineSequence");
            b.HasIndex("PurchaseOrderLineId").HasDatabaseName("IX_PurchaseInvoiceLines_PurchaseOrderLineId");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_PurchaseInvoiceLines_ProductVariantId");
            b.ToTable("tbl_PurchaseInvoiceLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseInvoiceLines_Quantity_Positive", "[Quantity] > 0");
                t.HasCheckConstraint("CK_PurchaseInvoiceLines_Amounts_NonNegative", "[UnitPrice] >= 0 AND [GrossAmount] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] >= 0 AND [TaxAmount] >= 0 AND [FinalAmount] >= 0 AND [BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseFinalAmount] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoiceReceiptAllocation", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PurchaseInvoiceLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("PurchaseReceiptLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("MatchedQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("MatchedNetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("QuantityVariance").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("PriceVarianceAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TaxVarianceAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<byte>("MatchStatus").IsRequired().HasColumnType("tinyint");
            b.Property<string>("ApprovalReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("ApprovedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("ApprovedAt").HasColumnType("datetimeoffset");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PurchaseInvoiceLineId").HasDatabaseName("IX_PurchaseInvoiceReceiptAllocations_InvoiceLine");
            b.HasIndex("PurchaseReceiptLineId").HasDatabaseName("IX_PurchaseInvoiceReceiptAllocations_ReceiptLine");
            b.HasIndex("MatchStatus").HasDatabaseName("IX_PurchaseInvoiceReceiptAllocations_MatchStatus");
            b.HasIndex("PurchaseInvoiceLineId", "PurchaseReceiptLineId").IsUnique().HasDatabaseName("UX_PurchaseInvoiceReceiptAllocations_InvoiceLine_ReceiptLine");
            b.ToTable("tbl_PurchaseInvoiceReceiptAllocations", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseInvoiceReceiptAllocations_MatchedQuantity_Positive", "[MatchedQuantity] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.SupplierCatalogItem", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null).WithMany()
                .HasForeignKey("SupplierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SupplierCatalogItems_Suppliers_SupplierId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.SupplierCatalogItem", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SupplierCatalogItems_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.SupplierCatalogItem", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Unit", null).WithMany()
                .HasForeignKey("PurchaseUnitId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SupplierCatalogItems_Units_PurchaseUnitId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.SupplierPriceHistory", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.SupplierCatalogItem", null).WithMany()
                .HasForeignKey("SupplierCatalogItemId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SupplierPriceHistory_CatalogItem");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.SupplierPriceHistory", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_SupplierPriceHistory_Currencies_CurrencyId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseRequest", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("WarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseRequests_Warehouses_WarehouseId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseRequest", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.CustomerOrder", null).WithMany()
                .HasForeignKey("CustomerOrderId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchaseRequests_CustomerOrders_CustomerOrderId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseRequestLine", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseRequest", null).WithMany("Lines")
                .HasForeignKey("PurchaseRequestId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseRequestLines_Requests_PurchaseRequestId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseRequestLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseRequestLines_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseRequestLine", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.CustomerOrderLine", null).WithMany()
                .HasForeignKey("CustomerOrderLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchaseRequestLines_CustomerOrderLines_CustomerOrderLineId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseRequestLine", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null).WithMany()
                .HasForeignKey("PreferredSupplierId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchaseRequestLines_Suppliers_PreferredSupplierId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrder", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null).WithMany()
                .HasForeignKey("SupplierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseOrders_Suppliers_SupplierId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrder", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("DestinationWarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseOrders_Warehouses_DestinationWarehouseId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrder", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseOrders_Currencies_CurrencyId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrderLine", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseOrder", null).WithMany("Lines")
                .HasForeignKey("PurchaseOrderId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseOrderLines_Orders_PurchaseOrderId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrderLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseOrderLines_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrderLine", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.SupplierCatalogItem", null).WithMany()
                .HasForeignKey("SupplierCatalogItemId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchaseOrderLines_SupplierCatalogItems_SupplierCatalogItemId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrderLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Unit", null).WithMany()
                .HasForeignKey("PurchaseUnitId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseOrderLines_Units_PurchaseUnitId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrderLineSource", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseOrderLine", null).WithMany()
                .HasForeignKey("PurchaseOrderLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseOrderLineSources_OrderLines_PurchaseOrderLineId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrderLineSource", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseRequestLine", null).WithMany()
                .HasForeignKey("PurchaseRequestLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseOrderLineSources_RequestLines_PurchaseRequestLineId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceipt", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseOrder", null).WithMany()
                .HasForeignKey("PurchaseOrderId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseReceipts_Orders_PurchaseOrderId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceipt", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null).WithMany()
                .HasForeignKey("SupplierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseReceipts_Suppliers_SupplierId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceipt", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany()
                .HasForeignKey("WarehouseId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseReceipts_Warehouses_WarehouseId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceipt", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.InventoryTransaction", null).WithMany()
                .HasForeignKey("InventoryTransactionId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchaseReceipts_InventoryTransactions_InventoryTransactionId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceipt", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany()
                .HasForeignKey("JournalEntryId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchaseReceipts_JournalEntries_JournalEntryId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceiptLine", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseReceipt", null).WithMany("Lines")
                .HasForeignKey("PurchaseReceiptId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseReceiptLines_Receipts_PurchaseReceiptId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceiptLine", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseOrderLine", null).WithMany()
                .HasForeignKey("PurchaseOrderLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseReceiptLines_OrderLines_PurchaseOrderLineId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceiptLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseReceiptLines_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoice", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null).WithMany()
                .HasForeignKey("SupplierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseInvoices_Suppliers_SupplierId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoice", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany()
                .HasForeignKey("CurrencyId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseInvoices_Currencies_CurrencyId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoice", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany()
                .HasForeignKey("JournalEntryId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchaseInvoices_JournalEntries_JournalEntryId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoiceLine", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseInvoice", null).WithMany("Lines")
                .HasForeignKey("PurchaseInvoiceId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseInvoiceLines_Invoices_PurchaseInvoiceId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoiceLine", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseOrderLine", null).WithMany()
                .HasForeignKey("PurchaseOrderLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PurchaseInvoiceLines_OrderLines_PurchaseOrderLineId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoiceLine", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany()
                .HasForeignKey("ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseInvoiceLines_ProductVariants_ProductVariantId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoiceReceiptAllocation", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseInvoiceLine", null).WithMany()
                .HasForeignKey("PurchaseInvoiceLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseInvoiceReceiptAllocations_InvoiceLines_PurchaseInvoiceLineId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoiceReceiptAllocation", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseReceiptLine", null).WithMany()
                .HasForeignKey("PurchaseReceiptLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_PurchaseInvoiceReceiptAllocations_ReceiptLines_PurchaseReceiptLineId");
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseRequest", b =>
        {
            b.Navigation("Lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseOrder", b =>
        {
            b.Navigation("Lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReceipt", b =>
        {
            b.Navigation("Lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseInvoice", b =>
        {
            b.Navigation("Lines").UsePropertyAccessMode(PropertyAccessMode.Field);
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

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.AccountingSettings", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Account", null).WithMany()
                .HasForeignKey("RetainedEarningsAccountId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AccountingSettings_RetainedEarningsAccount");
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

        // Added with 20261003113000_AddReturnsCommissionsOpticalProduction
        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesReturn", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("ReturnCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("SalesInvoiceId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("CustomerId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("ReturnDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly>("PostingDate").IsRequired().HasColumnType("date");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<Guid>("CurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencyCodeSnapshot").IsRequired().HasMaxLength(10).HasColumnType("nvarchar(10)");
            b.Property<byte>("CurrencyDecimalPlacesSnapshot").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("ExchangeRate").IsRequired().HasColumnType("decimal(19,8)");
            b.Property<DateOnly>("ExchangeRateDate").IsRequired().HasColumnType("date");
            b.Property<byte>("ExchangeRateType").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("ExchangeRateSource").IsRequired().HasColumnType("tinyint");
            b.Property<Guid>("BaseCurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("BaseCurrencyCodeSnapshot").IsRequired().HasMaxLength(10).HasColumnType("nvarchar(10)");
            b.Property<byte>("BaseCurrencyDecimalPlacesSnapshot").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("NetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TotalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseNetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseTaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseTotalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<string>("Reason").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("ConfirmedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("ConfirmedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("PostedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("PostedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("CancelledAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CancellationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("ReturnCode").IsUnique().HasDatabaseName("UX_SalesReturns_ReturnCode");
            b.HasIndex("SalesInvoiceId").HasDatabaseName("IX_SalesReturns_SalesInvoiceId");
            b.HasIndex("CustomerId").HasDatabaseName("IX_SalesReturns_CustomerId");
            b.HasIndex("PostingDate").HasDatabaseName("IX_SalesReturns_PostingDate");
            b.HasIndex("Status").HasDatabaseName("IX_SalesReturns_Status");
            b.HasIndex("JournalEntryId").HasDatabaseName("IX_SalesReturns_JournalEntryId");
            b.ToTable("tbl_SalesReturns", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SalesReturns_ExchangeRate_Positive", "[ExchangeRate] > 0");
                t.HasCheckConstraint("CK_SalesReturns_Amounts_NonNegative", "[NetAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0 AND [BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesReturnLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("SalesReturnId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineNumber").IsRequired().HasColumnType("int");
            b.Property<Guid>("SalesInvoiceLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<byte>("LineType").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("ProductVariantId").HasColumnType("uniqueidentifier");
            b.Property<Guid?>("WarehouseId").HasColumnType("uniqueidentifier");
            b.Property<string>("ProductCodeSnapshot").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ProductNameSnapshot").IsRequired().HasMaxLength(250).HasColumnType("nvarchar(250)");
            b.Property<decimal>("Quantity").IsRequired().HasColumnType("decimal(19,3)");
            b.Property<decimal>("NetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("FinalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseNetAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseTaxAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseFinalAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal?>("UnitCostSnapshot").HasColumnType("decimal(19,4)");
            b.Property<decimal?>("TotalCostSnapshot").HasColumnType("decimal(19,4)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("SalesReturnId", "LineNumber").IsUnique().HasDatabaseName("UX_SalesReturnLines_Return_LineNumber");
            b.HasIndex("SalesInvoiceLineId").HasDatabaseName("IX_SalesReturnLines_SalesInvoiceLineId");
            b.ToTable("tbl_SalesReturnLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_SalesReturnLines_Quantity_Positive", "[Quantity] > 0");
                t.HasCheckConstraint("CK_SalesReturnLines_Amounts_NonNegative", "[NetAmount] >= 0 AND [TaxAmount] >= 0 AND [FinalAmount] >= 0 AND [BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseFinalAmount] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReturn", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("ReturnCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("PurchaseReceiptId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("PurchaseInvoiceId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("SupplierId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("WarehouseId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("ReturnDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly>("PostingDate").IsRequired().HasColumnType("date");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("ReceiptCostBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("SupplierNetBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("SupplierTaxBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("SupplierGrossBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("InventoryCostBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("PurchasePriceVarianceBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<string>("Reason").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<Guid?>("JournalEntryId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("ConfirmedAt").HasColumnType("datetimeoffset");
            b.Property<string>("ConfirmedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("PostedAt").HasColumnType("datetimeoffset");
            b.Property<string>("PostedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("CancelledAt").HasColumnType("datetimeoffset");
            b.Property<string>("CancelledBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("CancellationReason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("ReturnCode").IsUnique().HasDatabaseName("UX_PurchaseReturns_ReturnCode");
            b.HasIndex("PurchaseReceiptId").HasDatabaseName("IX_PurchaseReturns_PurchaseReceiptId");
            b.HasIndex("PurchaseInvoiceId").HasDatabaseName("IX_PurchaseReturns_PurchaseInvoiceId");
            b.HasIndex("SupplierId").HasDatabaseName("IX_PurchaseReturns_SupplierId");
            b.HasIndex("Status").HasDatabaseName("IX_PurchaseReturns_Status");
            b.HasIndex("PostingDate").HasDatabaseName("IX_PurchaseReturns_PostingDate");
            b.ToTable("tbl_PurchaseReturns", "dbo", t => t.HasCheckConstraint("CK_PurchaseReturns_Amounts_NonNegative", "[ReceiptCostBaseAmount] >= 0 AND [SupplierNetBaseAmount] >= 0 AND [SupplierTaxBaseAmount] >= 0 AND [SupplierGrossBaseAmount] >= 0 AND [InventoryCostBaseAmount] >= 0"));
        });

        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReturnLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("PurchaseReturnId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<int>("LineNumber").IsRequired().HasColumnType("int");
            b.Property<Guid>("PurchaseReceiptLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("PurchaseInvoiceLineId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Quantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("BaseQuantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal>("ReceiptUnitCostBase").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("ReceiptCostBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("SupplierNetBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("SupplierTaxBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("SupplierGrossBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal?>("InventoryUnitCostBase").HasColumnType("decimal(19,4)");
            b.Property<decimal?>("InventoryCostBaseAmount").HasColumnType("decimal(19,4)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("PurchaseReturnId", "LineNumber").IsUnique().HasDatabaseName("UX_PurchaseReturnLines_Return_LineNumber");
            b.HasIndex("PurchaseReceiptLineId").HasDatabaseName("IX_PurchaseReturnLines_ReceiptLineId");
            b.HasIndex("PurchaseInvoiceLineId").HasDatabaseName("IX_PurchaseReturnLines_InvoiceLineId");
            b.ToTable("tbl_PurchaseReturnLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_PurchaseReturnLines_Quantity_Positive", "[Quantity] > 0 AND [BaseQuantity] > 0");
                t.HasCheckConstraint("CK_PurchaseReturnLines_Amounts_NonNegative", "[ReceiptUnitCostBase] >= 0 AND [ReceiptCostBaseAmount] >= 0 AND [SupplierNetBaseAmount] >= 0 AND [SupplierTaxBaseAmount] >= 0 AND [SupplierGrossBaseAmount] >= 0 AND ([InventoryUnitCostBase] IS NULL OR [InventoryUnitCostBase] >= 0) AND ([InventoryCostBaseAmount] IS NULL OR [InventoryCostBaseAmount] >= 0)");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CommissionRule", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("Code").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<string>("Name").IsRequired().HasMaxLength(160).HasColumnType("nvarchar(160)");
            b.Property<Guid?>("EmployeeId").HasColumnType("uniqueidentifier");
            b.Property<decimal>("RatePercent").IsRequired().HasColumnType("decimal(9,4)");
            b.Property<DateOnly>("EffectiveFrom").IsRequired().HasColumnType("date");
            b.Property<DateOnly?>("EffectiveTo").HasColumnType("date");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique().HasDatabaseName("UX_CommissionRules_Code");
            b.HasIndex("EmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive").HasDatabaseName("IX_CommissionRules_Resolution");
            b.ToTable("tbl_CommissionRules", "dbo", t => t.HasCheckConstraint("CK_CommissionRules_Rate", "[RatePercent] >= 0 AND [RatePercent] <= 100"));
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CommissionStatement", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("StatementCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("EmployeeId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("FromDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly>("ToDate").IsRequired().HasColumnType("date");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("SalesBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("ReturnsBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("CommissionBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<DateTimeOffset?>("CalculatedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("CalculatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("FinalizedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("FinalizedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("StatementCode").IsUnique().HasDatabaseName("UX_CommissionStatements_StatementCode");
            b.HasIndex("EmployeeId", "FromDate", "ToDate").HasDatabaseName("IX_CommissionStatements_Employee_Period");
            b.ToTable("tbl_CommissionStatements", "dbo", t => t.HasCheckConstraint("CK_CommissionStatements_Period", "[ToDate] >= [FromDate]"));
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CommissionEntry", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("CommissionStatementId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("EmployeeId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("SourceDocumentType").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("SourceDocumentId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("SourceLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("SalesInvoiceId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("OriginalSalesInvoiceLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("SalesReturnId").HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("SourceDate").IsRequired().HasColumnType("date");
            b.Property<decimal>("BaseSalesAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("RatePercent").IsRequired().HasColumnType("decimal(9,4)");
            b.Property<decimal>("CommissionBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<Guid>("CommissionRuleId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("RuleCodeSnapshot").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<string>("RuleNameSnapshot").IsRequired().HasMaxLength(160).HasColumnType("nvarchar(160)");
            b.Property<bool>("IsReversal").IsRequired().HasColumnType("bit");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("SourceDocumentType", "SourceLineId").IsUnique().HasDatabaseName("UX_CommissionEntries_SourceLine");
            b.HasIndex("OriginalSalesInvoiceLineId").HasDatabaseName("IX_CommissionEntries_OriginalSalesInvoiceLineId");
            b.HasIndex("EmployeeId").HasDatabaseName("IX_CommissionEntries_EmployeeId");
            b.ToTable("tbl_CommissionEntries", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.OpticalProductionJob", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("JobCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("SalesInvoiceId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("SalesInvoiceLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("CustomerId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("WarehouseId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("JobDate").IsRequired().HasColumnType("date");
            b.Property<DateOnly?>("TargetDate").HasColumnType("date");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("InventoryTransactionId").HasColumnType("uniqueidentifier");
            b.Property<decimal>("MaterialCostBase").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<Guid?>("RemakeOfJobId").HasColumnType("uniqueidentifier");
            b.Property<int>("RemakeNumber").IsRequired().HasColumnType("int");
            b.Property<byte?>("LastQcResult").HasColumnType("tinyint");
            b.Property<int>("QcAttemptCount").IsRequired().HasColumnType("int");
            b.Property<int>("FailedQcCount").IsRequired().HasColumnType("int");
            b.Property<string>("LastQcNotes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<bool>("LastQcWasBreakage").IsRequired().HasColumnType("bit");
            b.Property<DateTimeOffset?>("ReleasedAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("StartedAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("QcAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("FailedAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("CompletedAtUtc").HasColumnType("datetimeoffset");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id");
            b.HasIndex("JobCode").IsUnique().HasDatabaseName("UX_OpticalProductionJobs_JobCode");
            b.HasIndex("SalesInvoiceLineId").IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_OpticalProductionJobs_ActiveInvoiceLine");
            b.HasIndex("Status").HasDatabaseName("IX_OpticalProductionJobs_Status");
            b.HasIndex("RemakeOfJobId").HasDatabaseName("IX_OpticalProductionJobs_RemakeOfJobId");
            b.ToTable("tbl_OpticalProductionJobs", "dbo", t =>
            {
                t.HasCheckConstraint("CK_OpticalProductionJobs_RemakeNumber", "[RemakeNumber] >= 0");
                t.HasCheckConstraint("CK_OpticalProductionJobs_QcCounters", "[QcAttemptCount] >= 0 AND [FailedQcCount] >= 0 AND [FailedQcCount] <= [QcAttemptCount]");
                t.HasCheckConstraint("CK_OpticalProductionJobs_MaterialCost", "[MaterialCostBase] >= 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.OpticalProductionMaterial", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<Guid>("OpticalProductionJobId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Quantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<decimal?>("UnitCostSnapshot").HasColumnType("decimal(19,4)");
            b.Property<decimal?>("TotalCostSnapshot").HasColumnType("decimal(19,4)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.HasKey("Id");
            b.HasIndex("OpticalProductionJobId", "ProductVariantId").IsUnique().HasDatabaseName("UX_OpticalProductionMaterials_Job_Variant");
            b.ToTable("tbl_OpticalProductionMaterials", "dbo", t => t.HasCheckConstraint("CK_OpticalProductionMaterials_Qty", "[Quantity] > 0"));
        });


        modelBuilder.Entity("OAS.Domain.Entities.Inventory.LensVariantDetail", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("ProductVariantId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal?>("SPH").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("CYL").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("ADD").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("BaseCurve").HasColumnType("decimal(6,2)");
            b.Property<decimal?>("Diameter").HasColumnType("decimal(6,2)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id").HasName("PK_tbl_LensVariantDetails");
            b.HasIndex("ProductVariantId").IsUnique().HasDatabaseName("UX_LensVariantDetails_ProductVariantId");
            b.HasIndex("SPH", "CYL", "ADD").HasDatabaseName("IX_LensVariantDetails_SPH_CYL_ADD");
            b.ToTable("tbl_LensVariantDetails", "dbo", t =>
            {
                t.HasCheckConstraint("CK_LensVariantDetails_ADD", "[ADD] IS NULL OR [ADD] >= 0");
                t.HasCheckConstraint("CK_LensVariantDetails_BaseCurve", "[BaseCurve] IS NULL OR [BaseCurve] > 0");
                t.HasCheckConstraint("CK_LensVariantDetails_Diameter", "[Diameter] IS NULL OR [Diameter] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerOrderLineOpticalSnapshot", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("CustomerOrderLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<byte>("MeasurementSource").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("PrescriptionRevisionId").HasColumnType("uniqueidentifier");
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
            b.Property<string>("LensTypeSnapshot").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("MaterialSnapshot").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("CoatingSnapshot").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<decimal?>("RefractiveIndexSnapshot").HasColumnType("decimal(5,3)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id").HasName("PK_tbl_CustomerOrderLineOpticalSnapshots");
            b.HasIndex("CustomerOrderLineId").IsUnique().HasDatabaseName("UX_CustomerOrderLineOpticalSnapshots_CustomerOrderLineId");
            b.HasIndex("PrescriptionRevisionId").HasDatabaseName("IX_CustomerOrderLineOpticalSnapshots_PrescriptionRevisionId");
            b.HasIndex("Eye").HasDatabaseName("IX_CustomerOrderLineOpticalSnapshots_Eye");
            b.ToTable("tbl_CustomerOrderLineOpticalSnapshots", "dbo", t =>
            {
                t.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_Source", "([MeasurementSource] = 1 AND [PrescriptionRevisionId] IS NOT NULL) OR ([MeasurementSource] = 2)");
                t.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_Axis", "[Axis] IS NULL OR ([Axis] >= 0 AND [Axis] <= 180)");
                t.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_ADD", "[ADD] IS NULL OR [ADD] >= 0");
                t.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_Prism", "[Prism] IS NULL OR [Prism] >= 0");
                t.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_PD", "[PD] IS NULL OR [PD] > 0");
                t.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_MonocularPD", "[MonocularPD] IS NULL OR [MonocularPD] > 0");
                t.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_FittingHeight", "[FittingHeight] IS NULL OR [FittingHeight] > 0");
                t.HasCheckConstraint("CK_CustomerOrderLineOpticalSnapshots_RefractiveIndex", "[RefractiveIndexSnapshot] IS NULL OR [RefractiveIndexSnapshot] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CustomerAdvance", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("AdvanceNumber").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("CustomerId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("CustomerOrderId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("ReceiptVoucherLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("CurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("CurrencyCodeSnapshot").IsRequired().HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<string>("CurrencySymbolSnapshot").HasMaxLength(12).HasColumnType("nvarchar(12)");
            b.Property<byte>("CurrencyDecimalPlacesSnapshot").IsRequired().HasColumnType("tinyint");
            b.Property<Guid>("BaseCurrencyId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<string>("BaseCurrencyCodeSnapshot").IsRequired().HasMaxLength(8).HasColumnType("nvarchar(8)");
            b.Property<byte>("BaseCurrencyDecimalPlacesSnapshot").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("Amount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("ExchangeRate").IsRequired().HasColumnType("decimal(19,8)");
            b.Property<DateOnly>("ExchangeRateDate").IsRequired().HasColumnType("date");
            b.Property<byte>("ExchangeRateType").IsRequired().HasColumnType("tinyint");
            b.Property<byte>("ExchangeRateSource").IsRequired().HasColumnType("tinyint");
            b.Property<decimal>("BaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("AppliedAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseAppliedAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<DateTime>("ReceivedAtUtc").IsRequired().HasColumnType("datetime2(3)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id").HasName("PK_tbl_CustomerAdvances");
            b.HasIndex("AdvanceNumber").IsUnique().HasDatabaseName("UX_CustomerAdvances_AdvanceNumber");
            b.HasIndex("ReceiptVoucherLineId").IsUnique().HasDatabaseName("UX_CustomerAdvances_ReceiptVoucherLineId");
            b.HasIndex("CustomerId").HasDatabaseName("IX_CustomerAdvances_CustomerId");
            b.HasIndex("CustomerOrderId").HasDatabaseName("IX_CustomerAdvances_CustomerOrderId");
            b.HasIndex("Status").HasDatabaseName("IX_CustomerAdvances_Status");
            b.HasIndex("CurrencyId").HasDatabaseName("IX_CustomerAdvances_CurrencyId");
            b.HasIndex("BaseCurrencyId").HasDatabaseName("IX_CustomerAdvances_BaseCurrencyId");
            b.ToTable("tbl_CustomerAdvances", "dbo", t =>
            {
                t.HasCheckConstraint("CK_CustomerAdvances_Amount", "[Amount] > 0");
                t.HasCheckConstraint("CK_CustomerAdvances_BaseAmount", "[BaseAmount] > 0");
                t.HasCheckConstraint("CK_CustomerAdvances_ExchangeRate", "[ExchangeRate] > 0");
                t.HasCheckConstraint("CK_CustomerAdvances_AppliedAmount", "[AppliedAmount] >= 0 AND [AppliedAmount] <= [Amount]");
                t.HasCheckConstraint("CK_CustomerAdvances_BaseAppliedAmount", "[BaseAppliedAmount] >= 0 AND [BaseAppliedAmount] <= [BaseAmount]");
            });
        });

        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CustomerAdvanceApplication", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("CustomerAdvanceId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("SalesInvoiceId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<decimal>("Amount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("BaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<decimal>("TargetBaseAmount").IsRequired().HasColumnType("decimal(19,4)");
            b.Property<Guid>("JournalEntryId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateTime>("AppliedAtUtc").IsRequired().HasColumnType("datetime2(3)");
            b.Property<string>("AppliedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id").HasName("PK_tbl_CustomerAdvanceApplications");
            b.HasIndex("CustomerAdvanceId").HasDatabaseName("IX_CustomerAdvanceApplications_AdvanceId");
            b.HasIndex("SalesInvoiceId").HasDatabaseName("IX_CustomerAdvanceApplications_SalesInvoiceId");
            b.HasIndex("JournalEntryId").HasDatabaseName("IX_CustomerAdvanceApplications_JournalEntryId");
            b.ToTable("tbl_CustomerAdvanceApplications", "dbo", t =>
            {
                t.HasCheckConstraint("CK_CustomerAdvanceApplications_Amount", "[Amount] > 0");
                t.HasCheckConstraint("CK_CustomerAdvanceApplications_BaseAmount", "[BaseAmount] > 0");
                t.HasCheckConstraint("CK_CustomerAdvanceApplications_TargetBaseAmount", "[TargetBaseAmount] > 0");
            });
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.OpticalJob", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<string>("JobCode").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<Guid>("CustomerOrderId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("SalesInvoiceId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("CustomerId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<DateOnly?>("RequiredDate").HasColumnType("date");
            b.Property<byte>("Status").IsRequired().HasColumnType("tinyint");
            b.Property<Guid?>("AssignedTechnicianId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset?>("StartedAtUtc").HasColumnType("datetimeoffset");
            b.Property<DateTimeOffset?>("CompletedAtUtc").HasColumnType("datetimeoffset");
            b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<bool>("IsActive").IsRequired().HasColumnType("bit");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id").HasName("PK_tbl_OpticalJobs");
            b.HasIndex("JobCode").IsUnique().HasDatabaseName("UX_OpticalJobs_JobCode");
            b.HasIndex("CustomerOrderId").IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_OpticalJobs_CustomerOrderId");
            b.HasIndex("Status").HasDatabaseName("IX_OpticalJobs_Status");
            b.HasIndex("AssignedTechnicianId").HasDatabaseName("IX_OpticalJobs_AssignedTechnicianId");
            b.HasIndex("RequiredDate").HasDatabaseName("IX_OpticalJobs_RequiredDate");
            b.HasIndex("SalesInvoiceId").HasDatabaseName("IX_OpticalJobs_SalesInvoiceId");
            b.ToTable("tbl_OpticalJobs", "dbo");
        });

        modelBuilder.Entity("OAS.Domain.Sales.Entities.OpticalJobLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("CreatedAtUtc").IsRequired().HasColumnType("datetimeoffset").HasColumnName("CreatedAt");
            b.Property<string>("CreatedBy").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTimeOffset?>("LastModifiedAtUtc").HasColumnType("datetimeoffset").HasColumnName("UpdatedAt");
            b.Property<string>("LastModifiedBy").HasMaxLength(64).HasColumnType("nvarchar(64)").HasColumnName("UpdatedBy");
            b.Property<Guid>("OpticalJobId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid>("CustomerOrderLineId").IsRequired().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("ProductVariantId").HasColumnType("uniqueidentifier");
            b.Property<int>("LineNumber").IsRequired().HasColumnType("int");
            b.Property<byte>("LineType").IsRequired().HasColumnType("tinyint");
            b.Property<byte?>("Eye").HasColumnType("tinyint");
            b.Property<string>("DescriptionSnapshot").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<decimal>("Quantity").IsRequired().HasColumnType("decimal(18,3)");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<string>("CreatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("CreatedFromDevice");
            b.Property<string>("UpdatedFromDevice").HasMaxLength(256).HasColumnType("nvarchar(256)").HasColumnName("UpdatedFromDevice");
            b.HasKey("Id").HasName("PK_tbl_OpticalJobLines");
            b.HasIndex("OpticalJobId", "LineNumber").IsUnique().HasDatabaseName("UX_OpticalJobLines_Job_LineNumber");
            b.HasIndex("OpticalJobId", "CustomerOrderLineId").IsUnique().HasDatabaseName("UX_OpticalJobLines_Job_CustomerOrderLine");
            b.HasIndex("CustomerOrderLineId").HasDatabaseName("IX_OpticalJobLines_CustomerOrderLineId");
            b.HasIndex("ProductVariantId").HasDatabaseName("IX_OpticalJobLines_ProductVariantId");
            b.ToTable("tbl_OpticalJobLines", "dbo", t =>
            {
                t.HasCheckConstraint("CK_OpticalJobLines_LineNumber", "[LineNumber] > 0");
                t.HasCheckConstraint("CK_OpticalJobLines_Quantity", "[Quantity] > 0");
            });
        });


        // Relationships for foundation gap completion
        modelBuilder.Entity("OAS.Domain.Entities.Inventory.LensVariantDetail", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithOne()
                .HasForeignKey("OAS.Domain.Entities.Inventory.LensVariantDetail", "ProductVariantId")
                .OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_LensVariantDetails_ProductVariants_ProductVariantId");
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.CustomerOrderLineOpticalSnapshot", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.CustomerOrderLine", null).WithOne()
                .HasForeignKey("OAS.Domain.Sales.Entities.CustomerOrderLineOpticalSnapshot", "CustomerOrderLineId")
                .OnDelete(DeleteBehavior.Restrict).IsRequired()
                .HasConstraintName("FK_CustomerOrderLineOpticalSnapshots_CustomerOrderLines_CustomerOrderLineId");
            b.HasOne("OAS.Domain.Sales.Entities.PrescriptionRevision", null).WithMany()
                .HasForeignKey("PrescriptionRevisionId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CustomerOrderLineOpticalSnapshots_PrescriptionRevisions_PrescriptionRevisionId");
        });
        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CustomerAdvance", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany().HasForeignKey("CustomerId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CustomerAdvances_Customers_CustomerId");
            b.HasOne("OAS.Domain.Sales.Entities.CustomerOrder", null).WithMany().HasForeignKey("CustomerOrderId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CustomerAdvances_CustomerOrders_CustomerOrderId");
            b.HasOne("OAS.Domain.Accounting.Entities.ReceiptVoucherLine", null).WithMany().HasForeignKey("ReceiptVoucherLineId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CustomerAdvances_ReceiptVoucherLines_ReceiptVoucherLineId");
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("CurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CustomerAdvances_Currencies_CurrencyId");
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("BaseCurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CustomerAdvances_BaseCurrencies_BaseCurrencyId");
        });
        modelBuilder.Entity("OAS.Domain.Accounting.Entities.CustomerAdvanceApplication", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.CustomerAdvance", null).WithMany().HasForeignKey("CustomerAdvanceId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CustomerAdvanceApplications_CustomerAdvances_CustomerAdvanceId");
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoice", null).WithMany().HasForeignKey("SalesInvoiceId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CustomerAdvanceApplications_SalesInvoices_SalesInvoiceId");
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany().HasForeignKey("JournalEntryId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CustomerAdvanceApplications_JournalEntries_JournalEntryId");
        });
        modelBuilder.Entity("OAS.Domain.Accounting.Entities.PaymentAllocation", b =>
        {
            b.HasOne("OAS.Domain.Accounting.Entities.CustomerAdvanceApplication", null).WithMany()
                .HasForeignKey("CustomerAdvanceApplicationId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentAllocations_CustomerAdvanceApplication");
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.OpticalJob", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.CustomerOrder", null).WithMany().HasForeignKey("CustomerOrderId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_OpticalJobs_CustomerOrders_CustomerOrderId");
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoice", null).WithMany().HasForeignKey("SalesInvoiceId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalJobs_SalesInvoices_SalesInvoiceId");
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany().HasForeignKey("CustomerId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_OpticalJobs_Customers_CustomerId");
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("AssignedTechnicianId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalJobs_Employees_AssignedTechnicianId");
            b.HasMany("OAS.Domain.Sales.Entities.OpticalJobLine", "Lines").WithOne().HasForeignKey("OpticalJobId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_OpticalJobLines_OpticalJobs_OpticalJobId");
            b.Navigation("Lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.OpticalJobLine", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.CustomerOrderLine", null).WithMany().HasForeignKey("CustomerOrderLineId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_OpticalJobLines_CustomerOrderLines_CustomerOrderLineId");
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany().HasForeignKey("ProductVariantId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalJobLines_ProductVariants_ProductVariantId");
        });

        // Relationships for returns / commissions / optical production
        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesInvoice", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany()
                .HasForeignKey("SalesEmployeeId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SalesInvoices_Employees_SalesEmployeeId");
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesReturn", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoice", null).WithMany().HasForeignKey("SalesInvoiceId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_SalesReturns_SalesInvoices_SalesInvoiceId");
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany().HasForeignKey("CustomerId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_SalesReturns_Customers_CustomerId");
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("CurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_SalesReturns_Currencies_CurrencyId");
            b.HasOne("OAS.Domain.Accounting.Entities.Currency", null).WithMany().HasForeignKey("BaseCurrencyId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_SalesReturns_BaseCurrencies_BaseCurrencyId");
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany().HasForeignKey("JournalEntryId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturns_JournalEntries_JournalEntryId");
            b.HasMany("OAS.Domain.Sales.Entities.SalesReturnLine", "Lines").WithOne().HasForeignKey("SalesReturnId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_SalesReturnLines_SalesReturns_SalesReturnId");
            b.Navigation("Lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.SalesReturnLine", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoiceLine", null).WithMany().HasForeignKey("SalesInvoiceLineId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_SalesReturnLines_SalesInvoiceLines_SalesInvoiceLineId");
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany().HasForeignKey("ProductVariantId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturnLines_ProductVariants_ProductVariantId");
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany().HasForeignKey("WarehouseId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesReturnLines_Warehouses_WarehouseId");
        });
        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReturn", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseReceipt", null).WithMany().HasForeignKey("PurchaseReceiptId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PurchaseReturns_Receipts_PurchaseReceiptId");
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseInvoice", null).WithMany().HasForeignKey("PurchaseInvoiceId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturns_Invoices_PurchaseInvoiceId");
            b.HasOne("OAS.Domain.Accounting.Entities.Supplier", null).WithMany().HasForeignKey("SupplierId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PurchaseReturns_Suppliers_SupplierId");
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany().HasForeignKey("WarehouseId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PurchaseReturns_Warehouses_WarehouseId");
            b.HasOne("OAS.Domain.Accounting.Entities.JournalEntry", null).WithMany().HasForeignKey("JournalEntryId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturns_Journals_JournalEntryId");
            b.HasMany("OAS.Domain.Purchasing.Entities.PurchaseReturnLine", "Lines").WithOne().HasForeignKey("PurchaseReturnId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PurchaseReturnLines_Returns_PurchaseReturnId");
            b.Navigation("Lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        modelBuilder.Entity("OAS.Domain.Purchasing.Entities.PurchaseReturnLine", b =>
        {
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseReceiptLine", null).WithMany().HasForeignKey("PurchaseReceiptLineId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PurchaseReturnLines_ReceiptLines_PurchaseReceiptLineId");
            b.HasOne("OAS.Domain.Purchasing.Entities.PurchaseInvoiceLine", null).WithMany().HasForeignKey("PurchaseInvoiceLineId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseReturnLines_InvoiceLines_PurchaseInvoiceLineId");
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany().HasForeignKey("ProductVariantId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_PurchaseReturnLines_ProductVariants_ProductVariantId");
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.CommissionRule", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CommissionRules_Employees_EmployeeId");
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.CommissionStatement", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CommissionStatements_Employees_EmployeeId");
            b.HasMany("OAS.Domain.Sales.Entities.CommissionEntry", "Entries").WithOne().HasForeignKey("CommissionStatementId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CommissionEntries_Statements_CommissionStatementId");
            b.Navigation("Entries").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.CommissionEntry", b =>
        {
            b.HasOne("OAS.Domain.Features.Employees.Entities.Employee", null).WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CommissionEntries_Employees_EmployeeId");
            b.HasOne("OAS.Domain.Sales.Entities.CommissionRule", null).WithMany().HasForeignKey("CommissionRuleId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CommissionEntries_CommissionRules_CommissionRuleId");
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoice", null).WithMany().HasForeignKey("SalesInvoiceId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_CommissionEntries_SalesInvoices_SalesInvoiceId");
            b.HasOne("OAS.Domain.Sales.Entities.SalesReturn", null).WithMany().HasForeignKey("SalesReturnId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CommissionEntries_SalesReturns_SalesReturnId");
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.OpticalProductionJob", b =>
        {
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoice", null).WithMany().HasForeignKey("SalesInvoiceId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_OpticalProductionJobs_SalesInvoices_SalesInvoiceId");
            b.HasOne("OAS.Domain.Sales.Entities.SalesInvoiceLine", null).WithMany().HasForeignKey("SalesInvoiceLineId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_OpticalProductionJobs_SalesInvoiceLines_SalesInvoiceLineId");
            b.HasOne("OAS.Domain.Accounting.Entities.Customer", null).WithMany().HasForeignKey("CustomerId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_OpticalProductionJobs_Customers_CustomerId");
            b.HasOne("OAS.Domain.Entities.Inventory.Warehouse", null).WithMany().HasForeignKey("WarehouseId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_OpticalProductionJobs_Warehouses_WarehouseId");
            b.HasOne("OAS.Domain.Entities.Inventory.InventoryTransaction", null).WithMany().HasForeignKey("InventoryTransactionId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalProductionJobs_InventoryTransactions_InventoryTransactionId");
            b.HasOne("OAS.Domain.Sales.Entities.OpticalProductionJob", null).WithMany().HasForeignKey("RemakeOfJobId").OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalProductionJobs_RemakeOfJobId");
            b.HasMany("OAS.Domain.Sales.Entities.OpticalProductionMaterial", "Materials").WithOne().HasForeignKey("OpticalProductionJobId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_OpticalProductionMaterials_Jobs_OpticalProductionJobId");
            b.Navigation("Materials").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        modelBuilder.Entity("OAS.Domain.Sales.Entities.OpticalProductionMaterial", b =>
        {
            b.HasOne("OAS.Domain.Entities.Inventory.ProductVariant", null).WithMany().HasForeignKey("ProductVariantId").OnDelete(DeleteBehavior.Restrict).IsRequired().HasConstraintName("FK_OpticalProductionMaterials_ProductVariants_ProductVariantId");
        });

#pragma warning restore 612, 618
    }
}
