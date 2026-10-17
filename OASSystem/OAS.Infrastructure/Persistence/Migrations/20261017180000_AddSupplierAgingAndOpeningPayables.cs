using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261017180000_AddSupplierAgingAndOpeningPayables")]
public partial class AddSupplierAgingAndOpeningPayables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "DueDate",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<byte>(
            name: "DueDateProvenance",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            type: "tinyint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.AddColumn<int>(
            name: "PaymentTermDaysSnapshot",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            type: "int",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_PurchaseInvoices_DueDateProvenance",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            sql: "[DueDateProvenance] BETWEEN 0 AND 3");

        migrationBuilder.AddCheckConstraint(
            name: "CK_PurchaseInvoices_PaymentTermDaysSnapshot",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            sql: "[PaymentTermDaysSnapshot] IS NULL OR [PaymentTermDaysSnapshot] >= 0");

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseInvoices_Supplier_DueDate_Status",
            schema: "dbo",
            table: "tbl_PurchaseInvoices",
            columns: new[] { "SupplierId", "DueDate", "Status" });

        migrationBuilder.CreateTable(
            name: "tbl_PurchaseInvoiceDueDateAudits",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PurchaseInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OldDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                OldPaymentTermDays = table.Column<int>(type: "int", nullable: true),
                OldProvenance = table.Column<byte>(type: "tinyint", nullable: false),
                NewDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                NewPaymentTermDays = table.Column<int>(type: "int", nullable: true),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                VerifiedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                VerifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                CreatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tbl_PurchaseInvoiceDueDateAudits", x => x.Id);
                table.ForeignKey(
                    name: "FK_PurchaseInvoiceDueDateAudits_PurchaseInvoices_PurchaseInvoiceId",
                    column: x => x.PurchaseInvoiceId,
                    principalSchema: "dbo",
                    principalTable: "tbl_PurchaseInvoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "tbl_SupplierOpeningBatches",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BatchCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                AsOfDate = table.Column<DateOnly>(type: "date", nullable: false),
                BaseCurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false),
                JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                PostedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                CreatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tbl_SupplierOpeningBatches", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierOpeningBatches_Currencies_BaseCurrencyId",
                    column: x => x.BaseCurrencyId,
                    principalSchema: "dbo",
                    principalTable: "tbl_Currencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierOpeningBatches_JournalEntries_JournalEntryId",
                    column: x => x.JournalEntryId,
                    principalSchema: "dbo",
                    principalTable: "tbl_JournalEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "tbl_SupplierOpeningItems",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LegacyReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                DocumentDate = table.Column<DateOnly>(type: "date", nullable: false),
                DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                DueDateProvenance = table.Column<byte>(type: "tinyint", nullable: false),
                CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CurrencyCodeSnapshot = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                CurrencyDecimalPlacesSnapshot = table.Column<byte>(type: "tinyint", nullable: false),
                OriginalOpenAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                ExchangeRateSnapshot = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                BaseOpenAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                CreatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tbl_SupplierOpeningItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierOpeningItems_Batches_BatchId",
                    column: x => x.BatchId,
                    principalSchema: "dbo",
                    principalTable: "tbl_SupplierOpeningBatches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierOpeningItems_Currencies_CurrencyId",
                    column: x => x.CurrencyId,
                    principalSchema: "dbo",
                    principalTable: "tbl_Currencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierOpeningItems_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalSchema: "dbo",
                    principalTable: "tbl_Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_SupplierOpeningItems_OriginalOpenAmount_Positive", "[OriginalOpenAmount] > 0");
                table.CheckConstraint("CK_SupplierOpeningItems_ExchangeRateSnapshot_Positive", "[ExchangeRateSnapshot] > 0");
                table.CheckConstraint("CK_SupplierOpeningItems_BaseOpenAmount_Positive", "[BaseOpenAmount] > 0");
                table.CheckConstraint("CK_SupplierOpeningItems_CurrencyDecimals", "[CurrencyDecimalPlacesSnapshot] BETWEEN 0 AND 6");
                table.CheckConstraint("CK_SupplierOpeningItems_DueDateProvenance", "[DueDateProvenance] BETWEEN 0 AND 2");
            });

        migrationBuilder.CreateTable(
            name: "tbl_SupplierOpeningApplications",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupplierOpeningItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentVoucherLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AppliedSourceBaseAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                AppliedTargetBaseAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                AppliedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                AppliedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false),
                IdempotencyKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                ReversedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                ReversedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                ReversalReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                CreatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tbl_SupplierOpeningApplications", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupplierOpeningApplications_Items_ItemId",
                    column: x => x.SupplierOpeningItemId,
                    principalSchema: "dbo",
                    principalTable: "tbl_SupplierOpeningItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierOpeningApplications_PaymentVoucherLines_LineId",
                    column: x => x.PaymentVoucherLineId,
                    principalSchema: "dbo",
                    principalTable: "tbl_PaymentVoucherLines",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplierOpeningApplications_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalSchema: "dbo",
                    principalTable: "tbl_Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_SupplierOpeningApplications_SourceAmount_Positive", "[AppliedSourceBaseAmount] > 0");
                table.CheckConstraint("CK_SupplierOpeningApplications_TargetAmount_Positive", "[AppliedTargetBaseAmount] > 0");
                table.CheckConstraint("CK_SupplierOpeningApplications_Status", "[Status] IN (1,2)");
            });

        migrationBuilder.CreateIndex("IX_PurchaseInvoiceDueDateAudits_Invoice_VerifiedAt", "tbl_PurchaseInvoiceDueDateAudits", new[] { "PurchaseInvoiceId", "VerifiedAtUtc" }, schema: "dbo");
        migrationBuilder.CreateIndex("IX_SupplierOpeningBatches_AsOf_Status", "tbl_SupplierOpeningBatches", new[] { "AsOfDate", "Status" }, schema: "dbo");
        migrationBuilder.CreateIndex("IX_SupplierOpeningBatches_BaseCurrencyId", "tbl_SupplierOpeningBatches", "BaseCurrencyId", schema: "dbo");
        migrationBuilder.CreateIndex("UX_SupplierOpeningBatches_BatchCode", "tbl_SupplierOpeningBatches", "BatchCode", schema: "dbo", unique: true);
        migrationBuilder.CreateIndex("UX_SupplierOpeningBatches_JournalEntryId", "tbl_SupplierOpeningBatches", "JournalEntryId", schema: "dbo", unique: true, filter: "[JournalEntryId] IS NOT NULL");
        migrationBuilder.CreateIndex("IX_SupplierOpeningItems_BatchId", "tbl_SupplierOpeningItems", "BatchId", schema: "dbo");
        migrationBuilder.CreateIndex("IX_SupplierOpeningItems_CurrencyId", "tbl_SupplierOpeningItems", "CurrencyId", schema: "dbo");
        migrationBuilder.CreateIndex("IX_SupplierOpeningItems_Supplier_DueDate", "tbl_SupplierOpeningItems", new[] { "SupplierId", "DueDate" }, schema: "dbo");
        migrationBuilder.CreateIndex("UX_SupplierOpeningItems_Batch_Supplier_LegacyReference", "tbl_SupplierOpeningItems", new[] { "BatchId", "SupplierId", "LegacyReference" }, schema: "dbo", unique: true, filter: "[LegacyReference] IS NOT NULL");
        migrationBuilder.CreateIndex("IX_SupplierOpeningApplications_Item_Status", "tbl_SupplierOpeningApplications", new[] { "SupplierOpeningItemId", "Status" }, schema: "dbo");
        migrationBuilder.CreateIndex("IX_SupplierOpeningApplications_PaymentLine_Status", "tbl_SupplierOpeningApplications", new[] { "PaymentVoucherLineId", "Status" }, schema: "dbo");
        migrationBuilder.CreateIndex("IX_SupplierOpeningApplications_SupplierId", "tbl_SupplierOpeningApplications", "SupplierId", schema: "dbo");
        migrationBuilder.CreateIndex("UX_SupplierOpeningApplications_IdempotencyKey", "tbl_SupplierOpeningApplications", "IdempotencyKey", schema: "dbo", unique: true, filter: "[IdempotencyKey] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "tbl_PurchaseInvoiceDueDateAudits", schema: "dbo");
        migrationBuilder.DropTable(name: "tbl_SupplierOpeningApplications", schema: "dbo");
        migrationBuilder.DropTable(name: "tbl_SupplierOpeningItems", schema: "dbo");
        migrationBuilder.DropTable(name: "tbl_SupplierOpeningBatches", schema: "dbo");
        migrationBuilder.DropIndex(name: "IX_PurchaseInvoices_Supplier_DueDate_Status", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropCheckConstraint(name: "CK_PurchaseInvoices_DueDateProvenance", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropCheckConstraint(name: "CK_PurchaseInvoices_PaymentTermDaysSnapshot", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropColumn(name: "DueDate", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropColumn(name: "DueDateProvenance", schema: "dbo", table: "tbl_PurchaseInvoices");
        migrationBuilder.DropColumn(name: "PaymentTermDaysSnapshot", schema: "dbo", table: "tbl_PurchaseInvoices");
    }
}
