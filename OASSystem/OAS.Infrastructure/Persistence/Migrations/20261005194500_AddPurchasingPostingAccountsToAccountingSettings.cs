using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261005194500_AddPurchasingPostingAccountsToAccountingSettings")]
public sealed class AddPurchasingPostingAccountsToAccountingSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "GrniAccountId",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PurchaseTaxAccountId",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PurchasePriceVarianceAccountId",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_tbl_AccountingSettings_GrniAccountId",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            column: "GrniAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_tbl_AccountingSettings_PurchaseTaxAccountId",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            column: "PurchaseTaxAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_tbl_AccountingSettings_PurchasePriceVarianceAccountId",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            column: "PurchasePriceVarianceAccountId");

        migrationBuilder.AddForeignKey(
            name: "FK_AccountingSettings_GrniAccount",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            column: "GrniAccountId",
            principalSchema: "dbo",
            principalTable: "tbl_Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_AccountingSettings_PurchaseTaxAccount",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            column: "PurchaseTaxAccountId",
            principalSchema: "dbo",
            principalTable: "tbl_Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_AccountingSettings_PurchasePriceVarianceAccount",
            schema: "dbo",
            table: "tbl_AccountingSettings",
            column: "PurchasePriceVarianceAccountId",
            principalSchema: "dbo",
            principalTable: "tbl_Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateIndex(
            name: "UX_PostingProfiles_Active_PurchaseReceipt",
            schema: "dbo",
            table: "tbl_PostingProfiles",
            columns: new[] { "Module", "DocumentType" },
            unique: true,
            filter: "[IsActive] = 1 AND [Module] = N'Purchasing' AND [DocumentType] = N'PurchaseReceipt'");

        migrationBuilder.CreateIndex(
            name: "UX_PostingProfiles_Active_PurchaseInvoice",
            schema: "dbo",
            table: "tbl_PostingProfiles",
            columns: new[] { "Module", "DocumentType" },
            unique: true,
            filter: "[IsActive] = 1 AND [Module] = N'Purchasing' AND [DocumentType] = N'PurchaseInvoice'");

        // Profiles and lines are intentionally not seeded with account IDs here.
        // They are provisioned atomically by UpdateAccountingSettings using the
        // administrator-selected posting accounts, avoiding environment-specific IDs.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "UX_PostingProfiles_Active_PurchaseReceipt", schema: "dbo", table: "tbl_PostingProfiles");
        migrationBuilder.DropIndex(name: "UX_PostingProfiles_Active_PurchaseInvoice", schema: "dbo", table: "tbl_PostingProfiles");
        migrationBuilder.DropForeignKey(name: "FK_AccountingSettings_GrniAccount", schema: "dbo", table: "tbl_AccountingSettings");
        migrationBuilder.DropForeignKey(name: "FK_AccountingSettings_PurchaseTaxAccount", schema: "dbo", table: "tbl_AccountingSettings");
        migrationBuilder.DropForeignKey(name: "FK_AccountingSettings_PurchasePriceVarianceAccount", schema: "dbo", table: "tbl_AccountingSettings");
        migrationBuilder.DropIndex(name: "IX_tbl_AccountingSettings_GrniAccountId", schema: "dbo", table: "tbl_AccountingSettings");
        migrationBuilder.DropIndex(name: "IX_tbl_AccountingSettings_PurchaseTaxAccountId", schema: "dbo", table: "tbl_AccountingSettings");
        migrationBuilder.DropIndex(name: "IX_tbl_AccountingSettings_PurchasePriceVarianceAccountId", schema: "dbo", table: "tbl_AccountingSettings");
        migrationBuilder.DropColumn(name: "GrniAccountId", schema: "dbo", table: "tbl_AccountingSettings");
        migrationBuilder.DropColumn(name: "PurchaseTaxAccountId", schema: "dbo", table: "tbl_AccountingSettings");
        migrationBuilder.DropColumn(name: "PurchasePriceVarianceAccountId", schema: "dbo", table: "tbl_AccountingSettings");
    }
}
