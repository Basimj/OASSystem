using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261006021500_PersistInventoryPostingAccountInAccountingSettings")]
public sealed class PersistInventoryPostingAccountInAccountingSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "InventoryAccountId",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_tbl_AccountingSettings_InventoryAccountId",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            column: "InventoryAccountId");

        migrationBuilder.AddForeignKey(
            name: "FK_AccountingSettings_InventoryAccount",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            column: "InventoryAccountId",
            principalSchema: "dbo",
            principalTable: "tbl_Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        // Backfill from an already configured Purchasing receipt profile first,
        // then from the legacy SalesInvoice Inventory role. This keeps existing
        // databases working without asking the administrator to reselect the account.
        migrationBuilder.Sql(@"
UPDATE s
SET InventoryAccountId = COALESCE(
    (SELECT TOP (1) l.AccountId
     FROM dbo.tbl_PostingProfiles p
     INNER JOIN dbo.tbl_PostingProfileLines l ON l.PostingProfileId = p.Id
     WHERE p.Module = N'Purchasing'
       AND p.DocumentType = N'PurchaseReceipt'
       AND p.IsActive = 1
       AND l.AccountRole = N'Inventory'),
    (SELECT TOP (1) l.AccountId
     FROM dbo.tbl_PostingProfiles p
     INNER JOIN dbo.tbl_PostingProfileLines l ON l.PostingProfileId = p.Id
     WHERE p.Module = N'Sales'
       AND p.DocumentType = N'SalesInvoice'
       AND p.IsActive = 1
       AND l.AccountRole = N'Inventory'))
FROM dbo.tbl_AccountingSettings s
WHERE s.InventoryAccountId IS NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AccountingSettings_InventoryAccount",
            schema: "dbo",
            table: "tbl_AccountingSettings");

        migrationBuilder.DropIndex(
            name: "IX_tbl_AccountingSettings_InventoryAccountId",
            schema: "dbo",
            table: "tbl_AccountingSettings");

        migrationBuilder.DropColumn(
            name: "InventoryAccountId",
            schema: "dbo",
            table: "tbl_AccountingSettings");
    }
}
