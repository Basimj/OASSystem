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
        migrationBuilder.Sql("""
IF SCHEMA_ID(N'hr') IS NULL
    EXEC(N'CREATE SCHEMA [hr]');

IF OBJECT_ID(N'[hr].[JobTitles]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[JobTitles]
    (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_JobTitles_IsActive] DEFAULT(1),
        [RowVersion] rowversion NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,
        CONSTRAINT [PK_JobTitles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[JobTitles]')
      AND name = N'UX_JobTitles_Name'
)
    CREATE UNIQUE INDEX [UX_JobTitles_Name] ON [hr].[JobTitles]([Name]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[JobTitles]')
      AND name = N'IX_JobTitles_IsActive'
)
    CREATE INDEX [IX_JobTitles_IsActive] ON [hr].[JobTitles]([IsActive]);
""");

        migrationBuilder.Sql("""
DECLARE @Now datetimeoffset = SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM [hr].[JobTitles] WHERE [Name] = N'موظف مبيعات')
    INSERT INTO [hr].[JobTitles] ([Id], [Name], [IsActive], [CreatedAtUtc])
    VALUES (NEWID(), N'موظف مبيعات', 1, @Now);

IF NOT EXISTS (SELECT 1 FROM [hr].[JobTitles] WHERE [Name] = N'فني')
    INSERT INTO [hr].[JobTitles] ([Id], [Name], [IsActive], [CreatedAtUtc])
    VALUES (NEWID(), N'فني', 1, @Now);

IF NOT EXISTS (SELECT 1 FROM [hr].[JobTitles] WHERE [Name] = N'مخازن')
    INSERT INTO [hr].[JobTitles] ([Id], [Name], [IsActive], [CreatedAtUtc])
    VALUES (NEWID(), N'مخازن', 1, @Now);

IF COL_LENGTH(N'[hr].[Employees]', N'JobTitle') IS NOT NULL
BEGIN
    INSERT INTO [hr].[JobTitles] ([Id], [Name], [IsActive], [CreatedAtUtc])
    SELECT NEWID(), src.[Name], 1, @Now
    FROM
    (
        SELECT DISTINCT LTRIM(RTRIM([JobTitle])) AS [Name]
        FROM [hr].[Employees]
        WHERE NULLIF(LTRIM(RTRIM([JobTitle])), N'') IS NOT NULL
    ) src
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [hr].[JobTitles] existing
        WHERE existing.[Name] = src.[Name]
    );
END;

IF NOT EXISTS (SELECT 1 FROM [hr].[JobTitles] WHERE [Name] = N'غير محدد')
    INSERT INTO [hr].[JobTitles] ([Id], [Name], [IsActive], [CreatedAtUtc])
    VALUES (NEWID(), N'غير محدد', 0, @Now);
""");

        // Keep schema-changing statements in their own command. SQL Server compiles a batch
        // before executing it, so a column added and then referenced in the same batch can
        // still produce "Invalid column name" on an existing database.
        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'JobTitleId') IS NULL
    ALTER TABLE [hr].[Employees] ADD [JobTitleId] uniqueidentifier NULL;
""");

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
    (SELECT TOP (1) [Id] FROM [hr].[JobTitles] WHERE [Name] = N'غير محدد');

UPDATE [hr].[Employees]
SET [JobTitleId] = @FallbackJobTitleId
WHERE [JobTitleId] IS NULL;

ALTER TABLE [hr].[Employees] ALTER COLUMN [JobTitleId] uniqueidentifier NOT NULL;
""");

        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'EmployeeNumber') IS NULL
    ALTER TABLE [hr].[Employees] ADD [EmployeeNumber] int NULL;
""");

        migrationBuilder.Sql("""
