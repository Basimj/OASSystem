using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261126120000_LinkCashShiftMovements")]
public sealed class LinkCashShiftMovements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Do not invent historical assignments, and never silently close duplicate active shifts.
        migrationBuilder.Sql("""
            IF EXISTS (SELECT CashAccountId FROM dbo.tbl_CashShifts WHERE Status IN (1,2)
                       GROUP BY CashAccountId HAVING COUNT(*) > 1)
                THROW 51051, 'Multiple active cash shifts exist for a cashbox. Reconcile and close duplicates before migration.', 1;
            """);
        migrationBuilder.AddColumn<bool>(name: "UsesLinkedMovements", schema: "dbo", table: "tbl_CashShifts", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<Guid>(name: "CashShiftId", schema: "dbo", table: "tbl_JournalEntryLines", type: "uniqueidentifier", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_JournalEntryLines_CashShiftId", schema: "dbo", table: "tbl_JournalEntryLines", column: "CashShiftId");
        migrationBuilder.CreateIndex(name: "UX_CashShifts_OneActivePerCashAccount", schema: "dbo", table: "tbl_CashShifts", column: "CashAccountId", unique: true, filter: "[Status] IN (1, 2)");
        migrationBuilder.AddForeignKey(name: "FK_JournalEntryLines_CashShift", schema: "dbo", table: "tbl_JournalEntryLines", column: "CashShiftId", principalSchema: "dbo", principalTable: "tbl_CashShifts", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_JournalEntryLines_CashShift", "tbl_JournalEntryLines", "dbo");
        migrationBuilder.DropIndex("IX_JournalEntryLines_CashShiftId", "tbl_JournalEntryLines", "dbo");
        migrationBuilder.DropIndex("UX_CashShifts_OneActivePerCashAccount", "tbl_CashShifts", "dbo");
        migrationBuilder.DropColumn("CashShiftId", "tbl_JournalEntryLines", "dbo");
        migrationBuilder.DropColumn("UsesLinkedMovements", "tbl_CashShifts", "dbo");
    }
}
