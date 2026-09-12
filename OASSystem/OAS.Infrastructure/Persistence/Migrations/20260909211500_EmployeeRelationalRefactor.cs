
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260909211500_EmployeeRelationalRefactor")]
public sealed class EmployeeRelationalRefactor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // =========================================================
        // HR Schema + JobTitles
        // =========================================================

        migrationBuilder.Sql("""
IF SCHEMA_ID(N'hr') IS NULL
    EXEC(N'CREATE SCHEMA [hr]');

IF OBJECT_ID(N'[hr].[JobTitles]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[JobTitles]
    (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL
            CONSTRAINT [DF_JobTitles_IsActive] DEFAULT(1),
        [RowVersion] rowversion NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,

        CONSTRAINT [PK_JobTitles]
            PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[JobTitles]')
      AND name = N'UX_JobTitles_Name'
)
    CREATE UNIQUE INDEX [UX_JobTitles_Name]
        ON [hr].[JobTitles]([Name]);

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[JobTitles]')
      AND name = N'IX_JobTitles_IsActive'
)
    CREATE INDEX [IX_JobTitles_IsActive]
        ON [hr].[JobTitles]([IsActive]);
""");

        // =========================================================
        // Seed Job Titles
        // =========================================================

        migrationBuilder.Sql("""
DECLARE @Now datetimeoffset = SYSUTCDATETIME();

IF NOT EXISTS
(
    SELECT 1
    FROM [hr].[JobTitles]
    WHERE [Name] = N'موظف مبيعات'
)
    INSERT INTO [hr].[JobTitles]
    (
        [Id],
        [Name],
        [IsActive],
        [CreatedAtUtc]
    )
    VALUES
    (
        NEWID(),
        N'موظف مبيعات',
        1,
        @Now
    );

IF NOT EXISTS
(
    SELECT 1
    FROM [hr].[JobTitles]
    WHERE [Name] = N'فني'
)
    INSERT INTO [hr].[JobTitles]
    (
        [Id],
        [Name],
        [IsActive],
        [CreatedAtUtc]
    )
    VALUES
    (
        NEWID(),
        N'فني',
        1,
        @Now
    );

IF NOT EXISTS
(
    SELECT 1
    FROM [hr].[JobTitles]
    WHERE [Name] = N'مخازن'
)
    INSERT INTO [hr].[JobTitles]
    (
        [Id],
        [Name],
        [IsActive],
        [CreatedAtUtc]
    )
    VALUES
    (
        NEWID(),
        N'مخازن',
        1,
        @Now
    );

IF COL_LENGTH(N'[hr].[Employees]', N'JobTitle') IS NOT NULL
BEGIN
    INSERT INTO [hr].[JobTitles]
    (
        [Id],
        [Name],
        [IsActive],
        [CreatedAtUtc]
    )
    SELECT
        NEWID(),
        src.[Name],
        1,
        @Now
    FROM
    (
        SELECT DISTINCT
            LTRIM(RTRIM([JobTitle])) AS [Name]
        FROM [hr].[Employees]
        WHERE NULLIF(LTRIM(RTRIM([JobTitle])), N'') IS NOT NULL
    ) src
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [hr].[JobTitles] existing
        WHERE existing.[Name] = src.[Name]
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM [hr].[JobTitles]
    WHERE [Name] = N'غير محدد'
)
    INSERT INTO [hr].[JobTitles]
    (
        [Id],
        [Name],
        [IsActive],
        [CreatedAtUtc]
    )
    VALUES
    (
        NEWID(),
        N'غير محدد',
        0,
        @Now
    );
""");

        // =========================================================
        // Add JobTitleId
        // =========================================================

        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'JobTitleId') IS NULL
    ALTER TABLE [hr].[Employees]
        ADD [JobTitleId] uniqueidentifier NULL;
