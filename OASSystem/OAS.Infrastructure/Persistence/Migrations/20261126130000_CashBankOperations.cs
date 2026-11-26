using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261126130000_CashBankOperations")]
public sealed class CashBankOperations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT AccountId FROM (
                SELECT AccountId FROM dbo.tbl_CashAccounts
                UNION ALL SELECT AccountId FROM dbo.tbl_BankAccounts
            ) a GROUP BY AccountId HAVING COUNT(*) > 1)
                THROW 51054, 'Shared cash/bank ledger accounts exist. Reconcile mappings before migration.', 1;
            """);
        migrationBuilder.AddColumn<bool>(name: "RequiresOpenShift", schema: "dbo", table: "tbl_CashAccounts", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<decimal>(name: "BookOpeningBalance", schema: "dbo", table: "tbl_CashShifts", type: "decimal(19,4)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "OpeningDifferenceReason", schema: "dbo", table: "tbl_CashShifts", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "SettlementJournalEntryId", schema: "dbo", table: "tbl_CashShifts", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "SettlementAccountId", schema: "dbo", table: "tbl_CashShifts", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<DateOnly>(name: "SettlementDate", schema: "dbo", table: "tbl_CashShifts", type: "date", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SettlementReason", schema: "dbo", table: "tbl_CashShifts", type: "nvarchar(400)", maxLength: 400, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_tbl_CashShifts_SettlementJournalEntryId", schema: "dbo", table: "tbl_CashShifts", column: "SettlementJournalEntryId");
        migrationBuilder.CreateIndex(name: "IX_tbl_CashShifts_SettlementAccountId", schema: "dbo", table: "tbl_CashShifts", column: "SettlementAccountId");
        migrationBuilder.AddForeignKey(name: "FK_tbl_CashShifts_tbl_JournalEntries_SettlementJournalEntryId", schema: "dbo", table: "tbl_CashShifts", column: "SettlementJournalEntryId", principalSchema: "dbo", principalTable: "tbl_JournalEntries", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_tbl_CashShifts_tbl_Accounts_SettlementAccountId", schema: "dbo", table: "tbl_CashShifts", column: "SettlementAccountId", principalSchema: "dbo", principalTable: "tbl_Accounts", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddColumn<Guid>(name: "CurrencyId", schema: "dbo", table: "tbl_Expenses", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>(name: "CurrencyCodeSnapshot", schema: "dbo", table: "tbl_Expenses", type: "nvarchar(16)", maxLength: 16, nullable: true);
        migrationBuilder.AddColumn<byte>(name: "CurrencyDecimalPlacesSnapshot", schema: "dbo", table: "tbl_Expenses", type: "tinyint", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "BaseCurrencyId", schema: "dbo", table: "tbl_Expenses", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>(name: "BaseCurrencyCodeSnapshot", schema: "dbo", table: "tbl_Expenses", type: "nvarchar(16)", maxLength: 16, nullable: true);
        migrationBuilder.AddColumn<byte>(name: "BaseCurrencyDecimalPlacesSnapshot", schema: "dbo", table: "tbl_Expenses", type: "tinyint", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "BaseAmount", schema: "dbo", table: "tbl_Expenses", type: "decimal(19,4)", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "ExchangeRate", schema: "dbo", table: "tbl_Expenses", type: "decimal(19,8)", nullable: true);
        migrationBuilder.AddColumn<DateOnly>(name: "ExchangeRateDate", schema: "dbo", table: "tbl_Expenses", type: "date", nullable: true);
        migrationBuilder.AddColumn<byte>(name: "ExchangeRateType", schema: "dbo", table: "tbl_Expenses", type: "tinyint", nullable: true);
        migrationBuilder.AddColumn<byte>(name: "ExchangeRateSource", schema: "dbo", table: "tbl_Expenses", type: "tinyint", nullable: true);
        foreach (var table in new[] { "CashAccounts", "BankAccounts" })
        {
            migrationBuilder.DropIndex(name: $"IX_{table}_AccountId", schema: "dbo", table: $"tbl_{table}");
            migrationBuilder.CreateIndex(name: $"IX_{table}_AccountId", schema: "dbo", table: $"tbl_{table}", column: "AccountId", unique: true);
        }
        migrationBuilder.CreateTable(name: "tbl_CashTransfers", schema: "dbo", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            TransferNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
            TransferDate = table.Column<DateOnly>(type: "date", nullable: false),
            SourceAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            DestinationAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            SourceCurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            DestinationCurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            SourceName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
            DestinationName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
            SourceCurrencyCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
            DestinationCurrencyCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
            SourceAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
            DestinationAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
            FeeAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
            FeeAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
            Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
            Status = table.Column<byte>(type: "tinyint", nullable: false),
            ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            ApprovedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
            JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            ReversalJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            ReversalDate = table.Column<DateOnly>(type: "date", nullable: true),
            ReversalReason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
            SourceBaseAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: true),
            DestinationBaseAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: true),
            FeeBaseAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: true),
            FxDifferenceBase = table.Column<decimal>(type: "decimal(19,4)", nullable: true),
            RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
            CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
            UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
            CreatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
            UpdatedFromDevice = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
        }, constraints: table =>
        {
            table.PrimaryKey("PK_tbl_CashTransfers", x => x.Id);
            table.ForeignKey("FK_tbl_CashTransfers_tbl_Accounts_SourceAccountId", x => x.SourceAccountId, principalSchema: "dbo", principalTable: "tbl_Accounts", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_tbl_CashTransfers_tbl_Accounts_DestinationAccountId", x => x.DestinationAccountId, principalSchema: "dbo", principalTable: "tbl_Accounts", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_tbl_CashTransfers_tbl_Accounts_FeeAccountId", x => x.FeeAccountId, principalSchema: "dbo", principalTable: "tbl_Accounts", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_tbl_CashTransfers_tbl_Currencies_SourceCurrencyId", x => x.SourceCurrencyId, principalSchema: "dbo", principalTable: "tbl_Currencies", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_tbl_CashTransfers_tbl_Currencies_DestinationCurrencyId", x => x.DestinationCurrencyId, principalSchema: "dbo", principalTable: "tbl_Currencies", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_tbl_CashTransfers_tbl_JournalEntries_JournalEntryId", x => x.JournalEntryId, principalSchema: "dbo", principalTable: "tbl_JournalEntries", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_tbl_CashTransfers_tbl_JournalEntries_ReversalJournalEntryId", x => x.ReversalJournalEntryId, principalSchema: "dbo", principalTable: "tbl_JournalEntries", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex(name: "IX_tbl_CashTransfers_SourceAccountId", schema: "dbo", table: "tbl_CashTransfers", column: "SourceAccountId");
        migrationBuilder.CreateIndex(name: "IX_tbl_CashTransfers_DestinationAccountId", schema: "dbo", table: "tbl_CashTransfers", column: "DestinationAccountId");
        migrationBuilder.CreateIndex(name: "IX_tbl_CashTransfers_FeeAccountId", schema: "dbo", table: "tbl_CashTransfers", column: "FeeAccountId");
        migrationBuilder.CreateIndex(name: "IX_tbl_CashTransfers_SourceCurrencyId", schema: "dbo", table: "tbl_CashTransfers", column: "SourceCurrencyId");
        migrationBuilder.CreateIndex(name: "IX_tbl_CashTransfers_DestinationCurrencyId", schema: "dbo", table: "tbl_CashTransfers", column: "DestinationCurrencyId");
        migrationBuilder.CreateIndex(name: "IX_tbl_CashTransfers_JournalEntryId", schema: "dbo", table: "tbl_CashTransfers", column: "JournalEntryId");
        migrationBuilder.CreateIndex(name: "IX_tbl_CashTransfers_ReversalJournalEntryId", schema: "dbo", table: "tbl_CashTransfers", column: "ReversalJournalEntryId");
        migrationBuilder.CreateIndex(name: "UX_CashTransfers_Number", schema: "dbo", table: "tbl_CashTransfers", column: "TransferNumber", unique: true);
        migrationBuilder.CreateIndex(name: "IX_CashTransfers_Date", schema: "dbo", table: "tbl_CashTransfers", column: "TransferDate");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "tbl_CashTransfers", schema: "dbo");
        foreach (var table in new[] { "CashAccounts", "BankAccounts" })
        {
            migrationBuilder.DropIndex(name: $"IX_{table}_AccountId", schema: "dbo", table: $"tbl_{table}");
            migrationBuilder.CreateIndex(name: $"IX_{table}_AccountId", schema: "dbo", table: $"tbl_{table}", column: "AccountId");
        }
        migrationBuilder.DropColumn(name: "CurrencyId", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropColumn(name: "CurrencyCodeSnapshot", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropColumn(name: "CurrencyDecimalPlacesSnapshot", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropColumn(name: "BaseCurrencyId", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropColumn(name: "BaseCurrencyCodeSnapshot", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropColumn(name: "BaseCurrencyDecimalPlacesSnapshot", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropColumn(name: "BaseAmount", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropColumn(name: "ExchangeRate", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropColumn(name: "ExchangeRateDate", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropColumn(name: "ExchangeRateType", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropColumn(name: "ExchangeRateSource", schema: "dbo", table: "tbl_Expenses");
        migrationBuilder.DropForeignKey(name: "FK_tbl_CashShifts_tbl_JournalEntries_SettlementJournalEntryId", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropForeignKey(name: "FK_tbl_CashShifts_tbl_Accounts_SettlementAccountId", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropIndex(name: "IX_tbl_CashShifts_SettlementJournalEntryId", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropIndex(name: "IX_tbl_CashShifts_SettlementAccountId", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropColumn(name: "SettlementJournalEntryId", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropColumn(name: "SettlementAccountId", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropColumn(name: "SettlementDate", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropColumn(name: "SettlementReason", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropColumn(name: "OpeningDifferenceReason", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropColumn(name: "BookOpeningBalance", schema: "dbo", table: "tbl_CashShifts");
        migrationBuilder.DropColumn(name: "RequiresOpenShift", schema: "dbo", table: "tbl_CashAccounts");
    }
}
