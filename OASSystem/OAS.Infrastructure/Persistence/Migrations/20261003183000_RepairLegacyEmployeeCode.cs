using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261003183000_RepairLegacyEmployeeCode")]
public sealed class RepairLegacyEmployeeCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Some pre-migration/legacy databases already contain hr.Employees but do not
        // contain EmployeeCode. EmployeesInitial intentionally did not recreate an
        // existing table, so those databases can have migration history that is ahead
        // of their physical schema. Repair the drift without changing the current model.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[hr].[Employees]', N'EmployeeCode') IS NULL
BEGIN
    ALTER TABLE [hr].[Employees]
        ADD [EmployeeCode] nvarchar(32) NULL;
END;
""");

        // Preserve the legacy employee number when possible. Dynamic SQL is deliberate:
        // EmployeeNumber is not part of the current EF model and may not exist at all.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[hr].[Employees]', N'EmployeeNumber') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        UPDATE e
        SET [EmployeeCode] =
            CASE
                WHEN TRY_CONVERT(bigint, e.[EmployeeNumber]) > 0 THEN
                    N''EMP-'' +
                    CASE
                        WHEN LEN(CONVERT(varchar(32), TRY_CONVERT(bigint, e.[EmployeeNumber]))) < 5
                            THEN REPLICATE(N''0'', 5 - LEN(CONVERT(varchar(32), TRY_CONVERT(bigint, e.[EmployeeNumber]))))
                                 + CONVERT(nvarchar(32), TRY_CONVERT(bigint, e.[EmployeeNumber]))
                        ELSE CONVERT(nvarchar(32), TRY_CONVERT(bigint, e.[EmployeeNumber]))
                    END
                ELSE LEFT(NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(128), e.[EmployeeNumber]))), N''''), 32)
            END
        FROM [hr].[Employees] e
        WHERE NULLIF(LTRIM(RTRIM(e.[EmployeeCode])), N'''') IS NULL;';
END;
""");

        // A few older installations stored the normalized code separately. Use it only
        // when EmployeeCode is still empty after the EmployeeNumber migration above.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[hr].[Employees]', N'NormalizedEmployeeCode') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        UPDATE e
        SET [EmployeeCode] = LEFT(NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(128), e.[NormalizedEmployeeCode]))), N''''), 32)
        FROM [hr].[Employees] e
        WHERE NULLIF(LTRIM(RTRIM(e.[EmployeeCode])), N'''') IS NULL;';
END;
""");

        // Drop the canonical index before normalizing values. Trimming legacy values can
        // otherwise create temporary duplicates while a unique index is still active.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[hr].[Employees]', N'EmployeeCode') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.key_constraints
        WHERE [parent_object_id] = OBJECT_ID(N'[hr].[Employees]')
          AND [name] = N'UX_Employees_EmployeeCode'
          AND [type] = N'UQ'
    )
    BEGIN
        ALTER TABLE [hr].[Employees]
            DROP CONSTRAINT [UX_Employees_EmployeeCode];
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'[hr].[Employees]')
          AND [name] = N'UX_Employees_EmployeeCode'
          AND [is_primary_key] = 0
          AND [is_unique_constraint] = 0
    )
    BEGIN
        DROP INDEX [UX_Employees_EmployeeCode] ON [hr].[Employees];
    END;
END;
""");


        // Normalize existing values, then repair null/empty/duplicate codes. One copy of
        // an existing duplicate is preserved; only the additional rows are renumbered.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[hr].[Employees]', N'EmployeeCode') IS NOT NULL
