using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261017150000_AddSupplierSettlementFxSnapshots")]
public sealed class AddSupplierSettlementFxSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "TargetDocumentAmount",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            type: "decimal(19,4)",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_PaymentAllocations_TargetDocumentAmount_Positive",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            sql: "[TargetDocumentAmount] IS NULL OR [TargetDocumentAmount] > 0");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentAllocations_TargetDocument_Date",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            columns: new[] { "TargetDocumentType", "TargetDocumentId", "AllocatedAtUtc" });

        migrationBuilder.AddColumn<string>(
            name: "CurrencyCodeSnapshot",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            type: "nvarchar(8)",
            maxLength: 8,
            nullable: true);

        migrationBuilder.AddColumn<byte>(
            name: "CurrencyDecimalPlacesSnapshot",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            type: "tinyint",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "BaseCurrencyId",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BaseCurrencyCodeSnapshot",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            type: "nvarchar(8)",
            maxLength: 8,
            nullable: true);

        migrationBuilder.AddColumn<byte>(
            name: "BaseCurrencyDecimalPlacesSnapshot",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            type: "tinyint",
            nullable: true);

        migrationBuilder.AddColumn<byte>(
            name: "ExchangeRateType",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            type: "tinyint",
            nullable: true);

        migrationBuilder.AddColumn<byte>(
            name: "ExchangeRateSource",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            type: "tinyint",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_PurchaseInvoices_CurrencyDecimalPlacesSnapshot",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            sql: "[CurrencyDecimalPlacesSnapshot] IS NULL OR ([CurrencyDecimalPlacesSnapshot] BETWEEN 0 AND 6)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_PurchaseInvoices_BaseCurrencyDecimalPlacesSnapshot",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            sql: "[BaseCurrencyDecimalPlacesSnapshot] IS NULL OR ([BaseCurrencyDecimalPlacesSnapshot] BETWEEN 0 AND 6)");

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseInvoices_BaseCurrencyId",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            column: "BaseCurrencyId");

        migrationBuilder.AddForeignKey(
            name: "FK_PurchaseInvoices_Currencies_BaseCurrencyId",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            column: "BaseCurrencyId",
            principalSchema: "dbo",
            principalTable: "tbl_Currencies",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        // Expand/backfill only descriptive historical snapshots. The invoice's stored
        // ExchangeRate and ExchangeRateDate remain the source of truth; no historical
        // rate is reconstructed from today's exchange-rate table.
        migrationBuilder.Sql("""
            UPDATE pi
            SET
                pi.CurrencyCodeSnapshot = c.Code,
                pi.CurrencyDecimalPlacesSnapshot = c.DecimalPlaces,
                pi.BaseCurrencyId = s.BaseCurrencyId,
                pi.BaseCurrencyCodeSnapshot = bc.Code,
                pi.BaseCurrencyDecimalPlacesSnapshot = bc.DecimalPlaces,
                pi.ExchangeRateType = s.DefaultExchangeRateType,
                pi.ExchangeRateSource = 2
            FROM dbo.tbl_PurchaseInvoices AS pi
            INNER JOIN dbo.tbl_Currencies AS c ON c.Id = pi.CurrencyId
            CROSS JOIN (
                SELECT TOP (1) BaseCurrencyId, DefaultExchangeRateType
                FROM dbo.tbl_AccountingSettings
                ORDER BY Id
            ) AS s
            INNER JOIN dbo.tbl_Currencies AS bc ON bc.Id = s.BaseCurrencyId
            WHERE pi.CurrencyCodeSnapshot IS NULL
               OR pi.BaseCurrencyId IS NULL
               OR pi.ExchangeRateType IS NULL
               OR pi.ExchangeRateSource IS NULL;
            """);

        // Existing AP allocations did not persist the invoice-currency quantity.
        // Derive it from the already persisted historical target carrying amount and
        // the invoice's stored historical exchange rate. PurchaseInvoice enum value = 2.
        migrationBuilder.Sql("""
            UPDATE pa
            SET pa.TargetDocumentAmount = ROUND(
                COALESCE(
                    pa.TargetBaseAllocatedAmount,
                    pa.BaseAllocatedAmount,
                    CASE WHEN pa.ExchangeRate > 0 THEN pa.AllocatedAmount * pa.ExchangeRate END
                ) / pi.ExchangeRate,
                4)
            FROM dbo.tbl_PaymentAllocations AS pa
            INNER JOIN dbo.tbl_PurchaseInvoices AS pi ON pi.Id = pa.TargetDocumentId
            WHERE pa.TargetDocumentType = 2
              AND pa.TargetDocumentAmount IS NULL
              AND pi.ExchangeRate > 0
              AND COALESCE(
                    pa.TargetBaseAllocatedAmount,
                    pa.BaseAllocatedAmount,
                    CASE WHEN pa.ExchangeRate > 0 THEN pa.AllocatedAmount * pa.ExchangeRate END
                  ) > 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_PurchaseInvoices_Currencies_BaseCurrencyId",
            schema: "dbo",
            table: "tbl_PurchaseInvoices");

        migrationBuilder.DropIndex(
            name: "IX_PurchaseInvoices_BaseCurrencyId",
            schema: "dbo",
            table: "tbl_PurchaseInvoices");

        migrationBuilder.DropCheckConstraint(
            name: "CK_PurchaseInvoices_BaseCurrencyDecimalPlacesSnapshot",
            schema: "dbo",
            table: "tbl_PurchaseInvoices");

        migrationBuilder.DropCheckConstraint(
            name: "CK_PurchaseInvoices_CurrencyDecimalPlacesSnapshot",
            schema: "dbo",
            table: "tbl_PurchaseInvoices");

        migrationBuilder.DropColumn(name: "BaseCurrencyCodeSnapshot", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropColumn(name: "BaseCurrencyDecimalPlacesSnapshot", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropColumn(name: "BaseCurrencyId", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropColumn(name: "CurrencyCodeSnapshot", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropColumn(name: "CurrencyDecimalPlacesSnapshot", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropColumn(name: "ExchangeRateSource", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropColumn(name: "ExchangeRateType", schema: "dbo", table: "tbl_PurchaseInvoices");

        migrationBuilder.DropIndex(
            name: "IX_PaymentAllocations_TargetDocument_Date",
            schema: "dbo",
            table: "tbl_PaymentAllocations");

        migrationBuilder.DropCheckConstraint(
            name: "CK_PaymentAllocations_TargetDocumentAmount_Positive",
            schema: "dbo",
            table: "tbl_PaymentAllocations");

        migrationBuilder.DropColumn(
            name: "TargetDocumentAmount",
            schema: "dbo",
            table: "tbl_PaymentAllocations");
    }
}
