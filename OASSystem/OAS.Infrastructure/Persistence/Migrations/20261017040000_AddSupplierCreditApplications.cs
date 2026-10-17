using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261017040000_AddSupplierCreditApplications")]
public sealed class AddSupplierCreditApplications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "tbl_SupplierCreditApplications",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PurchaseReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PurchaseInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AppliedSourceBaseAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                AppliedTargetBaseAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                AppliedInvoiceCurrencyAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                AppliedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                AppliedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false),
                IdempotencyKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                ReversedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                ReversedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                ReversalReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                CreatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tbl_SupplierCreditApplications", x => x.Id);
                table.CheckConstraint("CK_SupplierCreditApplications_SourceBase_Positive", "[AppliedSourceBaseAmount] > 0");
                table.CheckConstraint("CK_SupplierCreditApplications_TargetBase_Positive", "[AppliedTargetBaseAmount] > 0");
                table.CheckConstraint("CK_SupplierCreditApplications_InvoiceAmount_Positive", "[AppliedInvoiceCurrencyAmount] > 0");
                table.CheckConstraint("CK_SupplierCreditApplications_Status", "[Status] IN (1,2)");
                table.CheckConstraint("CK_SupplierCreditApplications_ReversalState", "([Status] = 1 AND [ReversedAtUtc] IS NULL AND [ReversedBy] IS NULL AND [ReversalReason] IS NULL) OR ([Status] = 2 AND [ReversedAtUtc] IS NOT NULL AND [ReversedBy] IS NOT NULL AND [ReversalReason] IS NOT NULL)");
                table.ForeignKey(
                    name: "FK_SupplierCreditApplications_PurchaseReturns_PurchaseReturnId",
                    column: x => x.PurchaseReturnId,
                    principalSchema: "dbo",
                    principalTable: "tbl_PurchaseReturns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierCreditApplications_PurchaseInvoices_PurchaseInvoiceId",
                    column: x => x.PurchaseInvoiceId,
                    principalSchema: "dbo",
                    principalTable: "tbl_PurchaseInvoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierCreditApplications_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalSchema: "dbo",
                    principalTable: "tbl_Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierCreditApplications_Return_Status",
            schema: "dbo",
            table: "tbl_SupplierCreditApplications",
            columns: new[] { "PurchaseReturnId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierCreditApplications_Invoice_Status",
            schema: "dbo",
            table: "tbl_SupplierCreditApplications",
            columns: new[] { "PurchaseInvoiceId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplierCreditApplications_Supplier_Status",
            schema: "dbo",
            table: "tbl_SupplierCreditApplications",
            columns: new[] { "SupplierId", "Status" });

        migrationBuilder.CreateIndex(
            name: "UX_SupplierCreditApplications_IdempotencyKey",
            schema: "dbo",
            table: "tbl_SupplierCreditApplications",
            column: "IdempotencyKey",
            unique: true,
            filter: "[IdempotencyKey] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "tbl_SupplierCreditApplications",
            schema: "dbo");
    }
}
