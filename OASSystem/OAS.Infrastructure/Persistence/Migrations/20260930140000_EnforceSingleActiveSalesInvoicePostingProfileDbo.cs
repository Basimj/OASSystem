using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260930140000_EnforceSingleActiveSalesInvoicePostingProfileDbo")]
public sealed class EnforceSingleActiveSalesInvoicePostingProfileDbo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

-- Repair known historical locations/names before enforcing the filtered index.
-- Some development databases have migration history from the rename/move phase while
-- the physical table still exists under the old schema/name.
IF OBJECT_ID(N'[dbo].[tbl_PostingProfiles]', N'U') IS NULL
BEGIN
    IF OBJECT_ID(N'[accounting].[tbl_PostingProfiles]', N'U') IS NOT NULL
        ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_PostingProfiles];
    ELSE IF OBJECT_ID(N'[accounting].[PostingProfiles]', N'U') IS NOT NULL
    BEGIN
        EXEC sys.sp_rename N'[accounting].[PostingProfiles]', N'tbl_PostingProfiles', N'OBJECT';
        ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_PostingProfiles];
    END
    ELSE IF OBJECT_ID(N'[dbo].[PostingProfiles]', N'U') IS NOT NULL
        EXEC sys.sp_rename N'[dbo].[PostingProfiles]', N'tbl_PostingProfiles', N'OBJECT';
END;

IF OBJECT_ID(N'[dbo].[tbl_PostingProfileLines]', N'U') IS NULL
BEGIN
    IF OBJECT_ID(N'[accounting].[tbl_PostingProfileLines]', N'U') IS NOT NULL
        ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_PostingProfileLines];
    ELSE IF OBJECT_ID(N'[accounting].[PostingProfileLines]', N'U') IS NOT NULL
    BEGIN
        EXEC sys.sp_rename N'[accounting].[PostingProfileLines]', N'tbl_PostingProfileLines', N'OBJECT';
        ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_PostingProfileLines];
    END
    ELSE IF OBJECT_ID(N'[dbo].[PostingProfileLines]', N'U') IS NOT NULL
        EXEC sys.sp_rename N'[dbo].[PostingProfileLines]', N'tbl_PostingProfileLines', N'OBJECT';
END;

IF OBJECT_ID(N'[dbo].[tbl_PostingProfiles]', N'U') IS NULL
    THROW 51110, 'Required table dbo.tbl_PostingProfiles was not found after schema repair.', 1;

-- Repair only Sales / SalesInvoice legacy duplicates.
;WITH RankedSalesInvoiceProfiles AS
(
    SELECT
        [Id],
        ROW_NUMBER() OVER
        (
            ORDER BY CONVERT(bigint, [RowVersion]) DESC, [Id] DESC
        ) AS [rn]
    FROM [dbo].[tbl_PostingProfiles]
    WHERE [IsActive] = 1
      AND [Module] = N'Sales'
      AND [DocumentType] = N'SalesInvoice'
)
UPDATE p
   SET [IsActive] = 0
FROM [dbo].[tbl_PostingProfiles] p
INNER JOIN RankedSalesInvoiceProfiles r ON r.[Id] = p.[Id]
WHERE r.[rn] > 1;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UX_PostingProfiles_Active_SalesInvoice'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PostingProfiles]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_PostingProfiles_Active_SalesInvoice]
        ON [dbo].[tbl_PostingProfiles]([Module], [DocumentType])
        WHERE [IsActive] = 1
          AND [Module] = N'Sales'
          AND [DocumentType] = N'SalesInvoice';
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_PostingProfiles]', N'U') IS NOT NULL
   AND EXISTS
   (
       SELECT 1
       FROM sys.indexes
       WHERE [name] = N'UX_PostingProfiles_Active_SalesInvoice'
         AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PostingProfiles]')
   )
BEGIN
    DROP INDEX [UX_PostingProfiles_Active_SalesInvoice]
        ON [dbo].[tbl_PostingProfiles];
END;
""");
    }
}
