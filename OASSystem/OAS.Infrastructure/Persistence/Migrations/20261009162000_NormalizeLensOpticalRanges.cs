using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261009162000_NormalizeLensOpticalRanges")]
public sealed class NormalizeLensOpticalRanges : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_LensDetails]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[tbl_LensDetails]
    SET
        [SphereMin] = CASE WHEN [SphereMin] IS NOT NULL AND [SphereMax] IS NOT NULL AND [SphereMin] > [SphereMax] THEN [SphereMax] ELSE [SphereMin] END,
        [SphereMax] = CASE WHEN [SphereMin] IS NOT NULL AND [SphereMax] IS NOT NULL AND [SphereMin] > [SphereMax] THEN [SphereMin] ELSE [SphereMax] END,
        [CylinderMin] = CASE WHEN [CylinderMin] IS NOT NULL AND [CylinderMax] IS NOT NULL AND [CylinderMin] > [CylinderMax] THEN [CylinderMax] ELSE [CylinderMin] END,
        [CylinderMax] = CASE WHEN [CylinderMin] IS NOT NULL AND [CylinderMax] IS NOT NULL AND [CylinderMin] > [CylinderMax] THEN [CylinderMin] ELSE [CylinderMax] END,
        [AddMin] = CASE WHEN [AddMin] IS NOT NULL AND [AddMax] IS NOT NULL AND [AddMin] > [AddMax] THEN [AddMax] ELSE [AddMin] END,
        [AddMax] = CASE WHEN [AddMin] IS NOT NULL AND [AddMax] IS NOT NULL AND [AddMin] > [AddMax] THEN [AddMin] ELSE [AddMax] END;
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data normalization is intentionally not reversible because the original ordering was invalid.
    }
}
