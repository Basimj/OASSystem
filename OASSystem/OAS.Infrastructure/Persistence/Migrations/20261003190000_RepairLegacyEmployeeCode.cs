using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs legacy employee tables that were created before EmployeeCode became
/// the canonical employee identifier, while preserving any recoverable legacy
/// values and keeping the employee number sequence strictly forward-only.
///
/// This migration intentionally does not change the EF model. It repairs schema
/// drift in databases whose migration history is newer than their physical
/// hr.Employees shape.
/// </summary>
[DbContext(typeof(OasDbContext))]
[Migration("20261003190000_RepairLegacyEmployeeCode1")]
public sealed class RepairLegacyEmployeeCode1 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NULL
    THROW 51000, 'Cannot repair employee codes because hr.Employees does not exist.', 1;
""");

        // Add the canonical column first in its own command. Keeping it nullable
        // during backfill allows existing rows to be repaired safely.
        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'EmployeeCode') IS NULL
BEGIN
    ALTER TABLE [hr].[Employees]
        ADD [EmployeeCode] nvarchar(32) NULL;
END;
""");

        // Normalize empty values before attempting to recover/generate codes.
        migrationBuilder.Sql("""
UPDATE [hr].[Employees]
SET [EmployeeCode] = NULL
WHERE NULLIF(LTRIM(RTRIM([EmployeeCode])), N'') IS NULL;
""");

        // Recover a legacy EmployeeNumber when the old column still exists.
        // Dynamic SQL is required because SQL Server otherwise binds the optional
        // legacy column even when the COL_LENGTH branch is false.
        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'EmployeeNumber') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        UPDATE e
        SET [EmployeeCode] =
            CASE
                WHEN NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(64), e.[EmployeeNumber]))), N'''') IS NULL
                    THEN NULL
                WHEN UPPER(LTRIM(RTRIM(CONVERT(nvarchar(64), e.[EmployeeNumber])))) LIKE N''EMP-%''
                    THEN LEFT(LTRIM(RTRIM(CONVERT(nvarchar(64), e.[EmployeeNumber]))), 32)
                WHEN TRY_CONVERT(bigint, e.[EmployeeNumber]) > 0
                    THEN N''EMP-'' +
                        CASE
                            WHEN LEN(CONVERT(nvarchar(20), TRY_CONVERT(bigint, e.[EmployeeNumber]))) < 5
                                THEN RIGHT(N''00000'' + CONVERT(nvarchar(20), TRY_CONVERT(bigint, e.[EmployeeNumber])), 5)
                            ELSE CONVERT(nvarchar(20), TRY_CONVERT(bigint, e.[EmployeeNumber]))
                        END
                ELSE NULL
            END
        FROM [hr].[Employees] e
        WHERE NULLIF(LTRIM(RTRIM(e.[EmployeeCode])), N'''') IS NULL;';
END;
""");

        // Some legacy builds used NormalizedEmployeeCode. Preserve it when it is
        // still available and no EmployeeNumber value could be recovered.
        migrationBuilder.Sql("""
