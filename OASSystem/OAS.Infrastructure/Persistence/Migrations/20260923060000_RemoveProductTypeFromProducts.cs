using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260923060000_RemoveProductTypeFromProducts")]
public sealed class RemoveProductTypeFromProducts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep this migration tolerant of databases whose physical schema does not
        // exactly match migration history. The old ProductType was historically
        // nvarchar(32) and was later aligned to int, so preserve either form.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_ProductTypeMigrationBackup]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[tbl_ProductTypeMigrationBackup]
                (
                    [ProductId] uniqueidentifier NOT NULL,
                    [ProductType] int NOT NULL,
                    CONSTRAINT [PK_ProductTypeMigrationBackup] PRIMARY KEY ([ProductId])
                );
            END;
            """);

        // Dynamic SQL is intentional. Referencing a column that may or may not exist
        // in the same static batch can make SQL Server fail during compilation before
        // the IF condition is evaluated.
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.tbl_Products', N'ProductType') IS NOT NULL
            BEGIN
                EXEC sp_executesql N'
                    MERGE [dbo].[tbl_ProductTypeMigrationBackup] AS target
                    USING
                    (
                        SELECT
                            [Id] AS [ProductId],
                            CASE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(32), [ProductType]))))
                                WHEN N''1'' THEN 1
                                WHEN N''frame'' THEN 1
                                WHEN N''إطار'' THEN 1
                                WHEN N''2'' THEN 2
                                WHEN N''lens'' THEN 2
                                WHEN N''عدسة'' THEN 2
                                WHEN N''3'' THEN 3
                                WHEN N''sunglasses'' THEN 3
                                WHEN N''نظارة شمسية'' THEN 3
                                WHEN N''4'' THEN 4
                                WHEN N''accessory'' THEN 4
                                WHEN N''accessories'' THEN 4
                                WHEN N''إكسسوار'' THEN 4
                                WHEN N''5'' THEN 5
                                WHEN N''other'' THEN 5
                                WHEN N''أخرى'' THEN 5
                                WHEN N''6'' THEN 6
                                WHEN N''service'' THEN 6
                                WHEN N''services'' THEN 6
                                WHEN N''خدمة'' THEN 6
                                ELSE 5
                            END AS [ProductType]
                        FROM [dbo].[tbl_Products]
                    ) AS source
                        ON target.[ProductId] = source.[ProductId]
                    WHEN MATCHED THEN
                        UPDATE SET target.[ProductType] = source.[ProductType]
                    WHEN NOT MATCHED THEN
                        INSERT ([ProductId], [ProductType])
                        VALUES (source.[ProductId], source.[ProductType]);';

                DECLARE @DefaultConstraintName sysname;

                SELECT @DefaultConstraintName = dc.name
                FROM sys.default_constraints AS dc
                INNER JOIN sys.columns AS c
                    ON c.default_object_id = dc.object_id
                WHERE c.object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
                  AND c.name = N'ProductType';

                IF @DefaultConstraintName IS NOT NULL
                BEGIN
                    DECLARE @DropDefaultSql nvarchar(max) =
                        N'ALTER TABLE [dbo].[tbl_Products] DROP CONSTRAINT ' + QUOTENAME(@DefaultConstraintName) + N';';
                    EXEC sys.sp_executesql @DropDefaultSql;
                END;

                -- Drop any non-PK/non-unique-constraint index that contains the old column.
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
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.tbl_Products', N'ProductType') IS NULL
            BEGIN
                ALTER TABLE [dbo].[tbl_Products] ADD [ProductType] int NULL;
            END;
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.tbl_Products', N'ProductType') IS NOT NULL
            BEGIN
                IF OBJECT_ID(N'[dbo].[tbl_ProductTypeMigrationBackup]', N'U') IS NOT NULL
                BEGIN
                    EXEC sp_executesql N'
                        UPDATE p
                        SET [ProductType] = b.[ProductType]
                        FROM [dbo].[tbl_Products] p
                        INNER JOIN [dbo].[tbl_ProductTypeMigrationBackup] b
                            ON b.[ProductId] = p.[Id]
                        WHERE p.[ProductType] IS NULL;';
                END;

                EXEC sp_executesql N'
                    UPDATE [dbo].[tbl_Products]
                    SET [ProductType] = 5
                    WHERE [ProductType] IS NULL;';

                ALTER TABLE [dbo].[tbl_Products] ALTER COLUMN [ProductType] int NOT NULL;
            END;

            IF OBJECT_ID(N'[dbo].[tbl_ProductTypeMigrationBackup]', N'U') IS NOT NULL
                DROP TABLE [dbo].[tbl_ProductTypeMigrationBackup];
            """);
    }
}
