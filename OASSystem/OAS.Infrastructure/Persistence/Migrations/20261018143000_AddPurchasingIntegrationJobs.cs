using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261018143000_AddPurchasingIntegrationJobs")]
public partial class AddPurchasingIntegrationJobs : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable(
            name: "tbl_PurchasingIntegrationJobs", schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PurchaseReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                PurchaseRequestLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                TargetKind = table.Column<byte>(type: "tinyint", nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false),
                AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                LeaseOwner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                LeaseExpiresAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                LastErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                LastErrorSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tbl_PurchasingIntegrationJobs", x => x.Id);
                table.ForeignKey(
                    name: "FK_PurchasingIntegrationJobs_Receipts",
                    column: x => x.PurchaseReceiptId,
                    principalTable: "tbl_PurchaseReceipts",
                    principalColumn: "Id",
                    principalSchema: "dbo",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PurchasingIntegrationJobs_CustomerOrders",
                    column: x => x.CustomerOrderId,
                    principalTable: "tbl_CustomerOrders",
                    principalColumn: "Id",
                    principalSchema: "dbo",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PurchasingIntegrationJobs_RequestLines",
                    column: x => x.PurchaseRequestLineId,
                    principalTable: "tbl_PurchaseRequestLines",
                    principalColumn: "Id",
                    principalSchema: "dbo",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_PurchasingIntegrationJobs_Target",
                    "([TargetKind] = 1 AND [CustomerOrderId] IS NOT NULL AND [PurchaseRequestLineId] IS NULL) OR ([TargetKind] = 2 AND [PurchaseRequestLineId] IS NOT NULL AND [CustomerOrderId] IS NULL)");
                table.CheckConstraint("CK_PurchasingIntegrationJobs_Attempts", "[AttemptCount] >= 0");
                table.CheckConstraint("CK_PurchasingIntegrationJobs_Status", "[Status] BETWEEN 1 AND 5");
            });
        m.CreateIndex("UX_PurchasingIntegrationJobs_Receipt_CustomerOrder", "tbl_PurchasingIntegrationJobs",
            new[] { "PurchaseReceiptId", "CustomerOrderId" }, "dbo", unique: true, filter: "[CustomerOrderId] IS NOT NULL");
        m.CreateIndex("UX_PurchasingIntegrationJobs_Receipt_RequestLine", "tbl_PurchasingIntegrationJobs",
            new[] { "PurchaseReceiptId", "PurchaseRequestLineId" }, "dbo", unique: true, filter: "[PurchaseRequestLineId] IS NOT NULL");
        m.CreateIndex("IX_PurchasingIntegrationJobs_Status_NextAttempt", "tbl_PurchasingIntegrationJobs",
            new[] { "Status", "NextAttemptAtUtc", "LeaseExpiresAtUtc" }, "dbo");
        m.CreateIndex("IX_PurchasingIntegrationJobs_CustomerOrderId", "tbl_PurchasingIntegrationJobs", "CustomerOrderId", "dbo");
        m.CreateIndex("IX_PurchasingIntegrationJobs_PurchaseRequestLineId", "tbl_PurchasingIntegrationJobs", "PurchaseRequestLineId", "dbo");

        // Recover historical posted receipts by their persisted PO-line source graph.
        // Reconciliation ports are idempotent: completed customer/optical workflows are no-ops.
        // No inventory or GRNI posting is triggered by these jobs.
        m.Sql("""
            INSERT INTO dbo.tbl_PurchasingIntegrationJobs
              (Id, PurchaseReceiptId, CustomerOrderId, PurchaseRequestLineId, TargetKind,
               Status, AttemptCount, NextAttemptAtUtc, CreatedAtUtc)
            SELECT NEWID(), ReceiptId, CustomerOrderId, NULL, 1, 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
            FROM (
                SELECT DISTINCT r.Id AS ReceiptId, p.CustomerOrderId
                FROM dbo.tbl_PurchaseReceipts r
                JOIN dbo.tbl_PurchaseReceiptLines rl ON rl.PurchaseReceiptId = r.Id
                JOIN dbo.tbl_PurchaseOrderLineSources src ON src.PurchaseOrderLineId = rl.PurchaseOrderLineId
                JOIN dbo.tbl_PurchaseRequestLines prl ON prl.Id = src.PurchaseRequestLineId
                JOIN dbo.tbl_PurchaseRequests p ON p.Id = prl.PurchaseRequestId
                WHERE r.Status = 3 AND rl.AcceptedQuantity > 0 AND p.CustomerOrderId IS NOT NULL
            ) x;

            INSERT INTO dbo.tbl_PurchasingIntegrationJobs
              (Id, PurchaseReceiptId, CustomerOrderId, PurchaseRequestLineId, TargetKind,
               Status, AttemptCount, NextAttemptAtUtc, CreatedAtUtc)
            SELECT NEWID(), ReceiptId, NULL, RequestLineId, 2, 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
            FROM (
                SELECT DISTINCT r.Id AS ReceiptId, src.PurchaseRequestLineId AS RequestLineId
                FROM dbo.tbl_PurchaseReceipts r
                JOIN dbo.tbl_PurchaseReceiptLines rl ON rl.PurchaseReceiptId = r.Id
                JOIN dbo.tbl_PurchaseOrderLineSources src ON src.PurchaseOrderLineId = rl.PurchaseOrderLineId
                WHERE r.Status = 3 AND rl.AcceptedQuantity > 0
            ) x;
            """);
    }

    protected override void Down(MigrationBuilder m) =>
        m.DropTable(name: "tbl_PurchasingIntegrationJobs", schema: "dbo");
}
