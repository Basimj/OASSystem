using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260922213000_MoveAccountingTablesToDbo")]
public sealed class MoveAccountingTablesToDbo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
        IF OBJECT_ID(N'[accounting].[tbl_Accounts]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_Accounts]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_Accounts];

        IF OBJECT_ID(N'[accounting].[tbl_FiscalYears]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_FiscalYears]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_FiscalYears];

        IF OBJECT_ID(N'[accounting].[tbl_FiscalPeriods]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_FiscalPeriods]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_FiscalPeriods];

        IF OBJECT_ID(N'[accounting].[tbl_JournalEntries]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_JournalEntries]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_JournalEntries];

        IF OBJECT_ID(N'[accounting].[tbl_JournalEntryLines]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_JournalEntryLines];

        IF OBJECT_ID(N'[accounting].[tbl_PostingProfiles]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_PostingProfiles]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_PostingProfiles];

        IF OBJECT_ID(N'[accounting].[tbl_PostingProfileLines]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_PostingProfileLines]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_PostingProfileLines];

        IF OBJECT_ID(N'[accounting].[tbl_CostCenters]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_CostCenters]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_CostCenters];

        IF OBJECT_ID(N'[accounting].[tbl_CustomerAccounts]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_CustomerAccounts]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_CustomerAccounts];

        IF OBJECT_ID(N'[accounting].[tbl_SupplierAccounts]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_SupplierAccounts]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_SupplierAccounts];

        IF OBJECT_ID(N'[accounting].[tbl_ReceiptVouchers]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_ReceiptVouchers]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_ReceiptVouchers];

        IF OBJECT_ID(N'[accounting].[tbl_ReceiptVoucherLines]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_ReceiptVoucherLines]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_ReceiptVoucherLines];

        IF OBJECT_ID(N'[accounting].[tbl_PaymentVouchers]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_PaymentVouchers]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_PaymentVouchers];

        IF OBJECT_ID(N'[accounting].[tbl_PaymentVoucherLines]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_PaymentVoucherLines]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_PaymentVoucherLines];

        IF OBJECT_ID(N'[accounting].[tbl_PaymentAllocations]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_PaymentAllocations];

        IF OBJECT_ID(N'[accounting].[tbl_CashAccounts]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_CashAccounts]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_CashAccounts];

        IF OBJECT_ID(N'[accounting].[tbl_BankAccounts]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_BankAccounts]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_BankAccounts];

        IF OBJECT_ID(N'[accounting].[tbl_CashShifts]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_CashShifts]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_CashShifts];

        IF OBJECT_ID(N'[accounting].[tbl_ExpenseTypes]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_ExpenseTypes]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_ExpenseTypes];

        IF OBJECT_ID(N'[accounting].[tbl_Expenses]', N'U') IS NOT NULL
           AND OBJECT_ID(N'[dbo].[tbl_Expenses]', N'U') IS NULL
            ALTER SCHEMA [dbo] TRANSFER [accounting].[tbl_Expenses];
        """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
        IF SCHEMA_ID(N'accounting') IS NULL
            EXEC(N'CREATE SCHEMA [accounting]');

        IF OBJECT_ID(N'[dbo].[tbl_Accounts]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_Accounts];

        IF OBJECT_ID(N'[dbo].[tbl_FiscalYears]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_FiscalYears];

        IF OBJECT_ID(N'[dbo].[tbl_FiscalPeriods]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_FiscalPeriods];

        IF OBJECT_ID(N'[dbo].[tbl_JournalEntries]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_JournalEntries];

        IF OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_JournalEntryLines];

        IF OBJECT_ID(N'[dbo].[tbl_PostingProfiles]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_PostingProfiles];

        IF OBJECT_ID(N'[dbo].[tbl_PostingProfileLines]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_PostingProfileLines];

        IF OBJECT_ID(N'[dbo].[tbl_CostCenters]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_CostCenters];

        IF OBJECT_ID(N'[dbo].[tbl_CustomerAccounts]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_CustomerAccounts];

        IF OBJECT_ID(N'[dbo].[tbl_SupplierAccounts]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_SupplierAccounts];

        IF OBJECT_ID(N'[dbo].[tbl_ReceiptVouchers]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_ReceiptVouchers];

        IF OBJECT_ID(N'[dbo].[tbl_ReceiptVoucherLines]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_ReceiptVoucherLines];

        IF OBJECT_ID(N'[dbo].[tbl_PaymentVouchers]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_PaymentVouchers];

        IF OBJECT_ID(N'[dbo].[tbl_PaymentVoucherLines]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_PaymentVoucherLines];

        IF OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_PaymentAllocations];

        IF OBJECT_ID(N'[dbo].[tbl_CashAccounts]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_CashAccounts];

        IF OBJECT_ID(N'[dbo].[tbl_BankAccounts]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_BankAccounts];

        IF OBJECT_ID(N'[dbo].[tbl_CashShifts]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_CashShifts];

        IF OBJECT_ID(N'[dbo].[tbl_ExpenseTypes]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_ExpenseTypes];

        IF OBJECT_ID(N'[dbo].[tbl_Expenses]', N'U') IS NOT NULL
            ALTER SCHEMA [accounting] TRANSFER [dbo].[tbl_Expenses];
        """);
    }
}