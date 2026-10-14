using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261013203000_AccountingCoreHardening")]
public sealed class AccountingCoreHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_JournalEntries]', N'U') IS NOT NULL
BEGIN
    -- Legacy reversal semantics changed the original posted journal to Status = 5 (Reversed).
    -- Core hardening preserves the original as Posted and represents reversal only through
    -- ReversedJournalId + the separate opposite journal, so posted-only reports remain complete.
    UPDATE [dbo].[tbl_JournalEntries]
       SET [Status] = 4
     WHERE [Status] = 5;

    IF EXISTS
    (
        SELECT 1
        FROM [dbo].[tbl_JournalEntries]
        WHERE [SourceModule] IS NOT NULL
          AND [SourceDocumentType] IS NOT NULL
          AND [SourceDocumentId] IS NOT NULL
        GROUP BY [SourceModule], [SourceDocumentType], [SourceDocumentId]
        HAVING COUNT_BIG(*) > 1
    )
    BEGIN
        THROW 51001, 'Accounting Core Hardening cannot create UX_JournalEntries_SourceDocument because duplicate source journals already exist. Resolve the duplicate source postings before applying this migration.', 1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'UX_JournalEntries_SourceDocument'
          AND [object_id] = OBJECT_ID(N'[dbo].[tbl_JournalEntries]')
    )
    BEGIN
        CREATE UNIQUE INDEX [UX_JournalEntries_SourceDocument]
            ON [dbo].[tbl_JournalEntries]([SourceModule], [SourceDocumentType], [SourceDocumentId])
            WHERE [SourceModule] IS NOT NULL
              AND [SourceDocumentType] IS NOT NULL
              AND [SourceDocumentId] IS NOT NULL;
    END;
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_JournalEntries]', N'U') IS NOT NULL
   AND EXISTS
   (
       SELECT 1
       FROM sys.indexes
       WHERE [name] = N'UX_JournalEntries_SourceDocument'
         AND [object_id] = OBJECT_ID(N'[dbo].[tbl_JournalEntries]')
   )
BEGIN
    DROP INDEX [UX_JournalEntries_SourceDocument] ON [dbo].[tbl_JournalEntries];
END;
""");
    }
}
