using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260831145400_EmployeesInitial")]
public sealed class EmployeesInitial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF SCHEMA_ID(N'hr') IS NULL
    EXEC(N'CREATE SCHEMA [hr]');
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[Employees]
    (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeCode] nvarchar(32) NOT NULL,
        [NormalizedEmployeeCode] nvarchar(32) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Phone] nvarchar(32) NULL,
        [JobTitle] nvarchar(100) NULL,
        [HireDate] date NULL,
        [Notes] nvarchar(1000) NULL,

        [IsSalesperson] bit NOT NULL
            CONSTRAINT [DF_Employees_IsSalesperson] DEFAULT(0),

        [IsTechnician] bit NOT NULL
            CONSTRAINT [DF_Employees_IsTechnician] DEFAULT(0),

        [IsCommissionEligible] bit NOT NULL
            CONSTRAINT [DF_Employees_IsCommissionEligible] DEFAULT(0),

        [IsActive] bit NOT NULL
            CONSTRAINT [DF_Employees_IsActive] DEFAULT(1),

        [UserAccountId] uniqueidentifier NULL,

        [RowVersion] rowversion NOT NULL,

        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,

        CONSTRAINT [PK_Employees]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_Employees_Users_UserAccountId]
            FOREIGN KEY ([UserAccountId])
            REFERENCES [security].[Users]([Id])
            ON DELETE SET NULL
    );
END;
""");

        migrationBuilder.Sql("""
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'UX_Employees_NormalizedEmployeeCode'
)
    CREATE UNIQUE INDEX [UX_Employees_NormalizedEmployeeCode]
        ON [hr].[Employees]([NormalizedEmployeeCode]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'UX_Employees_UserAccountId'
)
    CREATE UNIQUE INDEX [UX_Employees_UserAccountId]
        ON [hr].[Employees]([UserAccountId])
        WHERE [UserAccountId] IS NOT NULL;
""");

        migrationBuilder.Sql("""
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'IX_Employees_IsActive'
)
    CREATE INDEX [IX_Employees_IsActive]
        ON [hr].[Employees]([IsActive]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'IX_Employees_LastName_FirstName'
)
    CREATE INDEX [IX_Employees_LastName_FirstName]
        ON [hr].[Employees]([LastName], [FirstName]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'IX_Employees_Phone'
)
    CREATE INDEX [IX_Employees_Phone]
        ON [hr].[Employees]([Phone])
        WHERE [Phone] IS NOT NULL;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
    DROP TABLE [hr].[Employees];
""");
    }
}