using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260923100000_ProductTypeSystemKey")]
public sealed class ProductTypeSystemKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_productTypes]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_productTypes', N'SystemKey') IS NULL
            BEGIN
                ALTER TABLE [dbo].[tbl_productTypes] ADD [SystemKey] nvarchar(32) NULL;
            END;
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_productTypes]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_productTypes', N'SystemKey') IS NOT NULL
            BEGIN
                -- Prefer the stable IDs created by AddProductTypesLookup.
                UPDATE [dbo].[tbl_productTypes]
                SET [SystemKey] = CASE [Id]
                    WHEN '71000000-0000-0000-0000-000000000001' THEN N'FRAME'
                    WHEN '71000000-0000-0000-0000-000000000002' THEN N'LENS'
                    WHEN '71000000-0000-0000-0000-000000000003' THEN N'SUNGLASSES'
                    WHEN '71000000-0000-0000-0000-000000000004' THEN N'ACCESSORY'
                    WHEN '71000000-0000-0000-0000-000000000005' THEN N'OTHER'
                    WHEN '71000000-0000-0000-0000-000000000006' THEN N'SERVICE'
                    ELSE [SystemKey]
                END
                WHERE [SystemKey] IS NULL
                  AND [Id] IN (
                    '71000000-0000-0000-0000-000000000001',
                    '71000000-0000-0000-0000-000000000002',
                    '71000000-0000-0000-0000-000000000003',
                    '71000000-0000-0000-0000-000000000004',
                    '71000000-0000-0000-0000-000000000005',
                    '71000000-0000-0000-0000-000000000006');

                -- Compatibility with databases where the lookup existed before the stable IDs.
                IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [SystemKey] = N'FRAME')
                    UPDATE TOP (1) [dbo].[tbl_productTypes] SET [SystemKey] = N'FRAME' WHERE [SystemKey] IS NULL AND UPPER(LTRIM(RTRIM([Code]))) = N'FRAME';
                IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [SystemKey] = N'LENS')
                    UPDATE TOP (1) [dbo].[tbl_productTypes] SET [SystemKey] = N'LENS' WHERE [SystemKey] IS NULL AND UPPER(LTRIM(RTRIM([Code]))) = N'LENS';
                IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [SystemKey] = N'SUNGLASSES')
                    UPDATE TOP (1) [dbo].[tbl_productTypes] SET [SystemKey] = N'SUNGLASSES' WHERE [SystemKey] IS NULL AND UPPER(LTRIM(RTRIM([Code]))) = N'SUNGLASSES';
                IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [SystemKey] = N'ACCESSORY')
                    UPDATE TOP (1) [dbo].[tbl_productTypes] SET [SystemKey] = N'ACCESSORY' WHERE [SystemKey] IS NULL AND UPPER(LTRIM(RTRIM([Code]))) = N'ACCESSORY';
                IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [SystemKey] = N'OTHER')
                    UPDATE TOP (1) [dbo].[tbl_productTypes] SET [SystemKey] = N'OTHER' WHERE [SystemKey] IS NULL AND UPPER(LTRIM(RTRIM([Code]))) = N'OTHER';
                IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [SystemKey] = N'SERVICE')
                    UPDATE TOP (1) [dbo].[tbl_productTypes] SET [SystemKey] = N'SERVICE' WHERE [SystemKey] IS NULL AND UPPER(LTRIM(RTRIM([Code]))) = N'SERVICE';
            END;
            """);


        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_Product' AND schema_id = SCHEMA_ID(N'dbo'))
                EXEC(N'CREATE SEQUENCE [dbo].[Seq_InventoryCode_Product] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
            IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_Brand' AND schema_id = SCHEMA_ID(N'dbo'))
                EXEC(N'CREATE SEQUENCE [dbo].[Seq_InventoryCode_Brand] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
            IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_ProductCategory' AND schema_id = SCHEMA_ID(N'dbo'))
                EXEC(N'CREATE SEQUENCE [dbo].[Seq_InventoryCode_ProductCategory] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
            IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_ProductType' AND schema_id = SCHEMA_ID(N'dbo'))
                EXEC(N'CREATE SEQUENCE [dbo].[Seq_InventoryCode_ProductType] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
            IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_Unit' AND schema_id = SCHEMA_ID(N'dbo'))
                EXEC(N'CREATE SEQUENCE [dbo].[Seq_InventoryCode_Unit] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
            IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_Warehouse' AND schema_id = SCHEMA_ID(N'dbo'))
                EXEC(N'CREATE SEQUENCE [dbo].[Seq_InventoryCode_Warehouse] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
            IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_ProductVariant' AND schema_id = SCHEMA_ID(N'dbo'))
                EXEC(N'CREATE SEQUENCE [dbo].[Seq_InventoryCode_ProductVariant] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_productTypes]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_productTypes', N'SystemKey') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_productTypes]')
                      AND name = N'UX_productTypes_SystemKey')
            BEGIN
                CREATE UNIQUE INDEX [UX_productTypes_SystemKey]
                    ON [dbo].[tbl_productTypes]([SystemKey])
                    WHERE [SystemKey] IS NOT NULL;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'[dbo].[tbl_productTypes]')
                  AND name = N'UX_productTypes_SystemKey')
            BEGIN
                DROP INDEX [UX_productTypes_SystemKey] ON [dbo].[tbl_productTypes];
            END;

            IF OBJECT_ID(N'[dbo].[tbl_productTypes]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_productTypes', N'SystemKey') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[tbl_productTypes] DROP COLUMN [SystemKey];
            END;
            """);

        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_Product' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[Seq_InventoryCode_Product];
            IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_Brand' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[Seq_InventoryCode_Brand];
            IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_ProductCategory' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[Seq_InventoryCode_ProductCategory];
            IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_ProductType' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[Seq_InventoryCode_ProductType];
            IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_Unit' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[Seq_InventoryCode_Unit];
            IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_Warehouse' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[Seq_InventoryCode_Warehouse];
            IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_InventoryCode_ProductVariant' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[Seq_InventoryCode_ProductVariant];
            """);
    }
}
