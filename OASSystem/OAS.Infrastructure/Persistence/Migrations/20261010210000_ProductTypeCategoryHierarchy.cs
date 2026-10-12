using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261010210000_ProductTypeCategoryHierarchy")]
public sealed class ProductTypeCategoryHierarchy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_ProductCategories]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_ProductCategories', N'ProductTypeId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[tbl_ProductCategories]
                    ADD [ProductTypeId] uniqueidentifier NULL;
            END;
            """);

        // Move the existing product -> type relationship up to the category level.
        // If one legacy category was used by products of multiple types, clone that
        // category for the additional type(s) and re-point only those products.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_ProductCategories]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[tbl_productTypes]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_ProductCategories', N'ProductTypeId') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.tbl_Products', N'ProductTypeId') IS NOT NULL
                BEGIN
                    ;WITH PreferredType AS
                    (
                        SELECT
                            p.[CategoryId],
                            p.[ProductTypeId],
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY p.[CategoryId]
                                ORDER BY COUNT_BIG(*) DESC, CONVERT(nvarchar(36), p.[ProductTypeId])
                            ) AS rn
                        FROM [dbo].[tbl_Products] p
                        WHERE p.[ProductTypeId] IS NOT NULL
                        GROUP BY p.[CategoryId], p.[ProductTypeId]
                    )
                    UPDATE c
                       SET c.[ProductTypeId] = pt.[ProductTypeId]
                    FROM [dbo].[tbl_ProductCategories] c
                    INNER JOIN PreferredType pt
                        ON pt.[CategoryId] = c.[Id]
                       AND pt.rn = 1
                    WHERE c.[ProductTypeId] IS NULL;

                    CREATE TABLE #CategoryTypeSplit
                    (
                        [CategoryId] uniqueidentifier NOT NULL,
                        [ProductTypeId] uniqueidentifier NOT NULL,
                        [NewCategoryId] uniqueidentifier NOT NULL,
                        CONSTRAINT [PK_CategoryTypeSplit] PRIMARY KEY ([CategoryId], [ProductTypeId])
                    );

                    -- Build exactly one split row per legacy (CategoryId, ProductTypeId).
                    -- NEWID() must be generated only after de-duplicating the pair; putting
                    -- NEWID() inside SELECT DISTINCT makes every product row distinct and
                    -- violates PK_CategoryTypeSplit when a category/type has many products.
                    INSERT #CategoryTypeSplit ([CategoryId], [ProductTypeId], [NewCategoryId])
                    SELECT d.[CategoryId], d.[ProductTypeId], NEWID()
                    FROM
                    (
                        SELECT p.[CategoryId], p.[ProductTypeId]
                        FROM [dbo].[tbl_Products] p
                        INNER JOIN [dbo].[tbl_ProductCategories] c ON c.[Id] = p.[CategoryId]
                        WHERE p.[ProductTypeId] IS NOT NULL
                          AND c.[ProductTypeId] IS NOT NULL
                          AND p.[ProductTypeId] <> c.[ProductTypeId]
                        GROUP BY p.[CategoryId], p.[ProductTypeId]
                    ) d;

                    INSERT [dbo].[tbl_ProductCategories]
                    (
                        [Id], [Code], [NameAr], [NameEn], [ProductTypeId], [ParentCategoryId],
                        [IsActive], [CreatedAtUtc], [CreatedBy], [LastModifiedAtUtc], [LastModifiedBy]
                    )
                    SELECT
                        m.[NewCategoryId],
                        LEFT(c.[Code], 16) + N'-' + LEFT(pt.[Code], 8) + N'-' + RIGHT(REPLACE(CONVERT(nvarchar(36), m.[NewCategoryId]), N'-', N''), 6),
                        LEFT(c.[NameAr] + N' - ' + pt.[NameAr], 100),
                        CASE
                            WHEN c.[NameEn] IS NULL THEN LEFT(pt.[NameEn], 100)
                            WHEN pt.[NameEn] IS NULL THEN LEFT(c.[NameEn], 100)
                            ELSE LEFT(c.[NameEn] + N' - ' + pt.[NameEn], 100)
                        END,
                        m.[ProductTypeId],
                        NULL,
                        c.[IsActive],
                        c.[CreatedAtUtc],
                        c.[CreatedBy],
                        c.[LastModifiedAtUtc],
                        c.[LastModifiedBy]
                    FROM #CategoryTypeSplit m
                    INNER JOIN [dbo].[tbl_ProductCategories] c ON c.[Id] = m.[CategoryId]
                    INNER JOIN [dbo].[tbl_productTypes] pt ON pt.[Id] = m.[ProductTypeId];

                    UPDATE p
                       SET p.[CategoryId] = m.[NewCategoryId]
                    FROM [dbo].[tbl_Products] p
                    INNER JOIN #CategoryTypeSplit m
                        ON m.[CategoryId] = p.[CategoryId]
                       AND m.[ProductTypeId] = p.[ProductTypeId];

                    DROP TABLE #CategoryTypeSplit;
                END;

                -- Preserve legacy category trees where they can belong to one type.
                -- Propagate a known parent type downward, and a single unambiguous child
                -- type upward. A parent whose children span multiple types is intentionally
                -- left unresolved and will be detached below because one category cannot
                -- belong to multiple product types in the new model.
                DECLARE @Rows int = 1;
                WHILE @Rows > 0
                BEGIN
                    SET @Rows = 0;

                    UPDATE child
                       SET child.[ProductTypeId] = parent.[ProductTypeId]
                    FROM [dbo].[tbl_ProductCategories] child
                    INNER JOIN [dbo].[tbl_ProductCategories] parent ON parent.[Id] = child.[ParentCategoryId]
                    WHERE child.[ProductTypeId] IS NULL
                      AND parent.[ProductTypeId] IS NOT NULL;
                    SET @Rows = @Rows + @@ROWCOUNT;

                    UPDATE parent
                       SET parent.[ProductTypeId] = typedChild.[ProductTypeId]
                    FROM [dbo].[tbl_ProductCategories] parent
                    CROSS APPLY
                    (
                        SELECT TOP (1) child.[ProductTypeId]
                        FROM [dbo].[tbl_ProductCategories] child
                        WHERE child.[ParentCategoryId] = parent.[Id]
                          AND child.[ProductTypeId] IS NOT NULL
                        ORDER BY CONVERT(nvarchar(36), child.[ProductTypeId])
                    ) typedChild
                    WHERE parent.[ProductTypeId] IS NULL
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM [dbo].[tbl_ProductCategories] otherChild
                          WHERE otherChild.[ParentCategoryId] = parent.[Id]
                            AND otherChild.[ProductTypeId] IS NOT NULL
                            AND otherChild.[ProductTypeId] <> typedChild.[ProductTypeId]
                      );
                    SET @Rows = @Rows + @@ROWCOUNT;
                END;

                DECLARE @FallbackTypeId uniqueidentifier =
                    COALESCE(
                        (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] WHERE [SystemKey] = N'OTHER' AND [IsActive] = 1),
                        (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] WHERE [Code] = N'OTHER' AND [IsActive] = 1),
                        (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] WHERE [IsActive] = 1 ORDER BY [Code]),
                        (SELECT TOP (1) [Id] FROM [dbo].[tbl_productTypes] ORDER BY [Code])
                    );

                IF @FallbackTypeId IS NULL
                    THROW 52110, 'Cannot migrate product categories because no product type exists.', 1;

                UPDATE [dbo].[tbl_ProductCategories]
                   SET [ProductTypeId] = @FallbackTypeId
                WHERE [ProductTypeId] IS NULL;

                -- A parent category belongs to exactly one type. Remove legacy cross-type
                -- parent links instead of keeping an invalid hierarchy.
                UPDATE child
                   SET child.[ParentCategoryId] = NULL
                FROM [dbo].[tbl_ProductCategories] child
                INNER JOIN [dbo].[tbl_ProductCategories] parent ON parent.[Id] = child.[ParentCategoryId]
                WHERE child.[ProductTypeId] <> parent.[ProductTypeId];
            END;
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_ProductCategories]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_ProductCategories', N'ProductTypeId') IS NOT NULL
               AND EXISTS
               (
                   SELECT 1
                   FROM sys.columns
                   WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ProductCategories]')
                     AND name = N'ProductTypeId'
                     AND is_nullable = 1
               )
            BEGIN
                ALTER TABLE [dbo].[tbl_ProductCategories]
                    ALTER COLUMN [ProductTypeId] uniqueidentifier NOT NULL;
            END;

            IF OBJECT_ID(N'[dbo].[tbl_ProductCategories]', N'U') IS NOT NULL
               AND NOT EXISTS
               (
                   SELECT 1 FROM sys.indexes
                   WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ProductCategories]')
                     AND name = N'IX_ProductCategories_ProductTypeId'
               )
            BEGIN
                CREATE INDEX [IX_ProductCategories_ProductTypeId]
                    ON [dbo].[tbl_ProductCategories]([ProductTypeId]);
            END;

            IF OBJECT_ID(N'[dbo].[tbl_ProductCategories]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[tbl_productTypes]', N'U') IS NOT NULL
               AND NOT EXISTS
               (
                   SELECT 1 FROM sys.foreign_keys
                   WHERE parent_object_id = OBJECT_ID(N'[dbo].[tbl_ProductCategories]')
                     AND name = N'FK_ProductCategories_ProductTypes_ProductTypeId'
               )
            BEGIN
                ALTER TABLE [dbo].[tbl_ProductCategories] WITH CHECK
                    ADD CONSTRAINT [FK_ProductCategories_ProductTypes_ProductTypeId]
                    FOREIGN KEY ([ProductTypeId]) REFERENCES [dbo].[tbl_productTypes]([Id]);
                ALTER TABLE [dbo].[tbl_ProductCategories]
                    CHECK CONSTRAINT [FK_ProductCategories_ProductTypes_ProductTypeId];
            END;
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_Products', N'ProductTypeId') IS NOT NULL
            BEGIN
                IF EXISTS
                (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE parent_object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                      AND name = N'FK_Products_ProductTypes_ProductTypeId'
                )
                    ALTER TABLE [dbo].[tbl_Products]
                        DROP CONSTRAINT [FK_Products_ProductTypes_ProductTypeId];

                IF EXISTS
                (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                      AND name = N'IX_Products_ProductTypeId'
                )
                    DROP INDEX [IX_Products_ProductTypeId] ON [dbo].[tbl_Products];

                ALTER TABLE [dbo].[tbl_Products]
                    DROP COLUMN [ProductTypeId];
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_Products', N'ProductTypeId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[tbl_Products]
                    ADD [ProductTypeId] uniqueidentifier NULL;
            END;

            IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[tbl_ProductCategories]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_Products', N'ProductTypeId') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_ProductCategories', N'ProductTypeId') IS NOT NULL
            BEGIN
                UPDATE p
                   SET p.[ProductTypeId] = c.[ProductTypeId]
                FROM [dbo].[tbl_Products] p
                INNER JOIN [dbo].[tbl_ProductCategories] c ON c.[Id] = p.[CategoryId];

                ALTER TABLE [dbo].[tbl_Products]
                    ALTER COLUMN [ProductTypeId] uniqueidentifier NOT NULL;

                IF NOT EXISTS
                (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                      AND name = N'IX_Products_ProductTypeId'
                )
                    CREATE INDEX [IX_Products_ProductTypeId]
                        ON [dbo].[tbl_Products]([ProductTypeId]);

                IF NOT EXISTS
                (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE parent_object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                      AND name = N'FK_Products_ProductTypes_ProductTypeId'
                )
                    ALTER TABLE [dbo].[tbl_Products] WITH CHECK
                        ADD CONSTRAINT [FK_Products_ProductTypes_ProductTypeId]
                        FOREIGN KEY ([ProductTypeId]) REFERENCES [dbo].[tbl_productTypes]([Id]);
            END;
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_ProductCategories]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.tbl_ProductCategories', N'ProductTypeId') IS NOT NULL
            BEGIN
                IF EXISTS
                (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE parent_object_id = OBJECT_ID(N'[dbo].[tbl_ProductCategories]')
                      AND name = N'FK_ProductCategories_ProductTypes_ProductTypeId'
                )
                    ALTER TABLE [dbo].[tbl_ProductCategories]
                        DROP CONSTRAINT [FK_ProductCategories_ProductTypes_ProductTypeId];

                IF EXISTS
                (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ProductCategories]')
                      AND name = N'IX_ProductCategories_ProductTypeId'
                )
                    DROP INDEX [IX_ProductCategories_ProductTypeId] ON [dbo].[tbl_ProductCategories];

                ALTER TABLE [dbo].[tbl_ProductCategories]
                    DROP COLUMN [ProductTypeId];
            END;
            """);
    }
}
