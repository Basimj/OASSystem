using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260923090000_InventoryProductIntegrationRules")]
public sealed class InventoryProductIntegrationRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Warehouses]', N'U') IS NOT NULL
            BEGIN
                ;WITH ActiveDefaults AS
                (
                    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc], [Id]) AS [rn]
                    FROM [dbo].[tbl_Warehouses]
                    WHERE [IsDefault] = 1 AND [IsActive] = 1
                )
                UPDATE w
                    SET [IsDefault] = 0,
                        [LastModifiedAtUtc] = SYSDATETIMEOFFSET(),
                        [LastModifiedBy] = N'migration'
                FROM [dbo].[tbl_Warehouses] w
                INNER JOIN ActiveDefaults d ON d.[Id] = w.[Id]
                WHERE d.[rn] > 1;

                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Warehouses]')
                      AND name = N'IX_Warehouses_IsDefault')
                BEGIN
                    DROP INDEX [IX_Warehouses_IsDefault] ON [dbo].[tbl_Warehouses];
                END;

                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Warehouses]')
                      AND name = N'IX_tbl_Warehouses_IsDefault')
                BEGIN
                    DROP INDEX [IX_tbl_Warehouses_IsDefault] ON [dbo].[tbl_Warehouses];
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Warehouses]')
                      AND name = N'UX_Warehouses_ActiveDefault')
                BEGIN
                    CREATE UNIQUE INDEX [UX_Warehouses_ActiveDefault]
                        ON [dbo].[tbl_Warehouses]([IsDefault])
                        WHERE [IsDefault] = 1 AND [IsActive] = 1;
                END;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[tbl_Warehouses]', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Warehouses]')
                      AND name = N'UX_Warehouses_ActiveDefault')
                BEGIN
                    DROP INDEX [UX_Warehouses_ActiveDefault] ON [dbo].[tbl_Warehouses];
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Warehouses]')
                      AND name = N'IX_tbl_Warehouses_IsDefault')
                BEGIN
                    CREATE INDEX [IX_tbl_Warehouses_IsDefault]
                        ON [dbo].[tbl_Warehouses]([IsDefault]);
                END;
            END;
            """);
    }
}
