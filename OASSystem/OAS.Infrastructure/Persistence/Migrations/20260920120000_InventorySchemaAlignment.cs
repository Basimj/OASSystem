using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260920120000_InventorySchemaAlignment")]
public sealed class InventorySchemaAlignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ProductVariant inherits AuditableEntity<Guid>, but InventoryInitial omitted
        // its audit columns. Add them safely for databases that already applied it.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_ProductVariants]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_ProductVariants', N'CreatedAtUtc') IS NULL
    BEGIN
        ALTER TABLE [dbo].[tbl_ProductVariants]
            ADD [CreatedAtUtc] datetimeoffset NOT NULL
                CONSTRAINT [DF_tbl_ProductVariants_CreatedAtUtc]
                DEFAULT (CONVERT(datetimeoffset, SYSUTCDATETIME()));

        ALTER TABLE [dbo].[tbl_ProductVariants]
            DROP CONSTRAINT [DF_tbl_ProductVariants_CreatedAtUtc];
    END;

    IF COL_LENGTH(N'dbo.tbl_ProductVariants', N'CreatedBy') IS NULL
        ALTER TABLE [dbo].[tbl_ProductVariants]
            ADD [CreatedBy] nvarchar(64) NULL;

    IF COL_LENGTH(N'dbo.tbl_ProductVariants', N'LastModifiedAtUtc') IS NULL
        ALTER TABLE [dbo].[tbl_ProductVariants]
            ADD [LastModifiedAtUtc] datetimeoffset NULL;

    IF COL_LENGTH(N'dbo.tbl_ProductVariants', N'LastModifiedBy') IS NULL
        ALTER TABLE [dbo].[tbl_ProductVariants]
            ADD [LastModifiedBy] nvarchar(64) NULL;
