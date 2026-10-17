using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261017163000_AddPaymentAllocationConcurrencyAndReversal")]
public sealed class AddPaymentAllocationConcurrencyAndReversal : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IdempotencyKey",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            type: "nvarchar(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ReversedAtUtc",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            type: "datetime2(3)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReversedBy",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReversalReason",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            type: "nvarchar(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            type: "rowversion",
            rowVersion: true,
            nullable: false);

        migrationBuilder.AddCheckConstraint(
            name: "CK_PaymentAllocations_ReversalAudit",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            sql: "([ReversedAtUtc] IS NULL AND [ReversedBy] IS NULL AND [ReversalReason] IS NULL) OR ([ReversedAtUtc] IS NOT NULL AND [ReversedBy] IS NOT NULL AND [ReversalReason] IS NOT NULL)");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentAllocations_PaymentVoucherLine_Date",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            columns: new[] { "PaymentVoucherLineId", "AllocatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "UX_PaymentAllocations_IdempotencyKey",
            schema: "dbo",
            table: "tbl_PaymentAllocations",
            column: "IdempotencyKey",
            unique: true,
            filter: "[IdempotencyKey] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PaymentAllocations_PaymentVoucherLine_Date",
            schema: "dbo",
            table: "tbl_PaymentAllocations");

        migrationBuilder.DropIndex(
            name: "UX_PaymentAllocations_IdempotencyKey",
            schema: "dbo",
            table: "tbl_PaymentAllocations");

        migrationBuilder.DropCheckConstraint(
            name: "CK_PaymentAllocations_ReversalAudit",
            schema: "dbo",
            table: "tbl_PaymentAllocations");

        migrationBuilder.DropColumn(name: "IdempotencyKey", schema: "dbo", table: "tbl_PaymentAllocations");
        migrationBuilder.DropColumn(name: "ReversedAtUtc", schema: "dbo", table: "tbl_PaymentAllocations");
        migrationBuilder.DropColumn(name: "ReversedBy", schema: "dbo", table: "tbl_PaymentAllocations");
        migrationBuilder.DropColumn(name: "ReversalReason", schema: "dbo", table: "tbl_PaymentAllocations");
        migrationBuilder.DropColumn(name: "RowVersion", schema: "dbo", table: "tbl_PaymentAllocations");
    }
}
