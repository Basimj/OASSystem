using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable
namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261024151500_AddWalkInCashSaleSettings")]
public sealed class AddWalkInCashSaleSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Separate single-row settings table is intentionally not an EF entity.
        // This avoids touching the global accounting model or its large snapshot.
        migrationBuilder.CreateTable(
            name: "tbl_WalkInCashSaleSettings",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WalkInCashSaleSettings", x => x.Id);
                table.ForeignKey(
                    name: "FK_WalkInCashSaleSettings_Customer",
                    column: x => x.CustomerId,
                    principalSchema: "dbo",
                    principalTable: "tbl_Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_WalkInCashSaleSettings_SingleRow", "[Id] = 1");
            });
        migrationBuilder.CreateIndex(
            name: "IX_WalkInCashSaleSettings_CustomerId",
            schema: "dbo",
            table: "tbl_WalkInCashSaleSettings",
            column: "CustomerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "tbl_WalkInCashSaleSettings", schema: "dbo");
    }
}
