using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260923070000_AddProductTypesLookup")]
public sealed class AddProductTypesLookup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1) Create/repair the lookup table first. Keep this independent from product
        // schema changes so SQL Server sees the table before later batches reference it.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_productTypes]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[tbl_productTypes]
                (
                    [Id] uniqueidentifier NOT NULL,
                    [Code] nvarchar(32) NOT NULL,
                    [NameAr] nvarchar(100) NOT NULL,
                    [NameEn] nvarchar(100) NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [LastModifiedAtUtc] datetimeoffset NULL,
                    [LastModifiedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_tbl_productTypes] PRIMARY KEY ([Id])
                );
            END;
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'[dbo].[tbl_productTypes]')
                  AND name = N'UX_productTypes_Code')
            BEGIN
                CREATE UNIQUE INDEX [UX_productTypes_Code]
                    ON [dbo].[tbl_productTypes]([Code]);
            END;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'[dbo].[tbl_productTypes]')
                  AND name = N'IX_productTypes_IsActive')
            BEGIN
                CREATE INDEX [IX_productTypes_IsActive]
                    ON [dbo].[tbl_productTypes]([IsActive]);
            END;
            """);

        // 2) Seed stable lookup IDs. UPDATE first makes retries/partially-created tables safe.
        migrationBuilder.Sql("""
            DECLARE @Now datetimeoffset(7) = SYSDATETIMEOFFSET();

            IF EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [Code] = N'FRAME')
                UPDATE [dbo].[tbl_productTypes] SET [NameAr] = N'إطار', [NameEn] = N'Frame', [IsActive] = 1 WHERE [Code] = N'FRAME';
            ELSE
                INSERT [dbo].[tbl_productTypes] ([Id], [Code], [NameAr], [NameEn], [IsActive], [CreatedAtUtc], [CreatedBy])
                VALUES ('71000000-0000-0000-0000-000000000001', N'FRAME', N'إطار', N'Frame', 1, @Now, N'migration');

            IF EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [Code] = N'LENS')
                UPDATE [dbo].[tbl_productTypes] SET [NameAr] = N'عدسة', [NameEn] = N'Lens', [IsActive] = 1 WHERE [Code] = N'LENS';
            ELSE
                INSERT [dbo].[tbl_productTypes] ([Id], [Code], [NameAr], [NameEn], [IsActive], [CreatedAtUtc], [CreatedBy])
                VALUES ('71000000-0000-0000-0000-000000000002', N'LENS', N'عدسة', N'Lens', 1, @Now, N'migration');

            IF EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [Code] = N'SUNGLASSES')
                UPDATE [dbo].[tbl_productTypes] SET [NameAr] = N'نظارة شمسية', [NameEn] = N'Sunglasses', [IsActive] = 1 WHERE [Code] = N'SUNGLASSES';
            ELSE
                INSERT [dbo].[tbl_productTypes] ([Id], [Code], [NameAr], [NameEn], [IsActive], [CreatedAtUtc], [CreatedBy])
                VALUES ('71000000-0000-0000-0000-000000000003', N'SUNGLASSES', N'نظارة شمسية', N'Sunglasses', 1, @Now, N'migration');

            IF EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [Code] = N'ACCESSORY')
                UPDATE [dbo].[tbl_productTypes] SET [NameAr] = N'إكسسوار', [NameEn] = N'Accessory', [IsActive] = 1 WHERE [Code] = N'ACCESSORY';
            ELSE
                INSERT [dbo].[tbl_productTypes] ([Id], [Code], [NameAr], [NameEn], [IsActive], [CreatedAtUtc], [CreatedBy])
                VALUES ('71000000-0000-0000-0000-000000000004', N'ACCESSORY', N'إكسسوار', N'Accessory', 1, @Now, N'migration');

            IF EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [Code] = N'OTHER')
                UPDATE [dbo].[tbl_productTypes] SET [NameAr] = N'أخرى', [NameEn] = N'Other', [IsActive] = 1 WHERE [Code] = N'OTHER';
            ELSE
                INSERT [dbo].[tbl_productTypes] ([Id], [Code], [NameAr], [NameEn], [IsActive], [CreatedAtUtc], [CreatedBy])
                VALUES ('71000000-0000-0000-0000-000000000005', N'OTHER', N'أخرى', N'Other', 1, @Now, N'migration');

            IF EXISTS (SELECT 1 FROM [dbo].[tbl_productTypes] WHERE [Code] = N'SERVICE')
                UPDATE [dbo].[tbl_productTypes] SET [NameAr] = N'خدمة', [NameEn] = N'Service', [IsActive] = 1 WHERE [Code] = N'SERVICE';
            ELSE
                INSERT [dbo].[tbl_productTypes] ([Id], [Code], [NameAr], [NameEn], [IsActive], [CreatedAtUtc], [CreatedBy])
                VALUES ('71000000-0000-0000-0000-000000000006', N'SERVICE', N'خدمة', N'Service', 1, @Now, N'migration');
            """);

        // 3) Add ProductTypeId in its own batch. This avoids SQL Server binding the new
        // column before ALTER TABLE has executed.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_Products', N'ProductTypeId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[tbl_Products]
                    ADD [ProductTypeId] uniqueidentifier NULL;
            END;
            """);

        // 4) Populate the already-existing ProductTypeId column. Prefer the exact backup
        // from the removal migration, then fall back to current product detail/category data.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_Products', N'ProductTypeId') IS NOT NULL
            BEGIN
                DECLARE @FrameTypeId uniqueidentifier = (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] WHERE [Code] = N'FRAME');
                DECLARE @LensTypeId uniqueidentifier = (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] WHERE [Code] = N'LENS');
                DECLARE @SunglassesTypeId uniqueidentifier = (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] WHERE [Code] = N'SUNGLASSES');
                DECLARE @AccessoryTypeId uniqueidentifier = (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] WHERE [Code] = N'ACCESSORY');
                DECLARE @OtherTypeId uniqueidentifier = (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] WHERE [Code] = N'OTHER');
                DECLARE @ServiceTypeId uniqueidentifier = (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] WHERE [Code] = N'SERVICE');

                IF OBJECT_ID(N'[dbo].[tbl_ProductTypeMigrationBackup]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.tbl_ProductTypeMigrationBackup', N'ProductType') IS NOT NULL
                BEGIN
                    DECLARE @BackupMapSql nvarchar(max) = N'
                        UPDATE p
                        SET [ProductTypeId] = CASE TRY_CONVERT(int, b.[ProductType])
                            WHEN 1 THEN @FrameTypeId
                            WHEN 2 THEN @LensTypeId
                            WHEN 3 THEN @SunglassesTypeId
                            WHEN 4 THEN @AccessoryTypeId
                            WHEN 5 THEN @OtherTypeId
                            WHEN 6 THEN @ServiceTypeId
                            ELSE @OtherTypeId
                        END
                        FROM [dbo].[tbl_Products] p
                        INNER JOIN [dbo].[tbl_ProductTypeMigrationBackup] b
                            ON b.[ProductId] = p.[Id]
                        WHERE p.[ProductTypeId] IS NULL;';

                    EXEC sp_executesql @BackupMapSql,
                        N'@FrameTypeId uniqueidentifier, @LensTypeId uniqueidentifier, @SunglassesTypeId uniqueidentifier, @AccessoryTypeId uniqueidentifier, @OtherTypeId uniqueidentifier, @ServiceTypeId uniqueidentifier',
                        @FrameTypeId, @LensTypeId, @SunglassesTypeId, @AccessoryTypeId, @OtherTypeId, @ServiceTypeId;
                END;

                -- If ProductType still exists because an older/partial deployment marked
                -- the removal migration as applied, recover directly from it then remove it.
                IF COL_LENGTH(N'dbo.tbl_Products', N'ProductType') IS NOT NULL
                BEGIN
                    DECLARE @DirectMapSql nvarchar(max) = N'
                        UPDATE p
                        SET [ProductTypeId] = CASE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(32), [ProductType]))))
                            WHEN N''1'' THEN @FrameTypeId
                            WHEN N''frame'' THEN @FrameTypeId
                            WHEN N''إطار'' THEN @FrameTypeId
                            WHEN N''2'' THEN @LensTypeId
                            WHEN N''lens'' THEN @LensTypeId
                            WHEN N''عدسة'' THEN @LensTypeId
                            WHEN N''3'' THEN @SunglassesTypeId
                            WHEN N''sunglasses'' THEN @SunglassesTypeId
                            WHEN N''نظارة شمسية'' THEN @SunglassesTypeId
                            WHEN N''4'' THEN @AccessoryTypeId
                            WHEN N''accessory'' THEN @AccessoryTypeId
                            WHEN N''accessories'' THEN @AccessoryTypeId
                            WHEN N''إكسسوار'' THEN @AccessoryTypeId
                            WHEN N''6'' THEN @ServiceTypeId
                            WHEN N''service'' THEN @ServiceTypeId
                            WHEN N''services'' THEN @ServiceTypeId
                            WHEN N''خدمة'' THEN @ServiceTypeId
                            ELSE @OtherTypeId
                        END
                        FROM [dbo].[tbl_Products] p
                        WHERE p.[ProductTypeId] IS NULL;';

                    EXEC sp_executesql @DirectMapSql,
                        N'@FrameTypeId uniqueidentifier, @LensTypeId uniqueidentifier, @SunglassesTypeId uniqueidentifier, @AccessoryTypeId uniqueidentifier, @OtherTypeId uniqueidentifier, @ServiceTypeId uniqueidentifier',
                        @FrameTypeId, @LensTypeId, @SunglassesTypeId, @AccessoryTypeId, @OtherTypeId, @ServiceTypeId;
                END;

                -- Structural fallback for databases where the old value was already lost.
                UPDATE p
                SET [ProductTypeId] = CASE
                    WHEN OBJECT_ID(N'[dbo].[tbl_LensDetails]', N'U') IS NOT NULL
                         AND EXISTS (SELECT 1 FROM [dbo].[tbl_LensDetails] l WHERE l.[ProductId] = p.[Id]) THEN @LensTypeId
                    WHEN OBJECT_ID(N'[dbo].[tbl_FrameDetails]', N'U') IS NOT NULL
                         AND EXISTS (SELECT 1 FROM [dbo].[tbl_FrameDetails] f WHERE f.[ProductId] = p.[Id])
                         AND EXISTS (
                            SELECT 1 FROM [dbo].[tbl_ProductCategories] c
                            WHERE c.[Id] = p.[CategoryId] AND UPPER(c.[Code]) = N'SUNGLASSES') THEN @SunglassesTypeId
                    WHEN OBJECT_ID(N'[dbo].[tbl_FrameDetails]', N'U') IS NOT NULL
                         AND EXISTS (SELECT 1 FROM [dbo].[tbl_FrameDetails] f WHERE f.[ProductId] = p.[Id]) THEN @FrameTypeId
                    WHEN EXISTS (
                            SELECT 1 FROM [dbo].[tbl_ProductCategories] c
                            WHERE c.[Id] = p.[CategoryId] AND UPPER(c.[Code]) IN (N'ACCESSORY', N'ACCESSORIES')) THEN @AccessoryTypeId
                    WHEN EXISTS (
                            SELECT 1 FROM [dbo].[tbl_ProductCategories] c
                            WHERE c.[Id] = p.[CategoryId] AND UPPER(c.[Code]) IN (N'SERVICE', N'SERVICES')) THEN @ServiceTypeId
                    ELSE @OtherTypeId
                END
                FROM [dbo].[tbl_Products] p
                WHERE p.[ProductTypeId] IS NULL;
            END;
            """);

        // 5) Enforce the final schema only after every row has been populated.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_Products', N'ProductTypeId') IS NOT NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM [dbo].[tbl_Products] WHERE [ProductTypeId] IS NULL)
                BEGIN
                    DECLARE @OtherTypeId uniqueidentifier = (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] WHERE [Code] = N'OTHER');
                    UPDATE [dbo].[tbl_Products]
                    SET [ProductTypeId] = @OtherTypeId
                    WHERE [ProductTypeId] IS NULL;
                END;

                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                      AND name = N'ProductTypeId'
                      AND is_nullable = 1)
                BEGIN
                    ALTER TABLE [dbo].[tbl_Products]
                        ALTER COLUMN [ProductTypeId] uniqueidentifier NOT NULL;
                END;
            END;
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_Products', N'ProductTypeId') IS NOT NULL
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                      AND name = N'IX_Products_ProductTypeId')
                BEGIN
                    CREATE INDEX [IX_Products_ProductTypeId]
                        ON [dbo].[tbl_Products]([ProductTypeId]);
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE parent_object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                      AND name = N'FK_Products_ProductTypes_ProductTypeId')
                BEGIN
                    ALTER TABLE [dbo].[tbl_Products] WITH CHECK
                        ADD CONSTRAINT [FK_Products_ProductTypes_ProductTypeId]
                        FOREIGN KEY ([ProductTypeId])
                        REFERENCES [dbo].[tbl_productTypes]([Id])
                        ON DELETE NO ACTION;

                    ALTER TABLE [dbo].[tbl_Products]
                        CHECK CONSTRAINT [FK_Products_ProductTypes_ProductTypeId];
                END;
            END;
            """);

        // 6) Cleanup temporary/legacy artifacts. Use dynamic SQL for the optional old
        // column so a missing ProductType can never break batch compilation.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_Products', N'ProductType') IS NOT NULL
            BEGIN
                DECLARE @DefaultConstraintName sysname;
                SELECT @DefaultConstraintName = dc.name
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                  AND c.name = N'ProductType';

                IF @DefaultConstraintName IS NOT NULL
                BEGIN
                    DECLARE @DropDefaultSql nvarchar(max) =
                        N'ALTER TABLE [dbo].[tbl_Products] DROP CONSTRAINT ' + QUOTENAME(@DefaultConstraintName) + N';';
                    EXEC sys.sp_executesql @DropDefaultSql;
                END;

                DECLARE @DropIndexes nvarchar(max) = N'';
                SELECT @DropIndexes = @DropIndexes +
                    N'DROP INDEX ' + QUOTENAME(i.name) + N' ON [dbo].[tbl_Products];'
                FROM sys.indexes i
                WHERE i.object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                  AND i.is_primary_key = 0
                  AND i.is_unique_constraint = 0
                  AND EXISTS
                  (
                      SELECT 1
                      FROM sys.index_columns ic
                      INNER JOIN sys.columns c
                          ON c.object_id = ic.object_id
                         AND c.column_id = ic.column_id
                      WHERE ic.object_id = i.object_id
                        AND ic.index_id = i.index_id
                        AND c.name = N'ProductType'
                  );

                IF LEN(@DropIndexes) > 0
                    EXEC sp_executesql @DropIndexes;

                EXEC(N'ALTER TABLE [dbo].[tbl_Products] DROP COLUMN [ProductType]');
            END;

            IF OBJECT_ID(N'[dbo].[tbl_ProductTypeMigrationBackup]', N'U') IS NOT NULL
                DROP TABLE [dbo].[tbl_ProductTypeMigrationBackup];
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE parent_object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                      AND name = N'FK_Products_ProductTypes_ProductTypeId')
                BEGIN
                    ALTER TABLE [dbo].[tbl_Products]
                        DROP CONSTRAINT [FK_Products_ProductTypes_ProductTypeId];
                END;

                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                      AND name = N'IX_Products_ProductTypeId')
                BEGIN
                    DROP INDEX [IX_Products_ProductTypeId] ON [dbo].[tbl_Products];
                END;
            END;
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_Products', N'ProductTypeId') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[tbl_Products] DROP COLUMN [ProductTypeId];
            END;

            IF OBJECT_ID(N'[dbo].[tbl_productTypes]', N'U') IS NOT NULL
                DROP TABLE [dbo].[tbl_productTypes];
            """);
    }
}