-- Preserve the numeric part of existing employee codes whenever possible.
-- Any legacy/custom/duplicate code receives a new number after the highest preserved value.
IF COL_LENGTH(N'[hr].[Employees]', N'EmployeeCode') IS NOT NULL
BEGIN
    ;WITH parsed AS
    (
        SELECT
            [Id],
            TRY_CONVERT(int,
                CASE
                    WHEN UPPER(LTRIM(RTRIM([EmployeeCode]))) LIKE N'EMP-%'
                        THEN SUBSTRING(LTRIM(RTRIM([EmployeeCode])), 5, 32)
                    ELSE LTRIM(RTRIM([EmployeeCode]))
                END) AS [ParsedNumber]
        FROM [hr].[Employees]
    ),
    ranked AS
    (
        SELECT
            p.[Id],
            p.[ParsedNumber],
            ROW_NUMBER() OVER
            (
                PARTITION BY p.[ParsedNumber]
                ORDER BY e.[CreatedAtUtc], e.[Id]
            ) AS [DuplicateRank]
        FROM parsed p
        INNER JOIN [hr].[Employees] e ON e.[Id] = p.[Id]
    )
    UPDATE e
       SET [EmployeeNumber] = CASE
            WHEN r.[ParsedNumber] > 0 AND r.[DuplicateRank] = 1 THEN r.[ParsedNumber]
            ELSE NULL
       END
    FROM [hr].[Employees] e
    INNER JOIN ranked r ON r.[Id] = e.[Id]
    WHERE e.[EmployeeNumber] IS NULL;
END;

DECLARE @HighestPreservedNumber bigint =
    ISNULL((SELECT MAX(CONVERT(bigint, [EmployeeNumber])) FROM [hr].[Employees]), 0);

;WITH missing AS
(
    SELECT
        [Id],
        ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc], [Id]) AS [Offset]
    FROM [hr].[Employees]
    WHERE [EmployeeNumber] IS NULL
)
UPDATE e
   SET [EmployeeNumber] = CONVERT(int, @HighestPreservedNumber + m.[Offset])
FROM [hr].[Employees] e
INNER JOIN missing m ON m.[Id] = e.[Id];

ALTER TABLE [hr].[Employees] ALTER COLUMN [EmployeeNumber] int NOT NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[hr].[Employees]')
      AND c.name = N'EmployeeNumber'
)
    ALTER TABLE [hr].[Employees]
        ADD CONSTRAINT [DF_Employees_EmployeeNumber]
        DEFAULT (NEXT VALUE FOR [core].[EmployeeNumberSequence]) FOR [EmployeeNumber];

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'UX_Employees_EmployeeNumber'
)
    CREATE UNIQUE INDEX [UX_Employees_EmployeeNumber]
        ON [hr].[Employees]([EmployeeNumber]);

DECLARE @NextEmployeeNumber bigint =
    ISNULL((SELECT MAX(CONVERT(bigint, [EmployeeNumber])) FROM [hr].[Employees]), 0) + 1;
DECLARE @RestartSql nvarchar(250) =
    N'ALTER SEQUENCE [core].[EmployeeNumberSequence] RESTART WITH ' + CONVERT(nvarchar(32), @NextEmployeeNumber) + N';';
