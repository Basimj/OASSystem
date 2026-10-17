using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261016080000_SalesReturnAccountingAndQualityHardening")]
public sealed class SalesReturnAccountingAndQualityHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // SQL Server compiles a batch before executing it. Adding a column and then
        // referencing that new column later in the same batch can therefore fail with
        // "Invalid column name". Keep schema changes and dependent data/index work
        // in separate commands so each command is compiled against the updated schema.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'PostingRole') IS NULL
BEGIN
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [PostingRole] nvarchar(40) NULL;
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'PostingRole') IS NOT NULL
BEGIN
    -- Safe historical reconstruction only. Description is deliberately not used.
    UPDATE jel
       SET [PostingRole] = N'SalesCustomerReceivable'
      FROM [dbo].[tbl_JournalEntryLines] jel
      JOIN [dbo].[tbl_JournalEntries] je ON je.[Id] = jel.[JournalEntryId]
     WHERE jel.[PostingRole] IS NULL
       AND je.[SourceModule] = N'Sales'
       AND je.[SourceDocumentType] = N'SalesInvoice'
       AND jel.[SourceDocumentLineId] IS NULL
       AND jel.[CustomerId] IS NOT NULL
       AND jel.[DebitAmount] > 0
       AND jel.[CreditAmount] = 0;

    UPDATE jel
       SET [PostingRole] = N'SalesCOGS'
      FROM [dbo].[tbl_JournalEntryLines] jel
      JOIN [dbo].[tbl_JournalEntries] je ON je.[Id] = jel.[JournalEntryId]
     WHERE jel.[PostingRole] IS NULL
       AND je.[SourceModule] = N'Sales'
       AND je.[SourceDocumentType] = N'SalesInvoice'
       AND jel.[SourceDocumentLineId] IS NOT NULL
       AND jel.[DebitAmount] > 0
       AND jel.[CreditAmount] = 0;

    UPDATE jel
       SET [PostingRole] = N'SalesInventory'
      FROM [dbo].[tbl_JournalEntryLines] jel
      JOIN [dbo].[tbl_JournalEntries] je ON je.[Id] = jel.[JournalEntryId]
     WHERE jel.[PostingRole] IS NULL
       AND je.[SourceModule] = N'Sales'
       AND je.[SourceDocumentType] = N'SalesInvoice'
       AND jel.[SourceDocumentLineId] IS NOT NULL
       AND jel.[CreditAmount] > 0
       AND jel.[DebitAmount] = 0;

    -- A legacy tax-free invoice can safely reconstruct revenue only when exactly one
    -- non-customer header credit exists. Taxed/ambiguous historical rows remain NULL
    -- for explicit accounting review instead of guessing from localized descriptions.
    UPDATE jel
       SET [PostingRole] = N'SalesRevenue'
      FROM [dbo].[tbl_JournalEntryLines] jel
      JOIN [dbo].[tbl_JournalEntries] je ON je.[Id] = jel.[JournalEntryId]
     WHERE jel.[PostingRole] IS NULL
       AND je.[SourceModule] = N'Sales'
       AND je.[SourceDocumentType] = N'SalesInvoice'
       AND jel.[SourceDocumentLineId] IS NULL
       AND jel.[CustomerId] IS NULL
       AND jel.[CreditAmount] > 0
       AND 1 = (
            SELECT COUNT_BIG(*)
              FROM [dbo].[tbl_JournalEntryLines] x
             WHERE x.[JournalEntryId] = jel.[JournalEntryId]
               AND x.[SourceDocumentLineId] IS NULL
               AND x.[CustomerId] IS NULL
               AND x.[CreditAmount] > 0
       );

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_JournalEntryLines_Journal_Role'
          AND [object_id] = OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]')
    )
        CREATE INDEX [IX_JournalEntryLines_Journal_Role]
            ON [dbo].[tbl_JournalEntryLines]([JournalEntryId], [PostingRole]);
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_SalesReturnLines]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_SalesReturnLines', N'ReturnCondition') IS NULL
        ALTER TABLE [dbo].[tbl_SalesReturnLines] ADD [ReturnCondition] tinyint NULL;
    IF COL_LENGTH(N'dbo.tbl_SalesReturnLines', N'StockDisposition') IS NULL
        ALTER TABLE [dbo].[tbl_SalesReturnLines] ADD [StockDisposition] tinyint NULL;
    IF COL_LENGTH(N'dbo.tbl_SalesReturnLines', N'InspectedAtUtc') IS NULL
        ALTER TABLE [dbo].[tbl_SalesReturnLines] ADD [InspectedAtUtc] datetimeoffset(3) NULL;
    IF COL_LENGTH(N'dbo.tbl_SalesReturnLines', N'InspectedBy') IS NULL
        ALTER TABLE [dbo].[tbl_SalesReturnLines] ADD [InspectedBy] nvarchar(64) NULL;
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_SalesReturnLines]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_SalesReturnLines', N'ReturnCondition') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesReturnLines_ReturnCondition')
        ALTER TABLE [dbo].[tbl_SalesReturnLines] ADD CONSTRAINT [CK_SalesReturnLines_ReturnCondition]
            CHECK ([ReturnCondition] IS NULL OR [ReturnCondition] IN (0,1,2,3));

    IF COL_LENGTH(N'dbo.tbl_SalesReturnLines', N'StockDisposition') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesReturnLines_StockDisposition')
        ALTER TABLE [dbo].[tbl_SalesReturnLines] ADD CONSTRAINT [CK_SalesReturnLines_StockDisposition]
            CHECK ([StockDisposition] IS NULL OR [StockDisposition] IN (0,1,2,3,4));
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_SalesReturns]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_SalesReturns', N'ReturnPolicyReferenceDate') IS NULL
        ALTER TABLE [dbo].[tbl_SalesReturns] ADD [ReturnPolicyReferenceDate] date NULL;
    IF COL_LENGTH(N'dbo.tbl_SalesReturns', N'ReturnWindowDaysSnapshot') IS NULL
        ALTER TABLE [dbo].[tbl_SalesReturns] ADD [ReturnWindowDaysSnapshot] int NULL;
    IF COL_LENGTH(N'dbo.tbl_SalesReturns', N'ReturnWindowOverridden') IS NULL
        ALTER TABLE [dbo].[tbl_SalesReturns] ADD [ReturnWindowOverridden] bit NOT NULL CONSTRAINT [DF_SalesReturns_ReturnWindowOverridden] DEFAULT(0);
    IF COL_LENGTH(N'dbo.tbl_SalesReturns', N'ReturnWindowOverrideReason') IS NULL
        ALTER TABLE [dbo].[tbl_SalesReturns] ADD [ReturnWindowOverrideReason] nvarchar(500) NULL;
    IF COL_LENGTH(N'dbo.tbl_SalesReturns', N'ReturnWindowOverriddenBy') IS NULL
        ALTER TABLE [dbo].[tbl_SalesReturns] ADD [ReturnWindowOverriddenBy] nvarchar(64) NULL;
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_Warehouses]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.tbl_Warehouses', N'AllowsSalesAllocation') IS NULL
BEGIN
    ALTER TABLE [dbo].[tbl_Warehouses] ADD [AllowsSalesAllocation] bit NOT NULL
        CONSTRAINT [DF_Warehouses_AllowsSalesAllocation] DEFAULT(1);
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_Warehouses]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.tbl_Warehouses', N'AllowsSalesAllocation') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1 FROM sys.indexes
       WHERE [name] = N'IX_Warehouses_SalesAllocation'
         AND [object_id] = OBJECT_ID(N'[dbo].[tbl_Warehouses]')
   )