""");

        // =========================================================
        // Migrate JobTitle -> JobTitleId
        // =========================================================

        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'JobTitle') IS NOT NULL
BEGIN
    UPDATE e
       SET [JobTitleId] = jt.[Id]
    FROM [hr].[Employees] e
    INNER JOIN [hr].[JobTitles] jt
        ON jt.[Name] = NULLIF(LTRIM(RTRIM(e.[JobTitle])), N'')
    WHERE e.[JobTitleId] IS NULL;
END;

DECLARE @FallbackJobTitleId uniqueidentifier =
(
    SELECT TOP (1) [Id]
    FROM [hr].[JobTitles]
    WHERE [Name] = N'غير محدد'
);

UPDATE [hr].[Employees]
SET [JobTitleId] = @FallbackJobTitleId
WHERE [JobTitleId] IS NULL;

ALTER TABLE [hr].[Employees]
    ALTER COLUMN [JobTitleId] uniqueidentifier NOT NULL;
""");

        // =========================================================
        // Photo
        // =========================================================

        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'Photo') IS NULL
    ALTER TABLE [hr].[Employees]
        ADD [Photo] nvarchar(512) NULL;

IF OBJECT_ID(N'[hr].[EmployeeImages]', N'U') IS NOT NULL
    DROP TABLE [hr].[EmployeeImages];
""");

        // =========================================================
        // Remove old employee columns
        //
        // EmployeeCode is intentionally preserved.
        // NormalizedEmployeeCode is no longer part of the schema.
        // EmployeeNumber is no longer part of the schema.
        // =========================================================

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]')
          AND name = N'DF_Employees_IsSalesperson'
    )
        ALTER TABLE [hr].[Employees]
            DROP CONSTRAINT [DF_Employees_IsSalesperson];

    IF EXISTS
    (
        SELECT 1
        FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]')
          AND name = N'DF_Employees_IsTechnician'
    )
        ALTER TABLE [hr].[Employees]
            DROP CONSTRAINT [DF_Employees_IsTechnician];
END;

IF COL_LENGTH(N'[hr].[Employees]', N'JobTitle') IS NOT NULL
    ALTER TABLE [hr].[Employees]
        DROP COLUMN [JobTitle];

IF COL_LENGTH(N'[hr].[Employees]', N'Notes') IS NOT NULL
    ALTER TABLE [hr].[Employees]
        DROP COLUMN [Notes];

IF COL_LENGTH(N'[hr].[Employees]', N'IsSalesperson') IS NOT NULL
    ALTER TABLE [hr].[Employees]
        DROP COLUMN [IsSalesperson];

IF COL_LENGTH(N'[hr].[Employees]', N'IsTechnician') IS NOT NULL
    ALTER TABLE [hr].[Employees]
        DROP COLUMN [IsTechnician];
""");

        // =========================================================
        // JobTitleId Index
        // =========================================================

        migrationBuilder.Sql("""
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'IX_Employees_JobTitleId'
)
    CREATE INDEX [IX_Employees_JobTitleId]
        ON [hr].[Employees]([JobTitleId]);
""");

        // =========================================================
        // JobTitle Foreign Key
        // =========================================================

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[hr].[JobTitles]', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]')
         AND name = N'FK_Employees_JobTitles_JobTitleId'
   )
BEGIN
    ALTER TABLE [hr].[Employees] WITH CHECK
        ADD CONSTRAINT [FK_Employees_JobTitles_JobTitleId]
        FOREIGN KEY ([JobTitleId])
        REFERENCES [hr].[JobTitles]([Id]);
END;
""");

        // =========================================================
        // UserAccount Foreign Key
        // =========================================================

        migrationBuilder.Sql("""
IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'FK_Employees_Users_UserAccountId'
)
    ALTER TABLE [hr].[Employees]
        DROP CONSTRAINT [FK_Employees_Users_UserAccountId];

