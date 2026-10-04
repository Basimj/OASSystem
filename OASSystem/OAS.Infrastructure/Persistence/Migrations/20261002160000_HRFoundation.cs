using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261002160000_HRFoundation")]
public sealed class HRFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF SCHEMA_ID(N'hr') IS NULL EXEC(N'CREATE SCHEMA [hr]');

/* Stable HR document sequences. Gaps are intentional and never reused. */
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'DepartmentCodeSequence' AND schema_id=SCHEMA_ID(N'hr'))
    EXEC(N'CREATE SEQUENCE [hr].[DepartmentCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'EmployeeContractCodeSequence' AND schema_id=SCHEMA_ID(N'hr'))
    EXEC(N'CREATE SEQUENCE [hr].[EmployeeContractCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'SalaryComponentCodeSequence' AND schema_id=SCHEMA_ID(N'hr'))
    EXEC(N'CREATE SEQUENCE [hr].[SalaryComponentCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'SalaryStructureCodeSequence' AND schema_id=SCHEMA_ID(N'hr'))
    EXEC(N'CREATE SEQUENCE [hr].[SalaryStructureCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'EmployeeDocumentCodeSequence' AND schema_id=SCHEMA_ID(N'hr'))
    EXEC(N'CREATE SEQUENCE [hr].[EmployeeDocumentCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');

/* Audit device columns follow the interceptor's existing naming convention. */
IF COL_LENGTH(N'hr.Employees', N'CreatedFromDevice') IS NULL ALTER TABLE [hr].[Employees] ADD [CreatedFromDevice] nvarchar(256) NULL;
IF COL_LENGTH(N'hr.Employees', N'UpdatedFromDevice') IS NULL ALTER TABLE [hr].[Employees] ADD [UpdatedFromDevice] nvarchar(256) NULL;
IF COL_LENGTH(N'hr.JobTitles', N'CreatedFromDevice') IS NULL ALTER TABLE [hr].[JobTitles] ADD [CreatedFromDevice] nvarchar(256) NULL;
IF COL_LENGTH(N'hr.JobTitles', N'UpdatedFromDevice') IS NULL ALTER TABLE [hr].[JobTitles] ADD [UpdatedFromDevice] nvarchar(256) NULL;

/* Organization. Departments are created before employee DepartmentId FK. */
IF OBJECT_ID(N'[hr].[Departments]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[Departments](
        [Id] uniqueidentifier NOT NULL,
        [DepartmentCode] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(150) NOT NULL,
        [NameEn] nvarchar(150) NULL,
        [ParentDepartmentId] uniqueidentifier NULL,
        [ManagerEmployeeId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_Departments_IsActive] DEFAULT(1),
        [Notes] nvarchar(500) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Departments_NotSelfParent] CHECK ([ParentDepartmentId] IS NULL OR [ParentDepartmentId] <> [Id]),
        CONSTRAINT [FK_Departments_Departments_ParentDepartmentId] FOREIGN KEY ([ParentDepartmentId]) REFERENCES [hr].[Departments]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Departments_Employees_ManagerEmployeeId] FOREIGN KEY ([ManagerEmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[hr].[Departments]') AND name=N'UX_Departments_DepartmentCode') CREATE UNIQUE INDEX [UX_Departments_DepartmentCode] ON [hr].[Departments]([DepartmentCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[hr].[Departments]') AND name=N'IX_Departments_ParentDepartmentId') CREATE INDEX [IX_Departments_ParentDepartmentId] ON [hr].[Departments]([ParentDepartmentId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[hr].[Departments]') AND name=N'IX_Departments_ManagerEmployeeId') CREATE INDEX [IX_Departments_ManagerEmployeeId] ON [hr].[Departments]([ManagerEmployeeId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[hr].[Departments]') AND name=N'IX_Departments_IsActive') CREATE INDEX [IX_Departments_IsActive] ON [hr].[Departments]([IsActive]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[hr].[Departments]') AND name=N'IX_Departments_NameAr') CREATE INDEX [IX_Departments_NameAr] ON [hr].[Departments]([NameAr]);

/* Extend the existing employee table without data guessing. */
IF COL_LENGTH(N'hr.Employees', N'DepartmentId') IS NULL ALTER TABLE [hr].[Employees] ADD [DepartmentId] uniqueidentifier NULL;
IF COL_LENGTH(N'hr.Employees', N'ManagerEmployeeId') IS NULL ALTER TABLE [hr].[Employees] ADD [ManagerEmployeeId] uniqueidentifier NULL;
IF COL_LENGTH(N'hr.Employees', N'IsSalesperson') IS NULL ALTER TABLE [hr].[Employees] ADD [IsSalesperson] bit NOT NULL CONSTRAINT [DF_Employees_IsSalesperson_HRFoundation] DEFAULT(0);
IF COL_LENGTH(N'hr.Employees', N'IsTechnician') IS NULL ALTER TABLE [hr].[Employees] ADD [IsTechnician] bit NOT NULL CONSTRAINT [DF_Employees_IsTechnician_HRFoundation] DEFAULT(0);
""");

        // SQL Server compiles references to newly added columns before ALTER TABLE ADD has executed
        // when both statements share a single command batch. Start a fresh command before creating
        // indexes, foreign keys, and check constraints that reference the new employee columns.
        migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'IX_Employees_DepartmentId') CREATE INDEX [IX_Employees_DepartmentId] ON [hr].[Employees]([DepartmentId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'IX_Employees_ManagerEmployeeId') CREATE INDEX [IX_Employees_ManagerEmployeeId] ON [hr].[Employees]([ManagerEmployeeId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'FK_Employees_Departments_DepartmentId') ALTER TABLE [hr].[Employees] ADD CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [hr].[Departments]([Id]) ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'FK_Employees_Employees_ManagerEmployeeId') ALTER TABLE [hr].[Employees] ADD CONSTRAINT [FK_Employees_Employees_ManagerEmployeeId] FOREIGN KEY ([ManagerEmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'CK_Employees_NotSelfManager') ALTER TABLE [hr].[Employees] ADD CONSTRAINT [CK_Employees_NotSelfManager] CHECK ([ManagerEmployeeId] IS NULL OR [ManagerEmployeeId] <> [Id]);

/* Employment contracts. */
IF OBJECT_ID(N'[hr].[EmployeeContracts]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[EmployeeContracts](
        [Id] uniqueidentifier NOT NULL,[ContractCode] nvarchar(40) NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[ContractType] tinyint NOT NULL,[Status] tinyint NOT NULL,
        [StartDate] date NOT NULL,[EndDate] date NULL,[ProbationEndDate] date NULL,[WorkingHoursPerDay] decimal(6,2) NULL,[WorkingDaysPerWeek] decimal(4,2) NULL,[CurrencyId] uniqueidentifier NOT NULL,
        [Notes] nvarchar(1000) NULL,[ActivatedBy] nvarchar(64) NULL,[ActivatedAtUtc] datetimeoffset NULL,[TerminatedBy] nvarchar(64) NULL,[TerminatedAtUtc] datetimeoffset NULL,[TerminationReason] nvarchar(500) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeContracts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeContracts_Dates] CHECK ([EndDate] IS NULL OR [EndDate] >= [StartDate]),
        CONSTRAINT [CK_EmployeeContracts_Probation] CHECK ([ProbationEndDate] IS NULL OR ([ProbationEndDate] >= [StartDate] AND ([EndDate] IS NULL OR [ProbationEndDate] <= [EndDate]))),
        CONSTRAINT [CK_EmployeeContracts_WorkingHours] CHECK ([WorkingHoursPerDay] IS NULL OR ([WorkingHoursPerDay] > 0 AND [WorkingHoursPerDay] <= 24)),
        CONSTRAINT [CK_EmployeeContracts_WorkingDays] CHECK ([WorkingDaysPerWeek] IS NULL OR ([WorkingDaysPerWeek] > 0 AND [WorkingDaysPerWeek] <= 7)),
        CONSTRAINT [FK_EmployeeContracts_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeContracts_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_EmployeeContracts_ContractCode] ON [hr].[EmployeeContracts]([ContractCode]);
CREATE INDEX [IX_EmployeeContracts_EmployeeId] ON [hr].[EmployeeContracts]([EmployeeId]);
CREATE INDEX [IX_EmployeeContracts_Status] ON [hr].[EmployeeContracts]([Status]);
CREATE INDEX [IX_EmployeeContracts_StartDate_EndDate] ON [hr].[EmployeeContracts]([StartDate],[EndDate]);
CREATE UNIQUE INDEX [UX_EmployeeContracts_Employee_Active] ON [hr].[EmployeeContracts]([EmployeeId],[Status]) WHERE [Status] = 2;

/* Salary component master. */
IF OBJECT_ID(N'[hr].[SalaryComponents]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[SalaryComponents](
        [Id] uniqueidentifier NOT NULL,[ComponentCode] nvarchar(32) NOT NULL,[NameAr] nvarchar(150) NOT NULL,[NameEn] nvarchar(150) NULL,[ComponentType] tinyint NOT NULL,[CalculationMethod] tinyint NOT NULL,
        [IsBasicSalary] bit NOT NULL,[IsRecurring] bit NOT NULL,[IsTaxable] bit NOT NULL,[IsActive] bit NOT NULL,[DebitPostingRole] nvarchar(50) NULL,[CreditPostingRole] nvarchar(50) NULL,[DisplayOrder] int NOT NULL,[Notes] nvarchar(500) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SalaryComponents] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalaryComponents_DisplayOrder] CHECK ([DisplayOrder] >= 0)
    );
END;
CREATE UNIQUE INDEX [UX_SalaryComponents_ComponentCode] ON [hr].[SalaryComponents]([ComponentCode]);
CREATE INDEX [IX_SalaryComponents_IsActive] ON [hr].[SalaryComponents]([IsActive]);
CREATE UNIQUE INDEX [UX_SalaryComponents_ActiveBasicSalary] ON [hr].[SalaryComponents]([IsBasicSalary]) WHERE [IsBasicSalary] = 1 AND [IsActive] = 1;

/* Versioned employee salary structures and immutable line snapshots. */
IF OBJECT_ID(N'[hr].[EmployeeSalaryStructures]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[EmployeeSalaryStructures](
        [Id] uniqueidentifier NOT NULL,[StructureCode] nvarchar(40) NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[ContractId] uniqueidentifier NULL,[CurrencyId] uniqueidentifier NOT NULL,
        [EffectiveFrom] date NOT NULL,[EffectiveTo] date NULL,[Status] tinyint NOT NULL,[Notes] nvarchar(500) NULL,[ApprovedBy] nvarchar(64) NULL,[ApprovedAtUtc] datetimeoffset NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeSalaryStructures] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeSalaryStructures_Dates] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [FK_EmployeeSalaryStructures_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeSalaryStructures_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [hr].[EmployeeContracts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeSalaryStructures_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_EmployeeSalaryStructures_StructureCode] ON [hr].[EmployeeSalaryStructures]([StructureCode]);
CREATE INDEX [IX_EmployeeSalaryStructures_EmployeeId] ON [hr].[EmployeeSalaryStructures]([EmployeeId]);
CREATE INDEX [IX_EmployeeSalaryStructures_EffectiveDates] ON [hr].[EmployeeSalaryStructures]([EmployeeId],[EffectiveFrom],[EffectiveTo]);
CREATE UNIQUE INDEX [UX_EmployeeSalaryStructures_Employee_Active] ON [hr].[EmployeeSalaryStructures]([EmployeeId],[Status]) WHERE [Status] = 2;

IF OBJECT_ID(N'[hr].[EmployeeSalaryStructureLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[EmployeeSalaryStructureLines](
        [Id] uniqueidentifier NOT NULL,[EmployeeSalaryStructureId] uniqueidentifier NOT NULL,[SalaryComponentId] uniqueidentifier NOT NULL,[Amount] decimal(19,4) NOT NULL,[Percentage] decimal(9,6) NULL,
        [ComponentCodeSnapshot] nvarchar(32) NOT NULL,[ComponentNameSnapshot] nvarchar(150) NOT NULL,[ComponentTypeSnapshot] tinyint NOT NULL,[CalculationMethodSnapshot] tinyint NOT NULL,[IsBasicSalarySnapshot] bit NOT NULL,
        [DebitPostingRoleSnapshot] nvarchar(50) NULL,[CreditPostingRoleSnapshot] nvarchar(50) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeSalaryStructureLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeSalaryStructureLines_Amount] CHECK ([Amount] >= 0),
        CONSTRAINT [CK_EmployeeSalaryStructureLines_Percentage] CHECK ([Percentage] IS NULL OR [Percentage] > 0),
        CONSTRAINT [FK_EmployeeSalaryStructureLines_Structures_StructureId] FOREIGN KEY ([EmployeeSalaryStructureId]) REFERENCES [hr].[EmployeeSalaryStructures]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeSalaryStructureLines_Components_ComponentId] FOREIGN KEY ([SalaryComponentId]) REFERENCES [hr].[SalaryComponents]([Id]) ON DELETE NO ACTION
    );
END;
CREATE INDEX [IX_EmployeeSalaryStructureLines_StructureId] ON [hr].[EmployeeSalaryStructureLines]([EmployeeSalaryStructureId]);
CREATE UNIQUE INDEX [UX_EmployeeSalaryStructureLines_Structure_Component] ON [hr].[EmployeeSalaryStructureLines]([EmployeeSalaryStructureId],[SalaryComponentId]);

/* Secure employee-document metadata. File contents live outside the public web root. */
IF OBJECT_ID(N'[hr].[EmployeeDocuments]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[EmployeeDocuments](
        [Id] uniqueidentifier NOT NULL,[DocumentCode] nvarchar(40) NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[DocumentType] tinyint NOT NULL,[Title] nvarchar(200) NOT NULL,
        [StorageKey] nvarchar(512) NOT NULL,[OriginalFileName] nvarchar(260) NOT NULL,[ContentType] nvarchar(100) NOT NULL,[FileSize] bigint NOT NULL,[Sha256Hash] nvarchar(64) NOT NULL,
        [IssueDate] date NULL,[ExpiryDate] date NULL,[IsActive] bit NOT NULL,[Notes] nvarchar(500) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeDocuments_FileSize] CHECK ([FileSize] > 0),
        CONSTRAINT [FK_EmployeeDocuments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_EmployeeDocuments_DocumentCode] ON [hr].[EmployeeDocuments]([DocumentCode]);
CREATE INDEX [IX_EmployeeDocuments_EmployeeId] ON [hr].[EmployeeDocuments]([EmployeeId]);
CREATE INDEX [IX_EmployeeDocuments_Employee_Status] ON [hr].[EmployeeDocuments]([EmployeeId],[IsActive]);
CREATE INDEX [IX_EmployeeDocuments_ExpiryDate] ON [hr].[EmployeeDocuments]([ExpiryDate]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF OBJECT_ID(N'[hr].[EmployeeDocuments]', N'U') IS NOT NULL DROP TABLE [hr].[EmployeeDocuments];
IF OBJECT_ID(N'[hr].[EmployeeSalaryStructureLines]', N'U') IS NOT NULL DROP TABLE [hr].[EmployeeSalaryStructureLines];
IF OBJECT_ID(N'[hr].[EmployeeSalaryStructures]', N'U') IS NOT NULL DROP TABLE [hr].[EmployeeSalaryStructures];
IF OBJECT_ID(N'[hr].[SalaryComponents]', N'U') IS NOT NULL DROP TABLE [hr].[SalaryComponents];
IF OBJECT_ID(N'[hr].[EmployeeContracts]', N'U') IS NOT NULL DROP TABLE [hr].[EmployeeContracts];

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'FK_Employees_Departments_DepartmentId') ALTER TABLE [hr].[Employees] DROP CONSTRAINT [FK_Employees_Departments_DepartmentId];
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'FK_Employees_Employees_ManagerEmployeeId') ALTER TABLE [hr].[Employees] DROP CONSTRAINT [FK_Employees_Employees_ManagerEmployeeId];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'CK_Employees_NotSelfManager') ALTER TABLE [hr].[Employees] DROP CONSTRAINT [CK_Employees_NotSelfManager];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'IX_Employees_DepartmentId') DROP INDEX [IX_Employees_DepartmentId] ON [hr].[Employees];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'IX_Employees_ManagerEmployeeId') DROP INDEX [IX_Employees_ManagerEmployeeId] ON [hr].[Employees];
IF COL_LENGTH(N'hr.Employees', N'DepartmentId') IS NOT NULL ALTER TABLE [hr].[Employees] DROP COLUMN [DepartmentId];
IF COL_LENGTH(N'hr.Employees', N'ManagerEmployeeId') IS NOT NULL ALTER TABLE [hr].[Employees] DROP COLUMN [ManagerEmployeeId];
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'DF_Employees_IsSalesperson_HRFoundation') ALTER TABLE [hr].[Employees] DROP CONSTRAINT [DF_Employees_IsSalesperson_HRFoundation];
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id=OBJECT_ID(N'[hr].[Employees]') AND name=N'DF_Employees_IsTechnician_HRFoundation') ALTER TABLE [hr].[Employees] DROP CONSTRAINT [DF_Employees_IsTechnician_HRFoundation];
IF COL_LENGTH(N'hr.Employees', N'IsSalesperson') IS NOT NULL ALTER TABLE [hr].[Employees] DROP COLUMN [IsSalesperson];
IF COL_LENGTH(N'hr.Employees', N'IsTechnician') IS NOT NULL ALTER TABLE [hr].[Employees] DROP COLUMN [IsTechnician];
IF OBJECT_ID(N'[hr].[Departments]', N'U') IS NOT NULL DROP TABLE [hr].[Departments];

IF COL_LENGTH(N'hr.Employees', N'UpdatedFromDevice') IS NOT NULL ALTER TABLE [hr].[Employees] DROP COLUMN [UpdatedFromDevice];
IF COL_LENGTH(N'hr.Employees', N'CreatedFromDevice') IS NOT NULL ALTER TABLE [hr].[Employees] DROP COLUMN [CreatedFromDevice];
IF COL_LENGTH(N'hr.JobTitles', N'UpdatedFromDevice') IS NOT NULL ALTER TABLE [hr].[JobTitles] DROP COLUMN [UpdatedFromDevice];
IF COL_LENGTH(N'hr.JobTitles', N'CreatedFromDevice') IS NOT NULL ALTER TABLE [hr].[JobTitles] DROP COLUMN [CreatedFromDevice];

IF EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'EmployeeDocumentCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[EmployeeDocumentCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'SalaryStructureCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[SalaryStructureCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'SalaryComponentCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[SalaryComponentCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'EmployeeContractCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[EmployeeContractCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'DepartmentCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[DepartmentCodeSequence];
""");
    }
}