EXEC sp_executesql @RestartSql;
""");

        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'Photo') IS NULL
    ALTER TABLE [hr].[Employees] ADD [Photo] nvarchar(512) NULL;

IF OBJECT_ID(N'[hr].[EmployeeImages]', N'U') IS NOT NULL
    DROP TABLE [hr].[EmployeeImages];
""");

        migrationBuilder.Sql("""
IF EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'UX_Employees_NormalizedEmployeeCode'
)
    DROP INDEX [UX_Employees_NormalizedEmployeeCode] ON [hr].[Employees];

IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]') AND name = N'DF_Employees_IsSalesperson')
        ALTER TABLE [hr].[Employees] DROP CONSTRAINT [DF_Employees_IsSalesperson];
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]') AND name = N'DF_Employees_IsTechnician')
        ALTER TABLE [hr].[Employees] DROP CONSTRAINT [DF_Employees_IsTechnician];
END;

IF COL_LENGTH(N'[hr].[Employees]', N'NormalizedEmployeeCode') IS NOT NULL
    ALTER TABLE [hr].[Employees] DROP COLUMN [NormalizedEmployeeCode];
IF COL_LENGTH(N'[hr].[Employees]', N'EmployeeCode') IS NOT NULL
    ALTER TABLE [hr].[Employees] DROP COLUMN [EmployeeCode];
IF COL_LENGTH(N'[hr].[Employees]', N'JobTitle') IS NOT NULL
    ALTER TABLE [hr].[Employees] DROP COLUMN [JobTitle];
IF COL_LENGTH(N'[hr].[Employees]', N'Notes') IS NOT NULL
    ALTER TABLE [hr].[Employees] DROP COLUMN [Notes];
IF COL_LENGTH(N'[hr].[Employees]', N'IsSalesperson') IS NOT NULL
    ALTER TABLE [hr].[Employees] DROP COLUMN [IsSalesperson];
IF COL_LENGTH(N'[hr].[Employees]', N'IsTechnician') IS NOT NULL
    ALTER TABLE [hr].[Employees] DROP COLUMN [IsTechnician];
""");

        migrationBuilder.Sql("""
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'IX_Employees_JobTitleId'
)
    CREATE INDEX [IX_Employees_JobTitleId]
        ON [hr].[Employees]([JobTitleId]);

IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[hr].[JobTitles]', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1 FROM sys.foreign_keys
       WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]')
         AND name = N'FK_Employees_JobTitles_JobTitleId'
   )
    ALTER TABLE [hr].[Employees] WITH CHECK
        ADD CONSTRAINT [FK_Employees_JobTitles_JobTitleId]
        FOREIGN KEY ([JobTitleId]) REFERENCES [hr].[JobTitles]([Id]);

IF EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'FK_Employees_Users_UserAccountId'
)
    ALTER TABLE [hr].[Employees] DROP CONSTRAINT [FK_Employees_Users_UserAccountId];

ALTER TABLE [hr].[Employees] WITH CHECK
    ADD CONSTRAINT [FK_Employees_Users_UserAccountId]
    FOREIGN KEY ([UserAccountId]) REFERENCES [security].[Users]([Id]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'FK_Employees_JobTitles_JobTitleId'
)
    ALTER TABLE [hr].[Employees] DROP CONSTRAINT [FK_Employees_JobTitles_JobTitleId];

IF EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'FK_Employees_Users_UserAccountId'
)
    ALTER TABLE [hr].[Employees] DROP CONSTRAINT [FK_Employees_Users_UserAccountId];

ALTER TABLE [hr].[Employees] WITH CHECK
    ADD CONSTRAINT [FK_Employees_Users_UserAccountId]
    FOREIGN KEY ([UserAccountId]) REFERENCES [security].[Users]([Id]) ON DELETE SET NULL;
""");

        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'EmployeeCode') IS NULL
    ALTER TABLE [hr].[Employees] ADD [EmployeeCode] nvarchar(32) NULL;
IF COL_LENGTH(N'[hr].[Employees]', N'NormalizedEmployeeCode') IS NULL
    ALTER TABLE [hr].[Employees] ADD [NormalizedEmployeeCode] nvarchar(32) NULL;
IF COL_LENGTH(N'[hr].[Employees]', N'JobTitle') IS NULL
    ALTER TABLE [hr].[Employees] ADD [JobTitle] nvarchar(100) NULL;
IF COL_LENGTH(N'[hr].[Employees]', N'Notes') IS NULL
    ALTER TABLE [hr].[Employees] ADD [Notes] nvarchar(1000) NULL;
