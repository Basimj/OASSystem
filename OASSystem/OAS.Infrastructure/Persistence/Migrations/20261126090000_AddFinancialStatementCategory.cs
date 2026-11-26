using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261126090000_AddFinancialStatementCategory")]
public sealed class AddFinancialStatementCategory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(name: "FinancialStatementCategory", schema: "dbo", table: "tbl_Accounts", type: "tinyint", nullable: true);
        // Use actual relationships, never account names or codes. Conflicting proposals remain unclassified.
        migrationBuilder.Sql("""
            ;WITH Candidates AS (
                SELECT CashParentAccountId AS Id, 1 AS Category, 1 AS [Class] FROM dbo.tbl_AccountingSettings
                UNION ALL SELECT BankParentAccountId, 1, 1 FROM dbo.tbl_AccountingSettings
                UNION ALL SELECT InventoryAccountId, 1, 1 FROM dbo.tbl_AccountingSettings
                UNION ALL SELECT GrniAccountId, 3, 2 FROM dbo.tbl_AccountingSettings
                UNION ALL SELECT RetainedEarningsAccountId, 5, 3 FROM dbo.tbl_AccountingSettings
                UNION ALL SELECT ExchangeGainAccountId, 8, 4 FROM dbo.tbl_AccountingSettings
                UNION ALL SELECT ExchangeLossAccountId, 11, 5 FROM dbo.tbl_AccountingSettings
                UNION ALL SELECT AccountId, 1, 1 FROM dbo.tbl_Customers
                UNION ALL SELECT AccountId, 3, 2 FROM dbo.tbl_Suppliers
                UNION ALL SELECT AccountId, 1, 1 FROM dbo.tbl_CashAccounts
                UNION ALL SELECT AccountId, 1, 1 FROM dbo.tbl_BankAccounts
                UNION ALL SELECT l.AccountId, 6, 4 FROM dbo.tbl_PostingProfileLines l JOIN dbo.tbl_PostingProfiles p ON p.Id=l.PostingProfileId WHERE p.IsActive=1 AND p.Module='Sales' AND p.DocumentType='SalesInvoice' AND l.AccountRole='SalesRevenue'
                UNION ALL SELECT l.AccountId, 9, 5 FROM dbo.tbl_PostingProfileLines l JOIN dbo.tbl_PostingProfiles p ON p.Id=l.PostingProfileId WHERE p.IsActive=1 AND p.Module='Sales' AND p.DocumentType='SalesInvoice' AND l.AccountRole IN ('COGS','SalesCOGS')
                UNION ALL SELECT l.AccountId, 1, 1 FROM dbo.tbl_PostingProfileLines l JOIN dbo.tbl_PostingProfiles p ON p.Id=l.PostingProfileId WHERE p.IsActive=1 AND p.Module='Sales' AND p.DocumentType='SalesInvoice' AND l.AccountRole='Inventory'
                UNION ALL SELECT l.AccountId, 3, 2 FROM dbo.tbl_PostingProfileLines l JOIN dbo.tbl_PostingProfiles p ON p.Id=l.PostingProfileId WHERE p.IsActive=1 AND p.Module='Sales' AND p.DocumentType='SalesInvoice' AND l.AccountRole='TaxPayable'
            ), Compatible AS (
                SELECT c.Id, MIN(c.Category) AS Category FROM Candidates c JOIN dbo.tbl_Accounts a ON a.Id=c.Id AND a.AccountClass=c.[Class]
                GROUP BY c.Id HAVING MIN(c.Category)=MAX(c.Category)
            )
            UPDATE a SET FinancialStatementCategory=c.Category FROM dbo.tbl_Accounts a JOIN Compatible c ON c.Id=a.Id
            WHERE a.FinancialStatementCategory IS NULL;
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "FinancialStatementCategory", schema: "dbo", table: "tbl_Accounts");
}