IF COL_LENGTH(N'[hr].[Employees]', N'NormalizedEmployeeCode') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        UPDATE e
        SET [EmployeeCode] = LEFT(LTRIM(RTRIM(CONVERT(nvarchar(64), e.[NormalizedEmployeeCode]))), 32)
        FROM [hr].[Employees] e
        WHERE NULLIF(LTRIM(RTRIM(e.[EmployeeCode])), N'''') IS NULL
          AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(64), e.[NormalizedEmployeeCode]))), N'''') IS NOT NULL;';
END;
""");

        // If a legacy source contained duplicate codes, retain one deterministic
        // value and regenerate only the duplicate rows. This protects existing
        // references/printed identifiers whenever possible while still allowing
        // the canonical unique index to be restored.
        migrationBuilder.Sql("""
;WITH DuplicateCodes AS
(
    SELECT
        [Id],
        ROW_NUMBER() OVER
        (
            PARTITION BY UPPER(LTRIM(RTRIM([EmployeeCode])))
            ORDER BY [Id]
        ) AS [DuplicateSequence]
    FROM [hr].[Employees]
    WHERE NULLIF(LTRIM(RTRIM([EmployeeCode])), N'') IS NOT NULL
)
UPDATE e
SET [EmployeeCode] = NULL
FROM [hr].[Employees] e
INNER JOIN DuplicateCodes d ON d.[Id] = e.[Id]
WHERE d.[DuplicateSequence] > 1;
""");

        // Generate canonical codes for rows that still have no recoverable code.
        // The starting point is never lower than either the highest existing EMP-
        // code or the current sequence value, so previously reserved sequence
        // numbers are not reused.
        migrationBuilder.Sql("""
DECLARE @MaxExistingCode bigint = ISNULL
(
    (
        SELECT MAX(TRY_CONVERT(bigint, SUBSTRING(LTRIM(RTRIM([EmployeeCode])), 5, 28)))
        FROM [hr].[Employees]
        WHERE UPPER(LTRIM(RTRIM([EmployeeCode]))) LIKE N'EMP-%'
          AND TRY_CONVERT(bigint, SUBSTRING(LTRIM(RTRIM([EmployeeCode])), 5, 28)) IS NOT NULL
    ),
    0
);

DECLARE @SequenceCurrent bigint = 0;

IF EXISTS
(
    SELECT 1
    FROM sys.sequences
    WHERE [name] = N'EmployeeNumberSequence'
      AND [schema_id] = SCHEMA_ID(N'core')
)
BEGIN
    SELECT @SequenceCurrent = ISNULL
    (
        TRY_CONVERT(bigint, [current_value]),
        ISNULL(TRY_CONVERT(bigint, [start_value]), 1) - 1
    )
    FROM sys.sequences
    WHERE [name] = N'EmployeeNumberSequence'
      AND [schema_id] = SCHEMA_ID(N'core');
END;

DECLARE @GenerationBase bigint =
    CASE
        WHEN @SequenceCurrent > @MaxExistingCode THEN @SequenceCurrent
        ELSE @MaxExistingCode
    END;

;WITH MissingCodes AS
(
    SELECT
        [Id],
        ROW_NUMBER() OVER (ORDER BY [Id]) AS [GeneratedSequence]
    FROM [hr].[Employees]
    WHERE NULLIF(LTRIM(RTRIM([EmployeeCode])), N'') IS NULL
)
UPDATE e
SET [EmployeeCode] =
    N'EMP-' +
    CASE
        WHEN LEN(CONVERT(nvarchar(20), @GenerationBase + m.[GeneratedSequence])) < 5
            THEN RIGHT(N'00000' + CONVERT(nvarchar(20), @GenerationBase + m.[GeneratedSequence]), 5)
        ELSE CONVERT(nvarchar(20), @GenerationBase + m.[GeneratedSequence])
    END
FROM [hr].[Employees] e
INNER JOIN MissingCodes m ON m.[Id] = e.[Id];
""");

        // Fail safely instead of silently truncating a legacy code that cannot fit
        // the canonical model.
        migrationBuilder.Sql("""
IF EXISTS
(
    SELECT 1
    FROM [hr].[Employees]
    WHERE [EmployeeCode] IS NULL
       OR NULLIF(LTRIM(RTRIM([EmployeeCode])), N'') IS NULL
)
    THROW 51001, 'EmployeeCode repair left one or more employees without a code.', 1;

IF EXISTS
(
    SELECT 1
    FROM [hr].[Employees]
    WHERE LEN([EmployeeCode]) > 32
)
    THROW 51002, 'A legacy employee code exceeds the supported length of 32 characters.', 1;

IF EXISTS
(
    SELECT UPPER(LTRIM(RTRIM([EmployeeCode])))
    FROM [hr].[Employees]
    GROUP BY UPPER(LTRIM(RTRIM([EmployeeCode])))
    HAVING COUNT(*) > 1
)
    THROW 51003, 'EmployeeCode repair found duplicate employee codes.', 1;
""");

        migrationBuilder.Sql("""
ALTER TABLE [hr].[Employees]
    ALTER COLUMN [EmployeeCode] nvarchar(32) NOT NULL;
""");

        migrationBuilder.Sql("""
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[hr].[Employees]')
      AND [name] = N'UX_Employees_EmployeeCode'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Employees_EmployeeCode]
        ON [hr].[Employees]([EmployeeCode]);
END;
""");

        // Keep future generated codes ahead of all repaired EMP-* codes. The
        // sequence is never restarted backwards.
        migrationBuilder.Sql("""
IF SCHEMA_ID(N'core') IS NULL
    EXEC(N'CREATE SCHEMA [core]');

DECLARE @MaxEmployeeNumber bigint = ISNULL
(
    (
        SELECT MAX(TRY_CONVERT(bigint, SUBSTRING(LTRIM(RTRIM([EmployeeCode])), 5, 28)))
        FROM [hr].[Employees]
        WHERE UPPER(LTRIM(RTRIM([EmployeeCode]))) LIKE N'EMP-%'
          AND TRY_CONVERT(bigint, SUBSTRING(LTRIM(RTRIM([EmployeeCode])), 5, 28)) IS NOT NULL
    ),
    0
);

DECLARE @SequenceCurrent bigint = NULL;

IF EXISTS
(
    SELECT 1
    FROM sys.sequences
    WHERE [name] = N'EmployeeNumberSequence'
      AND [schema_id] = SCHEMA_ID(N'core')
)
BEGIN
    SELECT @SequenceCurrent = ISNULL
    (
        TRY_CONVERT(bigint, [current_value]),
        ISNULL(TRY_CONVERT(bigint, [start_value]), 1) - 1
    )
    FROM sys.sequences
    WHERE [name] = N'EmployeeNumberSequence'
      AND [schema_id] = SCHEMA_ID(N'core');
END;

DECLARE @NextEmployeeNumber bigint = @MaxEmployeeNumber + 1;
DECLARE @sql NVARCHAR(MAX);

IF @SequenceCurrent IS NULL
BEGIN
    SET @sql = N'CREATE SEQUENCE [core].[EmployeeNumberSequence] AS int START WITH '
        + CONVERT(nvarchar(20), CASE WHEN @NextEmployeeNumber < 1 THEN 1 ELSE @NextEmployeeNumber END)
        + N' INCREMENT BY 1 MINVALUE 1 NO CYCLE;';
    EXEC(@sql);
END
ELSE IF @SequenceCurrent < @MaxEmployeeNumber
BEGIN
    SET @sql = N'ALTER SEQUENCE [core].[EmployeeNumberSequence] RESTART WITH '
        + CONVERT(nvarchar(20), @NextEmployeeNumber)
        + N';';
    EXEC(@sql);
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally no-op. This migration repairs physical schema drift to
        // match both the preceding and current EF model. Dropping EmployeeCode on
        // rollback would re-introduce the broken legacy schema and lose repaired
        // identifiers.
    }
}
