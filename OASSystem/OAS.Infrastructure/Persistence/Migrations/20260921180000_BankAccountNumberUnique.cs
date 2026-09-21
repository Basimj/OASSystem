using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace OAS.Infrastructure.Persistence.Migrations;
[DbContext(typeof(OasDbContext))]
[Migration("20260921180000_BankAccountNumberUnique")]
public sealed class BankAccountNumberUnique : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT AccountNumber FROM accounting.BankAccounts GROUP BY AccountNumber HAVING COUNT(*) > 1)
                THROW 51010, 'Duplicate bank account numbers must be resolved before applying this migration.', 1;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BankAccounts_AccountNumber' AND object_id = OBJECT_ID('accounting.BankAccounts'))
                CREATE UNIQUE INDEX UX_BankAccounts_AccountNumber ON accounting.BankAccounts(AccountNumber);
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("DROP INDEX IF EXISTS UX_BankAccounts_AccountNumber ON accounting.BankAccounts;");
}