BEGIN
    CREATE INDEX [IX_Warehouses_SalesAllocation]
        ON [dbo].[tbl_Warehouses]([IsActive], [AllowsSalesAllocation]);
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_Warehouses]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Warehouses_SalesAllocation' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_Warehouses]'))
        DROP INDEX [IX_Warehouses_SalesAllocation] ON [dbo].[tbl_Warehouses];
    IF COL_LENGTH(N'dbo.tbl_Warehouses', N'AllowsSalesAllocation') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE [name] = N'DF_Warehouses_AllowsSalesAllocation')
            ALTER TABLE [dbo].[tbl_Warehouses] DROP CONSTRAINT [DF_Warehouses_AllowsSalesAllocation];
        ALTER TABLE [dbo].[tbl_Warehouses] DROP COLUMN [AllowsSalesAllocation];
    END;
END;

IF OBJECT_ID(N'[dbo].[tbl_SalesReturns]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_SalesReturns', N'ReturnWindowOverridden') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE [name] = N'DF_SalesReturns_ReturnWindowOverridden')
            ALTER TABLE [dbo].[tbl_SalesReturns] DROP CONSTRAINT [DF_SalesReturns_ReturnWindowOverridden];
        ALTER TABLE [dbo].[tbl_SalesReturns] DROP COLUMN [ReturnWindowOverridden];
    END;
    IF COL_LENGTH(N'dbo.tbl_SalesReturns', N'ReturnWindowOverriddenBy') IS NOT NULL ALTER TABLE [dbo].[tbl_SalesReturns] DROP COLUMN [ReturnWindowOverriddenBy];
    IF COL_LENGTH(N'dbo.tbl_SalesReturns', N'ReturnWindowOverrideReason') IS NOT NULL ALTER TABLE [dbo].[tbl_SalesReturns] DROP COLUMN [ReturnWindowOverrideReason];
    IF COL_LENGTH(N'dbo.tbl_SalesReturns', N'ReturnWindowDaysSnapshot') IS NOT NULL ALTER TABLE [dbo].[tbl_SalesReturns] DROP COLUMN [ReturnWindowDaysSnapshot];
    IF COL_LENGTH(N'dbo.tbl_SalesReturns', N'ReturnPolicyReferenceDate') IS NOT NULL ALTER TABLE [dbo].[tbl_SalesReturns] DROP COLUMN [ReturnPolicyReferenceDate];