ALTER TABLE [hr].[Employees] WITH CHECK
    ADD CONSTRAINT [FK_Employees_Users_UserAccountId]
    FOREIGN KEY ([UserAccountId])
    REFERENCES [security].[Users]([Id]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // =========================================================
        // Remove JobTitle Foreign Key
        // =========================================================

        migrationBuilder.Sql("""
IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'FK_Employees_JobTitles_JobTitleId'
)
    ALTER TABLE [hr].[Employees]
        DROP CONSTRAINT [FK_Employees_JobTitles_JobTitleId];

IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'FK_Employees_Users_UserAccountId'
)
    ALTER TABLE [hr].[Employees]
        DROP CONSTRAINT [FK_Employees_Users_UserAccountId];

ALTER TABLE [hr].[Employees] WITH CHECK
    ADD CONSTRAINT [FK_Employees_Users_UserAccountId]
    FOREIGN KEY ([UserAccountId])
    REFERENCES [security].[Users]([Id])
    ON DELETE SET NULL;
""");

        // =========================================================
        // Restore old columns
        //
        // EmployeeCode remains the single employee-code column.
        // We do NOT restore NormalizedEmployeeCode.
        // We do NOT restore EmployeeNumber.
        // =========================================================

        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'JobTitle') IS NULL
    ALTER TABLE [hr].[Employees]
        ADD [JobTitle] nvarchar(100) NULL;

IF COL_LENGTH(N'[hr].[Employees]', N'Notes') IS NULL
    ALTER TABLE [hr].[Employees]
        ADD [Notes] nvarchar(1000) NULL;

IF COL_LENGTH(N'[hr].[Employees]', N'IsSalesperson') IS NULL
    ALTER TABLE [hr].[Employees]
        ADD [IsSalesperson] bit NOT NULL
            CONSTRAINT [DF_Employees_IsSalesperson]
            DEFAULT(0);

IF COL_LENGTH(N'[hr].[Employees]', N'IsTechnician') IS NULL
    ALTER TABLE [hr].[Employees]
        ADD [IsTechnician] bit NOT NULL
            CONSTRAINT [DF_Employees_IsTechnician]
            DEFAULT(0);
""");

        // =========================================================
        // Restore data from JobTitleId
        // =========================================================

        migrationBuilder.Sql("""
UPDATE e
SET
    [JobTitle] = jt.[Name],
    [IsSalesperson] =
        CASE
            WHEN jt.[Name] = N'موظف مبيعات'
                THEN 1
            ELSE 0
        END,
    [IsTechnician] =
        CASE
            WHEN jt.[Name] = N'فني'
                THEN 1
            ELSE 0
        END
FROM [hr].[Employees] e
LEFT JOIN [hr].[JobTitles] jt
    ON jt.[Id] = e.[JobTitleId];
""");

        // =========================================================
        // Remove JobTitleId index
        // =========================================================

        migrationBuilder.Sql("""
IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'IX_Employees_JobTitleId'
)
    DROP INDEX [IX_Employees_JobTitleId]
        ON [hr].[Employees];
""");

        // =========================================================
        // Remove Photo
        // =========================================================

        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'Photo') IS NOT NULL
    ALTER TABLE [hr].[Employees]
        DROP COLUMN [Photo];

IF OBJECT_ID(N'[hr].[EmployeeImages]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[EmployeeImages]
    (
        [EmployeeId] uniqueidentifier NOT NULL,
        [ContentType] nvarchar(64) NOT NULL,
        [ImageData] varbinary(max) NOT NULL,
        [FileSize] int NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,

        CONSTRAINT [PK_EmployeeImages]
            PRIMARY KEY ([EmployeeId]),

        CONSTRAINT [FK_EmployeeImages_Employees_EmployeeId]
            FOREIGN KEY ([EmployeeId])
            REFERENCES [hr].[Employees]([Id])
            ON DELETE CASCADE,

        CONSTRAINT [CK_EmployeeImages_FileSize]
            CHECK ([FileSize] > 0 AND [FileSize] <= 2500000)
    );
END;
""");

        // =========================================================
        // Remove JobTitleId
        // =========================================================

        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'JobTitleId') IS NOT NULL
    ALTER TABLE [hr].[Employees]
        DROP COLUMN [JobTitleId];

IF OBJECT_ID(N'[hr].[JobTitles]', N'U') IS NOT NULL
    DROP TABLE [hr].[JobTitles];
""");
    }
}
