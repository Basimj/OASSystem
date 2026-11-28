using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261128193000_AddOperationalCashBoxes")]
public sealed class AddOperationalCashBoxes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "tbl_CashBoxes", schema: "dbo", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
            Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
        }, constraints: table => table.PrimaryKey("PK_tbl_CashBoxes", x => x.Id));
        migrationBuilder.AddColumn<Guid>(name: "CashBoxId", schema: "dbo", table: "tbl_CashAccounts", type: "uniqueidentifier", nullable: true);
        migrationBuilder.Sql("""
            INSERT INTO dbo.tbl_CashBoxes (Id, Code, Name)
            SELECT Id, Code, Name FROM dbo.tbl_CashAccounts;
            UPDATE dbo.tbl_CashAccounts SET CashBoxId = Id;
            """);
        migrationBuilder.AlterColumn<Guid>(name: "CashBoxId", schema: "dbo", table: "tbl_CashAccounts", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);
        migrationBuilder.CreateIndex(name: "UX_CashBoxes_Code", schema: "dbo", table: "tbl_CashBoxes", column: "Code", unique: true);
        migrationBuilder.CreateIndex(name: "UX_CashAccounts_BoxCurrency", schema: "dbo", table: "tbl_CashAccounts", columns: new[] { "CashBoxId", "CurrencyId" }, unique: true, filter: "[CurrencyId] IS NOT NULL");
        migrationBuilder.AddForeignKey(name: "FK_CashAccounts_CashBox", schema: "dbo", table: "tbl_CashAccounts", column: "CashBoxId", principalSchema: "dbo", principalTable: "tbl_CashBoxes", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Downgrade would otherwise silently discard operational grouping.
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM dbo.tbl_CashAccounts GROUP BY CashBoxId HAVING COUNT(*) > 1) THROW 51053, 'Multi-currency cash boxes exist. Do not downgrade until grouping is resolved.', 1;");
        migrationBuilder.DropForeignKey(name: "FK_CashAccounts_CashBox", schema: "dbo", table: "tbl_CashAccounts");
        migrationBuilder.DropIndex(name: "UX_CashAccounts_BoxCurrency", schema: "dbo", table: "tbl_CashAccounts");
        migrationBuilder.DropColumn(name: "CashBoxId", schema: "dbo", table: "tbl_CashAccounts");
        migrationBuilder.DropTable(name: "tbl_CashBoxes", schema: "dbo");
    }
}