IF COL_LENGTH(N'[hr].[Employees]', N'IsSalesperson') IS NULL
    ALTER TABLE [hr].[Employees] ADD [IsSalesperson] bit NOT NULL CONSTRAINT [DF_Employees_IsSalesperson] DEFAULT(0);
IF COL_LENGTH(N'[hr].[Employees]', N'IsTechnician') IS NULL
    ALTER TABLE [hr].[Employees] ADD [IsTechnician] bit NOT NULL CONSTRAINT [DF_Employees_IsTechnician] DEFAULT(0);
""");

        migrationBuilder.Sql("""
UPDATE e
SET [EmployeeCode] = CONCAT(N'EMP-', RIGHT(N'00000' + CONVERT(nvarchar(20), e.[EmployeeNumber]), 5)),
    [NormalizedEmployeeCode] = CONCAT(N'EMP-', RIGHT(N'00000' + CONVERT(nvarchar(20), e.[EmployeeNumber]), 5)),
    [JobTitle] = jt.[Name],
    [IsSalesperson] = CASE WHEN jt.[Name] = N'موظف مبيعات' THEN 1 ELSE 0 END,
    [IsTechnician] = CASE WHEN jt.[Name] = N'فني' THEN 1 ELSE 0 END
FROM [hr].[Employees] e
LEFT JOIN [hr].[JobTitles] jt ON jt.[Id] = e.[JobTitleId];

ALTER TABLE [hr].[Employees] ALTER COLUMN [EmployeeCode] nvarchar(32) NOT NULL;
ALTER TABLE [hr].[Employees] ALTER COLUMN [NormalizedEmployeeCode] nvarchar(32) NOT NULL;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'UX_Employees_NormalizedEmployeeCode'
)
    CREATE UNIQUE INDEX [UX_Employees_NormalizedEmployeeCode]
        ON [hr].[Employees]([NormalizedEmployeeCode]);
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[EmployeeImages]', N'U') IS NULL
BEGIN
    CREATE TABLE [hr].[EmployeeImages]
    (
        [EmployeeId] uniqueidentifier NOT NULL,
        [ContentType] nvarchar(64) NOT NULL,
        [ImageData] varbinary(max) NOT NULL,
        [FileSize] int NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_EmployeeImages] PRIMARY KEY ([EmployeeId]),
        CONSTRAINT [FK_EmployeeImages_Employees_EmployeeId]
            FOREIGN KEY ([EmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_EmployeeImages_FileSize]
            CHECK ([FileSize] > 0 AND [FileSize] <= 2500000)
    );
END;
""");

        migrationBuilder.Sql("""
IF EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'UX_Employees_EmployeeNumber'
)
    DROP INDEX [UX_Employees_EmployeeNumber] ON [hr].[Employees];

IF EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
      AND name = N'IX_Employees_JobTitleId'
)
    DROP INDEX [IX_Employees_JobTitleId] ON [hr].[Employees];

DECLARE @EmployeeNumberDefault sysname =
(
    SELECT dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[hr].[Employees]')
      AND c.name = N'EmployeeNumber'
);
IF @EmployeeNumberDefault IS NOT NULL
    EXEC(N'ALTER TABLE [hr].[Employees] DROP CONSTRAINT [' + @EmployeeNumberDefault + N']');

IF COL_LENGTH(N'[hr].[Employees]', N'Photo') IS NOT NULL
    ALTER TABLE [hr].[Employees] DROP COLUMN [Photo];
IF COL_LENGTH(N'[hr].[Employees]', N'EmployeeNumber') IS NOT NULL
    ALTER TABLE [hr].[Employees] DROP COLUMN [EmployeeNumber];
IF COL_LENGTH(N'[hr].[Employees]', N'JobTitleId') IS NOT NULL
    ALTER TABLE [hr].[Employees] DROP COLUMN [JobTitleId];

IF OBJECT_ID(N'[hr].[JobTitles]', N'U') IS NOT NULL
    DROP TABLE [hr].[JobTitles];
""");
    }
}
