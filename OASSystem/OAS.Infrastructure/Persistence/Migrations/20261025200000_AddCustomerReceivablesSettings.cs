using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable
namespace OAS.Infrastructure.Persistence.Migrations;

// Isolated singleton setting, following the existing WalkInCashSaleSettings migration.
// It does not change the tracked EF model, so no snapshot regeneration is needed.
[DbContext(typeof(OasDbContext))]
[Migration("20261025200000_AddCustomerReceivablesSettings")]
public sealed class AddCustomerReceivablesSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "tbl_CustomerReceivablesSettings",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                ParentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerReceivablesSettings", x => x.Id);
                table.ForeignKey(
                    name: "FK_CustomerReceivablesSettings_ParentAccount",
                    column: x => x.ParentAccountId,
                    principalSchema: "dbo",
                    principalTable: "tbl_Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_CustomerReceivablesSettings_SingleRow", "[Id] = 1");
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "tbl_CustomerReceivablesSettings", schema: "dbo");
    }
}
