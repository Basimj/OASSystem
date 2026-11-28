using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261128161500_AddCashAccountMovementType")]
public sealed class AddCashAccountMovementType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(name: "MovementType", schema: "dbo", table: "tbl_CashAccounts", type: "tinyint", nullable: false, defaultValue: (byte)3);
        migrationBuilder.AddCheckConstraint(name: "CK_CashAccounts_MovementType", schema: "dbo", table: "tbl_CashAccounts", sql: "[MovementType] IN (1, 2, 3)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_CashAccounts_MovementType", schema: "dbo", table: "tbl_CashAccounts");
        migrationBuilder.DropColumn(name: "MovementType", schema: "dbo", table: "tbl_CashAccounts");
    }
}
