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

        // Existing installations may already contain more than one active purchasing
        // posting profile for the same document type. Repair that legacy state before
        // enforcing the filtered unique indexes. Keep the most recently changed row
        // (highest rowversion) active and preserve older profiles as inactive history.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_PostingProfiles]', N'U') IS NULL
    THROW 51120, 'Required table dbo.tbl_PostingProfiles was not found.', 1;

;WITH RankedPurchasingProfiles AS
(
    SELECT
        [Id],
        ROW_NUMBER() OVER
        (
            PARTITION BY [Module], [DocumentType]
            ORDER BY CONVERT(bigint, [RowVersion]) DESC, [Id] DESC
        ) AS [rn]
    FROM [dbo].[tbl_PostingProfiles]
    WHERE [IsActive] = 1
      AND [Module] = N'Purchasing'
      AND [DocumentType] IN (N'PurchaseReceipt', N'PurchaseInvoice')
)
UPDATE p
   SET [IsActive] = 0
FROM [dbo].[tbl_PostingProfiles] p
INNER JOIN RankedPurchasingProfiles r ON r.[Id] = p.[Id]
WHERE r.[rn] > 1;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UX_PostingProfiles_Active_PurchaseReceipt'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PostingProfiles]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_PostingProfiles_Active_PurchaseReceipt]
        ON [dbo].[tbl_PostingProfiles]([Module], [DocumentType])
        WHERE [IsActive] = 1
          AND [Module] = N'Purchasing'
          AND [DocumentType] = N'PurchaseReceipt';
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UX_PostingProfiles_Active_PurchaseInvoice'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PostingProfiles]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_PostingProfiles_Active_PurchaseInvoice]
        ON [dbo].[tbl_PostingProfiles]([Module], [DocumentType])
        WHERE [IsActive] = 1
          AND [Module] = N'Purchasing'
          AND [DocumentType] = N'PurchaseInvoice';
END;
""");

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
