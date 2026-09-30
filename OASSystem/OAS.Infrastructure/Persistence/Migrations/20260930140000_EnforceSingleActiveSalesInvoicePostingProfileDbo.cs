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
IF OBJECT_ID(N'[dbo].[tbl_PostingProfiles]', N'U') IS NULL
BEGIN
    THROW 50001, 'Table [dbo].[tbl_PostingProfiles] was not found.', 1;
END;

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