BEGIN
    UPDATE [hr].[Employees]
    SET [EmployeeCode] = LEFT(LTRIM(RTRIM([EmployeeCode])), 32)
    WHERE NULLIF(LTRIM(RTRIM([EmployeeCode])), N'') IS NOT NULL;

    DECLARE @baseNumber bigint = ISNULL
    (
        (
            SELECT MAX(TRY_CONVERT(bigint, SUBSTRING([EmployeeCode], 5, 28)))
            FROM [hr].[Employees]
            WHERE [EmployeeCode] LIKE N'EMP-%'
              AND TRY_CONVERT(bigint, SUBSTRING([EmployeeCode], 5, 28)) IS NOT NULL
        ),
        0
    );

    ;WITH Ranked AS
    (
        SELECT
            [Id],
            [EmployeeCode],
            ROW_NUMBER() OVER
            (
                PARTITION BY [EmployeeCode]
                ORDER BY [Id]
            ) AS [DuplicateRank]
        FROM [hr].[Employees]
    ),
    NeedsRepair AS
    (
        SELECT
            [Id],
            ROW_NUMBER() OVER (ORDER BY [Id]) AS [RepairSequence]
        FROM Ranked
        WHERE NULLIF(LTRIM(RTRIM([EmployeeCode])), N'') IS NULL
           OR [DuplicateRank] > 1
    )
    UPDATE e
    SET [EmployeeCode] =
        N'EMP-' +
        CASE
            WHEN LEN(CONVERT(varchar(32), @baseNumber + r.[RepairSequence])) < 5
                THEN REPLICATE(N'0', 5 - LEN(CONVERT(varchar(32), @baseNumber + r.[RepairSequence])))
                     + CONVERT(nvarchar(32), @baseNumber + r.[RepairSequence])
            ELSE CONVERT(nvarchar(32), @baseNumber + r.[RepairSequence])
        END
    FROM [hr].[Employees] e
    INNER JOIN NeedsRepair r ON r.[Id] = e.[Id];
END;
""");


        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[hr].[Employees]', N'EmployeeCode') IS NOT NULL
BEGIN
    ALTER TABLE [hr].[Employees]
        ALTER COLUMN [EmployeeCode] nvarchar(32) NOT NULL;
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[hr].[Employees]', N'EmployeeCode') IS NOT NULL
   AND NOT EXISTS
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

        // Keep the sequence ahead of every repaired/generated EMP-n code. This prevents
        // the next normal employee creation from generating a code that already exists.
        migrationBuilder.Sql("""
IF SCHEMA_ID(N'core') IS NULL
    EXEC(N'CREATE SCHEMA [core]');

DECLARE @maxEmployeeNumber bigint = 0;

IF OBJECT_ID(N'[hr].[Employees]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[hr].[Employees]', N'EmployeeCode') IS NOT NULL
BEGIN
    SELECT @maxEmployeeNumber = ISNULL
    (
        MAX(TRY_CONVERT(bigint, SUBSTRING([EmployeeCode], 5, 28))),
        0
    )
    FROM [hr].[Employees]
    WHERE [EmployeeCode] LIKE N'EMP-%'
      AND TRY_CONVERT(bigint, SUBSTRING([EmployeeCode], 5, 28)) IS NOT NULL;
END;

DECLARE @currentSequenceValue bigint = 0;

IF EXISTS
(
    SELECT 1
    FROM sys.sequences
    WHERE [name] = N'EmployeeNumberSequence'
      AND [schema_id] = SCHEMA_ID(N'core')
)
BEGIN
    SELECT @currentSequenceValue = ISNULL(TRY_CONVERT(bigint, [current_value]), 0)
    FROM sys.sequences
    WHERE [name] = N'EmployeeNumberSequence'
      AND [schema_id] = SCHEMA_ID(N'core');
END;

DECLARE @restartWith bigint =
    CASE
        WHEN @currentSequenceValue + 1 > @maxEmployeeNumber + 1
            THEN @currentSequenceValue + 1
        ELSE @maxEmployeeNumber + 1
    END;

IF @restartWith < 1 SET @restartWith = 1;

DECLARE @sql NVARCHAR(MAX);

IF NOT EXISTS
(
    SELECT 1
    FROM sys.sequences
    WHERE [name] = N'EmployeeNumberSequence'
      AND [schema_id] = SCHEMA_ID(N'core')
)
BEGIN
    SET @sql = N'CREATE SEQUENCE [core].[EmployeeNumberSequence] AS bigint START WITH '
        + CONVERT(nvarchar(32), @restartWith)
        + N' INCREMENT BY 1 MINVALUE 1 NO CYCLE;';
    EXEC(@sql);
END
ELSE
BEGIN
    SET @sql = N'ALTER SEQUENCE [core].[EmployeeNumberSequence] RESTART WITH '
        + CONVERT(nvarchar(32), @restartWith)
        + N';';
    EXEC(@sql);
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration repairs physical schema drift so that it matches the model that
        // already existed before this migration. Reverting it would intentionally recreate
        // the broken schema, so Down is a no-op.
    }
}
