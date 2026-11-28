using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace OAS.Infrastructure.Persistence.Migrations;
[DbContext(typeof(OasDbContext))]
[Migration("20261128200000_AddGroupedCashShifts")]
public sealed class AddGroupedCashShifts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "CashBoxShiftId", schema: "dbo", table: "tbl_CashShifts", type: "uniqueidentifier", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_CashShifts_CashBoxShiftId", schema: "dbo", table: "tbl_CashShifts", column: "CashBoxShiftId");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM dbo.tbl_CashShifts WHERE CashBoxShiftId IS NOT NULL) THROW 51054, 'Grouped cash shifts exist; downgrade would lose history.', 1;");
        migrationBuilder.DropIndex(name: "IX_CashShifts_CashBoxShiftId", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropColumn(name: "CashBoxShiftId", schema: "dbo", table: "tbl_CashShifts");
    }
}