END;
""");

        // Inventory enums are configured with HasConversion<int>().
        // InventoryInitial created these columns as nvarchar, so normalize any
        // legacy named/numeric text values and then align the physical type to int.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
      AND c.name = N'ProductType'
      AND t.name <> N'int'
)
BEGIN
    UPDATE [dbo].[tbl_Products]
    SET [ProductType] = CASE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(32), [ProductType]))))
        WHEN N'1' THEN N'1'
        WHEN N'frame' THEN N'1'
        WHEN N'إطار' THEN N'1'
        WHEN N'2' THEN N'2'
        WHEN N'lens' THEN N'2'
        WHEN N'عدسة' THEN N'2'
        WHEN N'3' THEN N'3'
        WHEN N'sunglasses' THEN N'3'
        WHEN N'نظارة شمسية' THEN N'3'
        WHEN N'4' THEN N'4'
        WHEN N'accessory' THEN N'4'
        WHEN N'accessories' THEN N'4'
        WHEN N'إكسسوار' THEN N'4'
        WHEN N'5' THEN N'5'
        WHEN N'other' THEN N'5'
        WHEN N'أخرى' THEN N'5'
        WHEN N'6' THEN N'6'
        WHEN N'service' THEN N'6'
        WHEN N'services' THEN N'6'
        WHEN N'خدمة' THEN N'6'
        ELSE N'5'
    END;

    ALTER TABLE [dbo].[tbl_Products]
        ALTER COLUMN [ProductType] int NOT NULL;
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]')
          AND c.name = N'TransactionType'
          AND t.name <> N'int'
    )
    BEGIN
        UPDATE [dbo].[tbl_InventoryTransactions]
        SET [TransactionType] = CASE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(32), [TransactionType]))))
            WHEN N'opening' THEN N'1'
            WHEN N'receipt' THEN N'2'
            WHEN N'issue' THEN N'3'
            WHEN N'transfer' THEN N'4'
            WHEN N'adjustmentincrease' THEN N'5'
            WHEN N'adjustmentdecrease' THEN N'6'
            WHEN N'salesreturn' THEN N'7'
            WHEN N'purchasereturn' THEN N'8'
            WHEN N'productionissue' THEN N'9'
            WHEN N'scrap' THEN N'10'
            ELSE LTRIM(RTRIM(CONVERT(nvarchar(32), [TransactionType])))
        END;

        IF EXISTS (
            SELECT 1 FROM [dbo].[tbl_InventoryTransactions]
            WHERE TRY_CONVERT(int, [TransactionType]) IS NULL
               OR TRY_CONVERT(int, [TransactionType]) NOT BETWEEN 1 AND 10
        )
            THROW 51002, 'Cannot convert dbo.tbl_InventoryTransactions.TransactionType to int because unsupported values exist.', 1;

        ALTER TABLE [dbo].[tbl_InventoryTransactions]
            ALTER COLUMN [TransactionType] int NOT NULL;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]')
          AND c.name = N'Status'
          AND t.name <> N'int'
    )
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]') AND name = N'IX_tbl_InventoryTransactions_Status')
            DROP INDEX [IX_tbl_InventoryTransactions_Status] ON [dbo].[tbl_InventoryTransactions];

        IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]') AND name = N'IX_InventoryTransactions_Status')
            DROP INDEX [IX_InventoryTransactions_Status] ON [dbo].[tbl_InventoryTransactions];

        UPDATE [dbo].[tbl_InventoryTransactions]
        SET [Status] = CASE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(32), [Status]))))
            WHEN N'draft' THEN N'1'
            WHEN N'posted' THEN N'2'
            ELSE LTRIM(RTRIM(CONVERT(nvarchar(32), [Status])))
        END;

        IF EXISTS (
            SELECT 1 FROM [dbo].[tbl_InventoryTransactions]
            WHERE TRY_CONVERT(int, [Status]) IS NULL
               OR TRY_CONVERT(int, [Status]) NOT BETWEEN 1 AND 2
        )
            THROW 51003, 'Cannot convert dbo.tbl_InventoryTransactions.Status to int because unsupported values exist.', 1;

        ALTER TABLE [dbo].[tbl_InventoryTransactions]
            ALTER COLUMN [Status] int NOT NULL;

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]') AND name = N'IX_tbl_InventoryTransactions_Status')
            CREATE INDEX [IX_tbl_InventoryTransactions_Status]
                ON [dbo].[tbl_InventoryTransactions]([Status]);
    END;
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_InventoryLedger]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_InventoryLedger]')
      AND c.name = N'MovementType'
      AND t.name <> N'int'
)
BEGIN
    UPDATE [dbo].[tbl_InventoryLedger]
    SET [MovementType] = CASE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(32), [MovementType]))))
        WHEN N'in' THEN N'1'
        WHEN N'out' THEN N'2'
        ELSE LTRIM(RTRIM(CONVERT(nvarchar(32), [MovementType])))
    END;

    IF EXISTS (
        SELECT 1 FROM [dbo].[tbl_InventoryLedger]
        WHERE TRY_CONVERT(int, [MovementType]) IS NULL
           OR TRY_CONVERT(int, [MovementType]) NOT BETWEEN 1 AND 2
    )
        THROW 51004, 'Cannot convert dbo.tbl_InventoryLedger.MovementType to int because unsupported values exist.', 1;

    ALTER TABLE [dbo].[tbl_InventoryLedger]
        ALTER COLUMN [MovementType] int NOT NULL;
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_StockCounts]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]')
      AND c.name = N'Status'
      AND t.name <> N'int'
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]') AND name = N'IX_tbl_StockCounts_Status')
        DROP INDEX [IX_tbl_StockCounts_Status] ON [dbo].[tbl_StockCounts];

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]') AND name = N'IX_StockCounts_Status')
        DROP INDEX [IX_StockCounts_Status] ON [dbo].[tbl_StockCounts];

    UPDATE [dbo].[tbl_StockCounts]
    SET [Status] = CASE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(32), [Status]))))
        WHEN N'draft' THEN N'1'
        WHEN N'counting' THEN N'2'
        WHEN N'review' THEN N'3'
        WHEN N'approved' THEN N'4'
        WHEN N'posted' THEN N'5'
        WHEN N'cancelled' THEN N'6'
        ELSE LTRIM(RTRIM(CONVERT(nvarchar(32), [Status])))
    END;

    IF EXISTS (
        SELECT 1 FROM [dbo].[tbl_StockCounts]
        WHERE TRY_CONVERT(int, [Status]) IS NULL
           OR TRY_CONVERT(int, [Status]) NOT BETWEEN 1 AND 6
    )
        THROW 51005, 'Cannot convert dbo.tbl_StockCounts.Status to int because unsupported values exist.', 1;

    ALTER TABLE [dbo].[tbl_StockCounts]
        ALTER COLUMN [Status] int NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]') AND name = N'IX_tbl_StockCounts_Status')
        CREATE INDEX [IX_tbl_StockCounts_Status]
            ON [dbo].[tbl_StockCounts]([Status]);
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_StockCounts]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]') AND c.name = N'Status' AND t.name = N'int'
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]') AND name = N'IX_tbl_StockCounts_Status')
        DROP INDEX [IX_tbl_StockCounts_Status] ON [dbo].[tbl_StockCounts];

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]') AND name = N'IX_StockCounts_Status')
        DROP INDEX [IX_StockCounts_Status] ON [dbo].[tbl_StockCounts];

    ALTER TABLE [dbo].[tbl_StockCounts] ALTER COLUMN [Status] nvarchar(32) NOT NULL;

    CREATE INDEX [IX_tbl_StockCounts_Status]
        ON [dbo].[tbl_StockCounts]([Status]);
END;

IF OBJECT_ID(N'[dbo].[tbl_InventoryLedger]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_InventoryLedger]') AND c.name = N'MovementType' AND t.name = N'int'
)
    ALTER TABLE [dbo].[tbl_InventoryLedger] ALTER COLUMN [MovementType] nvarchar(32) NOT NULL;

IF OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]') AND c.name = N'Status' AND t.name = N'int'
    )
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]') AND name = N'IX_tbl_InventoryTransactions_Status')
            DROP INDEX [IX_tbl_InventoryTransactions_Status] ON [dbo].[tbl_InventoryTransactions];

        IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]') AND name = N'IX_InventoryTransactions_Status')
            DROP INDEX [IX_InventoryTransactions_Status] ON [dbo].[tbl_InventoryTransactions];

        ALTER TABLE [dbo].[tbl_InventoryTransactions] ALTER COLUMN [Status] nvarchar(32) NOT NULL;

        CREATE INDEX [IX_tbl_InventoryTransactions_Status]
            ON [dbo].[tbl_InventoryTransactions]([Status]);
    END;

    IF EXISTS (
        SELECT 1 FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]') AND c.name = N'TransactionType' AND t.name = N'int'
    )
        ALTER TABLE [dbo].[tbl_InventoryTransactions] ALTER COLUMN [TransactionType] nvarchar(32) NOT NULL;
END;

IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_Products]') AND c.name = N'ProductType' AND t.name = N'int'
)
    ALTER TABLE [dbo].[tbl_Products] ALTER COLUMN [ProductType] nvarchar(32) NOT NULL;

IF OBJECT_ID(N'[dbo].[tbl_ProductVariants]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_ProductVariants', N'LastModifiedBy') IS NOT NULL
        ALTER TABLE [dbo].[tbl_ProductVariants] DROP COLUMN [LastModifiedBy];

    IF COL_LENGTH(N'dbo.tbl_ProductVariants', N'LastModifiedAtUtc') IS NOT NULL
        ALTER TABLE [dbo].[tbl_ProductVariants] DROP COLUMN [LastModifiedAtUtc];

    IF COL_LENGTH(N'dbo.tbl_ProductVariants', N'CreatedBy') IS NOT NULL
        ALTER TABLE [dbo].[tbl_ProductVariants] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'dbo.tbl_ProductVariants', N'CreatedAtUtc') IS NOT NULL
        ALTER TABLE [dbo].[tbl_ProductVariants] DROP COLUMN [CreatedAtUtc];
END;
""");
    }
}
