using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261013190000_AddBankCardClearingReconciliation")]
public sealed class AddBankCardClearingReconciliation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "tbl_BankStatements",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StatementNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                OpeningBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                ClosingBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false),
                SourceType = table.Column<byte>(type: "tinyint", nullable: false),
                ImportedFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                ImportedFileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                ImportedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                ImportedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ReconciledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                ReconciledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                table.PrimaryKey("PK_tbl_BankStatements", x => x.Id);
                table.CheckConstraint("CK_BankStatements_Period", "[FromDate] <= [ToDate]");
                table.ForeignKey(
                    name: "FK_BankStatements_BankAccount",
                    column: x => x.BankAccountId,
                    principalSchema: "dbo",
                    principalTable: "tbl_BankAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BankStatements_Currency",
                    column: x => x.CurrencyId,
                    principalSchema: "dbo",
                    principalTable: "tbl_Currencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "tbl_BankReconciliations",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BankStatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReconciliationDate = table.Column<DateOnly>(type: "date", nullable: false),
                BookClosingBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                StatementClosingBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                Difference = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false),
                SubmittedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                ClosedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                ReopenedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ReopenedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                ReopenReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                table.PrimaryKey("PK_tbl_BankReconciliations", x => x.Id);
                table.ForeignKey(
                    name: "FK_BankReconciliations_BankAccount",
                    column: x => x.BankAccountId,
                    principalSchema: "dbo",
                    principalTable: "tbl_BankAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BankReconciliations_BankStatement",
                    column: x => x.BankStatementId,
                    principalSchema: "dbo",
                    principalTable: "tbl_BankStatements",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "tbl_BankStatementLines",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BankStatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                ValueDate = table.Column<DateOnly>(type: "date", nullable: true),
                Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                DebitAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                CreditAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                RunningBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                ExternalTransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                MatchStatus = table.Column<byte>(type: "tinyint", nullable: false),
                IsIgnored = table.Column<bool>(type: "bit", nullable: false),
                IgnoreReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IgnoredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                IgnoredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                table.PrimaryKey("PK_tbl_BankStatementLines", x => x.Id);
                table.CheckConstraint("CK_BankStatementLines_Debit_NonNegative", "[DebitAmount] >= 0");
                table.CheckConstraint("CK_BankStatementLines_Credit_NonNegative", "[CreditAmount] >= 0");
                table.CheckConstraint("CK_BankStatementLines_OneSidedAmount", "NOT ([DebitAmount] > 0 AND [CreditAmount] > 0)");
                table.CheckConstraint("CK_BankStatementLines_NonZeroAmount", "[DebitAmount] > 0 OR [CreditAmount] > 0");
                table.ForeignKey(
                    name: "FK_BankStatementLines_BankStatement",
                    column: x => x.BankStatementId,
                    principalSchema: "dbo",
                    principalTable: "tbl_BankStatements",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "tbl_BankReconciliationMatches",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BankReconciliationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BankStatementLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                JournalEntryLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ReceiptVoucherLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                PaymentVoucherLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                MatchedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                MatchType = table.Column<byte>(type: "tinyint", nullable: false),
                MatchMethod = table.Column<byte>(type: "tinyint", nullable: false),
                IsVoided = table.Column<bool>(type: "bit", nullable: false),
                VoidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                VoidedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                CreatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tbl_BankReconciliationMatches", x => x.Id);
                table.CheckConstraint("CK_BankReconciliationMatches_MatchedAmount_Positive", "[MatchedAmount] > 0");
                table.CheckConstraint(
                    "CK_BankReconciliationMatches_ExactlyOneSource",
                    "(([MatchType] = 1 AND [JournalEntryLineId] IS NOT NULL AND [ReceiptVoucherLineId] IS NULL AND [PaymentVoucherLineId] IS NULL) OR ([MatchType] = 2 AND [JournalEntryLineId] IS NULL AND [ReceiptVoucherLineId] IS NOT NULL AND [PaymentVoucherLineId] IS NULL) OR ([MatchType] = 3 AND [JournalEntryLineId] IS NULL AND [ReceiptVoucherLineId] IS NULL AND [PaymentVoucherLineId] IS NOT NULL))");
                table.ForeignKey(
                    name: "FK_BankReconciliationMatches_BankReconciliation",
                    column: x => x.BankReconciliationId,
                    principalSchema: "dbo",
                    principalTable: "tbl_BankReconciliations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BankReconciliationMatches_BankStatementLine",
                    column: x => x.BankStatementLineId,
                    principalSchema: "dbo",
                    principalTable: "tbl_BankStatementLines",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BankReconciliationMatches_JournalEntryLine",
                    column: x => x.JournalEntryLineId,
                    principalSchema: "dbo",
                    principalTable: "tbl_JournalEntryLines",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BankReconciliationMatches_ReceiptVoucherLine",
                    column: x => x.ReceiptVoucherLineId,
                    principalSchema: "dbo",
                    principalTable: "tbl_ReceiptVoucherLines",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BankReconciliationMatches_PaymentVoucherLine",
                    column: x => x.PaymentVoucherLineId,
                    principalSchema: "dbo",
                    principalTable: "tbl_PaymentVoucherLines",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BankStatements_BankAccountId_FromDate_ToDate",
            schema: "dbo",
            table: "tbl_BankStatements",
            columns: new[] { "BankAccountId", "FromDate", "ToDate" });

        migrationBuilder.CreateIndex(
            name: "IX_BankStatements_Status",
            schema: "dbo",
            table: "tbl_BankStatements",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_BankStatements_BankAccountId_StatementNumber",
            schema: "dbo",
            table: "tbl_BankStatements",
            columns: new[] { "BankAccountId", "StatementNumber" });

        migrationBuilder.CreateIndex(
            name: "UX_BankStatements_ImportedFileHash_Active",
            schema: "dbo",
            table: "tbl_BankStatements",
            column: "ImportedFileHash",
            unique: true,
            filter: "[ImportedFileHash] IS NOT NULL AND [Status] <> 5");

        migrationBuilder.CreateIndex(
            name: "IX_BankStatementLines_Statement_TransactionDate",
            schema: "dbo",
            table: "tbl_BankStatementLines",
            columns: new[] { "BankStatementId", "TransactionDate" });

        migrationBuilder.CreateIndex(
            name: "IX_BankStatementLines_Statement_MatchStatus",
            schema: "dbo",
            table: "tbl_BankStatementLines",
            columns: new[] { "BankStatementId", "MatchStatus" });

        migrationBuilder.CreateIndex(
            name: "IX_BankStatementLines_Statement_ExternalTransactionId",
            schema: "dbo",
            table: "tbl_BankStatementLines",
            columns: new[] { "BankStatementId", "ExternalTransactionId" },
            filter: "[ExternalTransactionId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_BankReconciliations_BankAccountId",
            schema: "dbo",
            table: "tbl_BankReconciliations",
            column: "BankAccountId");

        migrationBuilder.CreateIndex(
            name: "UX_BankReconciliations_BankStatementId_Active",
            schema: "dbo",
            table: "tbl_BankReconciliations",
            column: "BankStatementId",
            unique: true,
            filter: "[Status] <> 5");

        migrationBuilder.CreateIndex(
            name: "IX_BankReconciliations_Status",
            schema: "dbo",
            table: "tbl_BankReconciliations",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_BankReconciliations_ReconciliationDate",
            schema: "dbo",
            table: "tbl_BankReconciliations",
            column: "ReconciliationDate");

        migrationBuilder.CreateIndex(
            name: "IX_BankReconciliationMatches_Reconciliation_StatementLine",
            schema: "dbo",
            table: "tbl_BankReconciliationMatches",
            columns: new[] { "BankReconciliationId", "BankStatementLineId" });

        migrationBuilder.CreateIndex(
            name: "IX_BankReconciliationMatches_JournalEntryLine_Active",
            schema: "dbo",
            table: "tbl_BankReconciliationMatches",
            column: "JournalEntryLineId",
            filter: "[JournalEntryLineId] IS NOT NULL AND [IsVoided] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_BankReconciliationMatches_ReceiptVoucherLine_Active",
            schema: "dbo",
            table: "tbl_BankReconciliationMatches",
            column: "ReceiptVoucherLineId",
            filter: "[ReceiptVoucherLineId] IS NOT NULL AND [IsVoided] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_BankReconciliationMatches_PaymentVoucherLine_Active",
            schema: "dbo",
            table: "tbl_BankReconciliationMatches",
            column: "PaymentVoucherLineId",
            filter: "[PaymentVoucherLineId] IS NOT NULL AND [IsVoided] = 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "tbl_BankReconciliationMatches", schema: "dbo");
        migrationBuilder.DropTable(name: "tbl_BankReconciliations", schema: "dbo");
        migrationBuilder.DropTable(name: "tbl_BankStatementLines", schema: "dbo");
        migrationBuilder.DropTable(name: "tbl_BankStatements", schema: "dbo");
    }
}
