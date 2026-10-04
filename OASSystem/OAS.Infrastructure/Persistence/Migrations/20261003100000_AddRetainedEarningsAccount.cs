using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261003100000_AddRetainedEarningsAccount")]
public sealed class AddRetainedEarningsAccount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // This migration intentionally uses defensive SQL instead of AddColumn/CreateIndex/AddForeignKey.
        // Development databases for OAS have gone through table/schema renames, and some can have the
        // physical change without a matching __EFMigrationsHistory row. Keep the migration repair-safe.
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

-- Repair the two accounting tables if an older database still has them in the accounting schema
-- or with the pre tbl_* name while migration history already advanced.
IF OBJECT_ID(N'[dbo].[tbl_Accounts]', N'U') IS NULL
BEGIN
    IF OBJECT_ID(N'[accounting].[tbl_Accounts]', N'U') IS NOT NULL
        ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_Accounts];
    ELSE IF OBJECT_ID(N'[accounting].[Accounts]', N'U') IS NOT NULL
    BEGIN
        EXEC sys.sp_rename N'[accounting].[Accounts]', N'tbl_Accounts', N'OBJECT';
        ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_Accounts];
    END
    ELSE IF OBJECT_ID(N'[dbo].[Accounts]', N'U') IS NOT NULL
        EXEC sys.sp_rename N'[dbo].[Accounts]', N'tbl_Accounts', N'OBJECT';
END;

IF OBJECT_ID(N'[dbo].[tbl_AccountingSettings]', N'U') IS NULL
BEGIN
    IF OBJECT_ID(N'[accounting].[tbl_AccountingSettings]', N'U') IS NOT NULL
        ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_AccountingSettings];
    ELSE IF OBJECT_ID(N'[accounting].[AccountingSettings]', N'U') IS NOT NULL
    BEGIN
        EXEC sys.sp_rename N'[accounting].[AccountingSettings]', N'tbl_AccountingSettings', N'OBJECT';
        ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_AccountingSettings];
    END
    ELSE IF OBJECT_ID(N'[dbo].[AccountingSettings]', N'U') IS NOT NULL
        EXEC sys.sp_rename N'[dbo].[AccountingSettings]', N'tbl_AccountingSettings', N'OBJECT';
END;

IF OBJECT_ID(N'[dbo].[tbl_AccountingSettings]', N'U') IS NULL
    THROW 51100, 'Required table dbo.tbl_AccountingSettings was not found after schema repair.', 1;

IF OBJECT_ID(N'[dbo].[tbl_Accounts]', N'U') IS NULL
    THROW 51101, 'Required table dbo.tbl_Accounts was not found after schema repair.', 1;

IF COL_LENGTH(N'dbo.tbl_AccountingSettings', N'RetainedEarningsAccountId') IS NULL
    ALTER TABLE [dbo].[tbl_AccountingSettings]
        ADD [RetainedEarningsAccountId] uniqueidentifier NULL;

-- SQL Server binds column names for the whole batch before executing ALTER TABLE.
-- Execute statements that reference the newly-added column dynamically so repair databases
-- where the column is missing can apply this migration in one pass.
EXEC(N'UPDATE s
   SET [RetainedEarningsAccountId] = NULL
FROM [dbo].[tbl_AccountingSettings] s
WHERE s.[RetainedEarningsAccountId] IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM [dbo].[tbl_Accounts] a
      WHERE a.[Id] = s.[RetainedEarningsAccountId]
  );');

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_tbl_AccountingSettings_RetainedEarningsAccountId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_AccountingSettings]')
)
    EXEC(N'CREATE INDEX [IX_tbl_AccountingSettings_RetainedEarningsAccountId]
        ON [dbo].[tbl_AccountingSettings]([RetainedEarningsAccountId]);');

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_AccountingSettings_RetainedEarningsAccount'
      AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_AccountingSettings]')
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[tbl_AccountingSettings] WITH CHECK
        ADD CONSTRAINT [FK_AccountingSettings_RetainedEarningsAccount]
        FOREIGN KEY ([RetainedEarningsAccountId])
        REFERENCES [dbo].[tbl_Accounts]([Id])
        ON DELETE NO ACTION;

    ALTER TABLE [dbo].[tbl_AccountingSettings]
        CHECK CONSTRAINT [FK_AccountingSettings_RetainedEarningsAccount];');
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

IF OBJECT_ID(N'[dbo].[tbl_AccountingSettings]', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE [name] = N'FK_AccountingSettings_RetainedEarningsAccount'
          AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_AccountingSettings]')
    )
        ALTER TABLE [dbo].[tbl_AccountingSettings]
            DROP CONSTRAINT [FK_AccountingSettings_RetainedEarningsAccount];

    IF EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_tbl_AccountingSettings_RetainedEarningsAccountId'
          AND [object_id] = OBJECT_ID(N'[dbo].[tbl_AccountingSettings]')
    )
        DROP INDEX [IX_tbl_AccountingSettings_RetainedEarningsAccountId]
            ON [dbo].[tbl_AccountingSettings];

    IF COL_LENGTH(N'dbo.tbl_AccountingSettings', N'RetainedEarningsAccountId') IS NOT NULL
        ALTER TABLE [dbo].[tbl_AccountingSettings]
            DROP COLUMN [RetainedEarningsAccountId];
END;
""");
    }
}