END;

IF OBJECT_ID(N'[dbo].[tbl_SalesReturnLines]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesReturnLines_StockDisposition') ALTER TABLE [dbo].[tbl_SalesReturnLines] DROP CONSTRAINT [CK_SalesReturnLines_StockDisposition];
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_SalesReturnLines_ReturnCondition') ALTER TABLE [dbo].[tbl_SalesReturnLines] DROP CONSTRAINT [CK_SalesReturnLines_ReturnCondition];
    IF COL_LENGTH(N'dbo.tbl_SalesReturnLines', N'InspectedBy') IS NOT NULL ALTER TABLE [dbo].[tbl_SalesReturnLines] DROP COLUMN [InspectedBy];
    IF COL_LENGTH(N'dbo.tbl_SalesReturnLines', N'InspectedAtUtc') IS NOT NULL ALTER TABLE [dbo].[tbl_SalesReturnLines] DROP COLUMN [InspectedAtUtc];
    IF COL_LENGTH(N'dbo.tbl_SalesReturnLines', N'StockDisposition') IS NOT NULL ALTER TABLE [dbo].[tbl_SalesReturnLines] DROP COLUMN [StockDisposition];
    IF COL_LENGTH(N'dbo.tbl_SalesReturnLines', N'ReturnCondition') IS NOT NULL ALTER TABLE [dbo].[tbl_SalesReturnLines] DROP COLUMN [ReturnCondition];
END;

IF OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_JournalEntryLines_Journal_Role' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]'))
        DROP INDEX [IX_JournalEntryLines_Journal_Role] ON [dbo].[tbl_JournalEntryLines];
    IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'PostingRole') IS NOT NULL
        ALTER TABLE [dbo].[tbl_JournalEntryLines] DROP COLUMN [PostingRole];
END;
""");
    }
}
