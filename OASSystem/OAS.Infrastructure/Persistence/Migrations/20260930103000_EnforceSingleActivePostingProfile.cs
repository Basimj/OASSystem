using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260930103000_EnforceSingleActivePostingProfile")]
public sealed class EnforceSingleActivePostingProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Repair legacy data first. If more than one profile is active for the same
        // Module + DocumentType, keep the most recently changed row (highest rowversion)
        // active and preserve the older profiles as inactive history.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[accounting].[PostingProfiles]', N'U') IS NOT NULL
BEGIN
    ;WITH RankedActiveProfiles AS
    (
        SELECT
            [Id],
            ROW_NUMBER() OVER
            (
                PARTITION BY [Module], [DocumentType]
                ORDER BY CONVERT(bigint, [RowVersion]) DESC, [Id] DESC
            ) AS [rn]
        FROM [accounting].[PostingProfiles]
        WHERE [IsActive] = 1
    )
    UPDATE p
       SET [IsActive] = 0
    FROM [accounting].[PostingProfiles] p
    INNER JOIN RankedActiveProfiles r ON r.[Id] = p.[Id]
    WHERE r.[rn] > 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'UX_PostingProfiles_Active_Module_DocumentType'
          AND [object_id] = OBJECT_ID(N'[accounting].[PostingProfiles]')
    )
    BEGIN
        CREATE UNIQUE INDEX [UX_PostingProfiles_Active_Module_DocumentType]
            ON [accounting].[PostingProfiles]([Module], [DocumentType])
            WHERE [IsActive] = 1;
    END
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[accounting].[PostingProfiles]', N'U') IS NOT NULL
   AND EXISTS
   (
       SELECT 1
       FROM sys.indexes
       WHERE [name] = N'UX_PostingProfiles_Active_Module_DocumentType'
         AND [object_id] = OBJECT_ID(N'[accounting].[PostingProfiles]')
   )
BEGIN
    DROP INDEX [UX_PostingProfiles_Active_Module_DocumentType]
        ON [accounting].[PostingProfiles];
END;
""");
    }
}
