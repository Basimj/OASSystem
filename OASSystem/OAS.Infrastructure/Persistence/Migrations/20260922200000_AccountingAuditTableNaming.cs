using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260922200000_AccountingAuditTableNaming")]
public sealed class AccountingAuditTableNaming : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
-- Accounting-only table naming alignment. Existing tables are renamed in place so data,
-- foreign keys and indexes are preserved.
IF OBJECT_ID(N'[accounting].[Accounts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_Accounts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[Accounts]', N'tbl_Accounts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[FiscalYears]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_FiscalYears]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[FiscalYears]', N'tbl_FiscalYears', N'OBJECT';
IF OBJECT_ID(N'[accounting].[FiscalPeriods]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_FiscalPeriods]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[FiscalPeriods]', N'tbl_FiscalPeriods', N'OBJECT';
IF OBJECT_ID(N'[accounting].[JournalEntries]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_JournalEntries]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[JournalEntries]', N'tbl_JournalEntries', N'OBJECT';
IF OBJECT_ID(N'[accounting].[JournalEntryLines]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_JournalEntryLines]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[JournalEntryLines]', N'tbl_JournalEntryLines', N'OBJECT';
IF OBJECT_ID(N'[accounting].[PostingProfiles]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_PostingProfiles]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[PostingProfiles]', N'tbl_PostingProfiles', N'OBJECT';
IF OBJECT_ID(N'[accounting].[PostingProfileLines]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_PostingProfileLines]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[PostingProfileLines]', N'tbl_PostingProfileLines', N'OBJECT';
IF OBJECT_ID(N'[accounting].[CostCenters]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_CostCenters]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[CostCenters]', N'tbl_CostCenters', N'OBJECT';
IF OBJECT_ID(N'[accounting].[CustomerAccounts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_CustomerAccounts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[CustomerAccounts]', N'tbl_CustomerAccounts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[SupplierAccounts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_SupplierAccounts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[SupplierAccounts]', N'tbl_SupplierAccounts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[ReceiptVouchers]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_ReceiptVouchers]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[ReceiptVouchers]', N'tbl_ReceiptVouchers', N'OBJECT';
IF OBJECT_ID(N'[accounting].[ReceiptVoucherLines]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_ReceiptVoucherLines]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[ReceiptVoucherLines]', N'tbl_ReceiptVoucherLines', N'OBJECT';
IF OBJECT_ID(N'[accounting].[PaymentVouchers]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_PaymentVouchers]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[PaymentVouchers]', N'tbl_PaymentVouchers', N'OBJECT';
IF OBJECT_ID(N'[accounting].[PaymentVoucherLines]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_PaymentVoucherLines]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[PaymentVoucherLines]', N'tbl_PaymentVoucherLines', N'OBJECT';
IF OBJECT_ID(N'[accounting].[PaymentAllocations]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_PaymentAllocations]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[PaymentAllocations]', N'tbl_PaymentAllocations', N'OBJECT';
IF OBJECT_ID(N'[accounting].[CashAccounts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_CashAccounts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[CashAccounts]', N'tbl_CashAccounts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[BankAccounts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_BankAccounts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[BankAccounts]', N'tbl_BankAccounts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[CashShifts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_CashShifts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[CashShifts]', N'tbl_CashShifts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[ExpenseTypes]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_ExpenseTypes]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[ExpenseTypes]', N'tbl_ExpenseTypes', N'OBJECT';
IF OBJECT_ID(N'[accounting].[Expenses]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[tbl_Expenses]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[Expenses]', N'tbl_Expenses', N'OBJECT';
""");

        migrationBuilder.Sql("""
-- Accounting audit-column alignment.
-- IMPORTANT:
-- All DDL/DML that touches columns which may be created or renamed in this migration
-- is executed through dynamic SQL. SQL Server compiles a normal batch before running
-- ALTER TABLE / sp_rename, which otherwise causes "Invalid column name 'CreatedAt'".

DECLARE @Tables TABLE
(
    [TableName] sysname NOT NULL PRIMARY KEY
);

INSERT INTO @Tables ([TableName])
VALUES
    (N'tbl_Accounts'),
    (N'tbl_FiscalYears'),
    (N'tbl_FiscalPeriods'),
    (N'tbl_JournalEntries'),
    (N'tbl_JournalEntryLines'),
    (N'tbl_PostingProfiles'),
    (N'tbl_PostingProfileLines'),
    (N'tbl_CostCenters'),
    (N'tbl_CustomerAccounts'),
    (N'tbl_SupplierAccounts'),
    (N'tbl_ReceiptVouchers'),
    (N'tbl_ReceiptVoucherLines'),
    (N'tbl_PaymentVouchers'),
    (N'tbl_PaymentVoucherLines'),
    (N'tbl_PaymentAllocations'),
    (N'tbl_CashAccounts'),
    (N'tbl_BankAccounts'),
    (N'tbl_CashShifts'),
    (N'tbl_ExpenseTypes'),
    (N'tbl_Expenses');

DECLARE @TableName sysname;
DECLARE @LookupName nvarchar(517);
DECLARE @FullName nvarchar(517);
DECLARE @ColumnName nvarchar(776);
DECLARE @Sql nvarchar(max);

DECLARE audit_tables CURSOR LOCAL FAST_FORWARD FOR
    SELECT [TableName]
    FROM @Tables
    ORDER BY [TableName];

OPEN audit_tables;

FETCH NEXT FROM audit_tables INTO @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @LookupName = N'accounting.' + @TableName;
    SET @FullName = QUOTENAME(N'accounting') + N'.' + QUOTENAME(@TableName);

    IF OBJECT_ID(@FullName, N'U') IS NOT NULL
    BEGIN
        ----------------------------------------------------------------------
        -- CreatedAt
        ----------------------------------------------------------------------
        IF COL_LENGTH(@LookupName, N'CreatedAt') IS NULL
           AND COL_LENGTH(@LookupName, N'CreatedAtUtc') IS NOT NULL
        BEGIN
            SET @ColumnName =
                QUOTENAME(N'accounting') + N'.'
                + QUOTENAME(@TableName) + N'.'
                + QUOTENAME(N'CreatedAtUtc');

            EXEC sys.sp_rename
                @objname = @ColumnName,
                @newname = N'CreatedAt',
                @objtype = N'COLUMN';
        END;

        IF COL_LENGTH(@LookupName, N'CreatedAt') IS NULL
        BEGIN
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ADD [CreatedAt] datetimeoffset NULL;';

            EXEC sys.sp_executesql @Sql;
        END;

        -- Normalize any legacy datetime2/datetimeoffset CreatedAt value.
        SET @Sql =
            N'ALTER TABLE ' + @FullName
            + N' ALTER COLUMN [CreatedAt] datetimeoffset NULL;';

        EXEC sys.sp_executesql @Sql;

        SET @Sql =
            N'UPDATE ' + @FullName
            + N'
                 SET [CreatedAt] = TODATETIMEOFFSET(SYSUTCDATETIME(), ''+00:00'')
               WHERE [CreatedAt] IS NULL;';

        EXEC sys.sp_executesql @Sql;

        SET @Sql =
            N'ALTER TABLE ' + @FullName
            + N' ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;';

        EXEC sys.sp_executesql @Sql;

        ----------------------------------------------------------------------
        -- CreatedBy
        ----------------------------------------------------------------------
        IF COL_LENGTH(@LookupName, N'CreatedBy') IS NULL
        BEGIN
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ADD [CreatedBy] nvarchar(64) NULL;';

            EXEC sys.sp_executesql @Sql;
        END
        ELSE
        BEGIN
            -- Some older Accounting tables stored CreatedBy as uniqueidentifier.
            -- Converting to nvarchar(64) preserves the existing GUID text and matches
            -- the shared AuditableEntity/ICurrentUser pipeline.
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ALTER COLUMN [CreatedBy] nvarchar(64) NULL;';

            EXEC sys.sp_executesql @Sql;
        END;

        ----------------------------------------------------------------------
        -- UpdatedAt
        ----------------------------------------------------------------------
        IF COL_LENGTH(@LookupName, N'UpdatedAt') IS NULL
           AND COL_LENGTH(@LookupName, N'LastModifiedAtUtc') IS NOT NULL
        BEGIN
            SET @ColumnName =
                QUOTENAME(N'accounting') + N'.'
                + QUOTENAME(@TableName) + N'.'
                + QUOTENAME(N'LastModifiedAtUtc');

            EXEC sys.sp_rename
                @objname = @ColumnName,
                @newname = N'UpdatedAt',
                @objtype = N'COLUMN';
        END;

        IF COL_LENGTH(@LookupName, N'UpdatedAt') IS NULL
        BEGIN
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ADD [UpdatedAt] datetimeoffset NULL;';

            EXEC sys.sp_executesql @Sql;
        END
        ELSE
        BEGIN
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ALTER COLUMN [UpdatedAt] datetimeoffset NULL;';

            EXEC sys.sp_executesql @Sql;
        END;

        ----------------------------------------------------------------------
        -- UpdatedBy
        ----------------------------------------------------------------------
        IF COL_LENGTH(@LookupName, N'UpdatedBy') IS NULL
           AND COL_LENGTH(@LookupName, N'LastModifiedBy') IS NOT NULL
        BEGIN
            SET @ColumnName =
                QUOTENAME(N'accounting') + N'.'
                + QUOTENAME(@TableName) + N'.'
                + QUOTENAME(N'LastModifiedBy');

            EXEC sys.sp_rename
                @objname = @ColumnName,
                @newname = N'UpdatedBy',
                @objtype = N'COLUMN';
        END;

        IF COL_LENGTH(@LookupName, N'UpdatedBy') IS NULL
        BEGIN
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ADD [UpdatedBy] nvarchar(64) NULL;';

            EXEC sys.sp_executesql @Sql;
        END
        ELSE
        BEGIN
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ALTER COLUMN [UpdatedBy] nvarchar(64) NULL;';

            EXEC sys.sp_executesql @Sql;
        END;

        ----------------------------------------------------------------------
        -- CreatedFromDevice
        ----------------------------------------------------------------------
        IF COL_LENGTH(@LookupName, N'CreatedFromDevice') IS NULL
        BEGIN
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ADD [CreatedFromDevice] nvarchar(256) NULL;';

            EXEC sys.sp_executesql @Sql;
        END
        ELSE
        BEGIN
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ALTER COLUMN [CreatedFromDevice] nvarchar(256) NULL;';

            EXEC sys.sp_executesql @Sql;
        END;

        ----------------------------------------------------------------------
        -- UpdatedFromDevice
        ----------------------------------------------------------------------
        IF COL_LENGTH(@LookupName, N'UpdatedFromDevice') IS NULL
        BEGIN
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ADD [UpdatedFromDevice] nvarchar(256) NULL;';

            EXEC sys.sp_executesql @Sql;
        END
        ELSE
        BEGIN
            SET @Sql =
                N'ALTER TABLE ' + @FullName
                + N' ALTER COLUMN [UpdatedFromDevice] nvarchar(256) NULL;';

            EXEC sys.sp_executesql @Sql;
        END;
    END;

    FETCH NEXT FROM audit_tables INTO @TableName;
END;

CLOSE audit_tables;
DEALLOCATE audit_tables;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
-- tbl_Expenses
IF OBJECT_ID(N'[accounting].[tbl_Expenses]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_Expenses', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_Expenses] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_Expenses', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_Expenses] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_Expenses', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_Expenses] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_Expenses', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_Expenses] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_Expenses', N'CreatedBy') IS NOT NULL
    BEGIN
        UPDATE [accounting].[tbl_Expenses]
           SET [CreatedBy] = N'00000000-0000-0000-0000-000000000000'
         WHERE [CreatedBy] IS NULL OR TRY_CONVERT(uniqueidentifier, [CreatedBy]) IS NULL;
        ALTER TABLE [accounting].[tbl_Expenses] ALTER COLUMN [CreatedBy] uniqueidentifier NOT NULL;
    END;

    IF COL_LENGTH(N'accounting.tbl_Expenses', N'CreatedAt') IS NOT NULL
    BEGIN
        ALTER TABLE [accounting].[tbl_Expenses] ALTER COLUMN [CreatedAt] datetime2(3) NOT NULL;
        EXEC sys.sp_rename N'[accounting].[tbl_Expenses].[CreatedAt]', N'CreatedAtUtc', N'COLUMN';
    END;

END;
-- tbl_ExpenseTypes
IF OBJECT_ID(N'[accounting].[tbl_ExpenseTypes]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_ExpenseTypes', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ExpenseTypes] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_ExpenseTypes', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ExpenseTypes] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_ExpenseTypes', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ExpenseTypes] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_ExpenseTypes', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ExpenseTypes] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_ExpenseTypes', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ExpenseTypes] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_ExpenseTypes', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ExpenseTypes] DROP COLUMN [CreatedAt];

END;
-- tbl_CashShifts
IF OBJECT_ID(N'[accounting].[tbl_CashShifts]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_CashShifts', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashShifts] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_CashShifts', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashShifts] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_CashShifts', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashShifts] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_CashShifts', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashShifts] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_CashShifts', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashShifts] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_CashShifts', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashShifts] DROP COLUMN [CreatedAt];

END;
-- tbl_BankAccounts
IF OBJECT_ID(N'[accounting].[tbl_BankAccounts]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_BankAccounts', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_BankAccounts] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_BankAccounts', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_BankAccounts] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_BankAccounts', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_BankAccounts] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_BankAccounts', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_BankAccounts] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_BankAccounts', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_BankAccounts] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_BankAccounts', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_BankAccounts] DROP COLUMN [CreatedAt];

END;
-- tbl_CashAccounts
IF OBJECT_ID(N'[accounting].[tbl_CashAccounts]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_CashAccounts', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashAccounts] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_CashAccounts', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashAccounts] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_CashAccounts', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashAccounts] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_CashAccounts', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashAccounts] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_CashAccounts', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashAccounts] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_CashAccounts', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CashAccounts] DROP COLUMN [CreatedAt];

END;
-- tbl_PaymentAllocations
IF OBJECT_ID(N'[accounting].[tbl_PaymentAllocations]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_PaymentAllocations', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentAllocations] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_PaymentAllocations', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentAllocations] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_PaymentAllocations', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentAllocations] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_PaymentAllocations', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentAllocations] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_PaymentAllocations', N'CreatedBy') IS NOT NULL
    BEGIN
        UPDATE [accounting].[tbl_PaymentAllocations]
           SET [CreatedBy] = N'00000000-0000-0000-0000-000000000000'
         WHERE [CreatedBy] IS NULL OR TRY_CONVERT(uniqueidentifier, [CreatedBy]) IS NULL;
        ALTER TABLE [accounting].[tbl_PaymentAllocations] ALTER COLUMN [CreatedBy] uniqueidentifier NOT NULL;
    END;

    IF COL_LENGTH(N'accounting.tbl_PaymentAllocations', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentAllocations] DROP COLUMN [CreatedAt];

END;
-- tbl_PaymentVoucherLines
IF OBJECT_ID(N'[accounting].[tbl_PaymentVoucherLines]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_PaymentVoucherLines', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentVoucherLines] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_PaymentVoucherLines', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentVoucherLines] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_PaymentVoucherLines', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentVoucherLines] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_PaymentVoucherLines', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentVoucherLines] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_PaymentVoucherLines', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentVoucherLines] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_PaymentVoucherLines', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentVoucherLines] DROP COLUMN [CreatedAt];

END;
-- tbl_PaymentVouchers
IF OBJECT_ID(N'[accounting].[tbl_PaymentVouchers]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_PaymentVouchers', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentVouchers] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_PaymentVouchers', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentVouchers] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_PaymentVouchers', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentVouchers] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_PaymentVouchers', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PaymentVouchers] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_PaymentVouchers', N'CreatedBy') IS NOT NULL
    BEGIN
        UPDATE [accounting].[tbl_PaymentVouchers]
           SET [CreatedBy] = N'00000000-0000-0000-0000-000000000000'
         WHERE [CreatedBy] IS NULL OR TRY_CONVERT(uniqueidentifier, [CreatedBy]) IS NULL;
        ALTER TABLE [accounting].[tbl_PaymentVouchers] ALTER COLUMN [CreatedBy] uniqueidentifier NOT NULL;
    END;

    IF COL_LENGTH(N'accounting.tbl_PaymentVouchers', N'CreatedAt') IS NOT NULL
    BEGIN
        ALTER TABLE [accounting].[tbl_PaymentVouchers] ALTER COLUMN [CreatedAt] datetime2(3) NOT NULL;
        EXEC sys.sp_rename N'[accounting].[tbl_PaymentVouchers].[CreatedAt]', N'CreatedAtUtc', N'COLUMN';
    END;

END;
-- tbl_ReceiptVoucherLines
IF OBJECT_ID(N'[accounting].[tbl_ReceiptVoucherLines]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_ReceiptVoucherLines', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ReceiptVoucherLines] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_ReceiptVoucherLines', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ReceiptVoucherLines] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_ReceiptVoucherLines', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ReceiptVoucherLines] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_ReceiptVoucherLines', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ReceiptVoucherLines] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_ReceiptVoucherLines', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ReceiptVoucherLines] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_ReceiptVoucherLines', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ReceiptVoucherLines] DROP COLUMN [CreatedAt];

END;
-- tbl_ReceiptVouchers
IF OBJECT_ID(N'[accounting].[tbl_ReceiptVouchers]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_ReceiptVouchers', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ReceiptVouchers] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_ReceiptVouchers', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ReceiptVouchers] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_ReceiptVouchers', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ReceiptVouchers] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_ReceiptVouchers', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_ReceiptVouchers] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_ReceiptVouchers', N'CreatedBy') IS NOT NULL
    BEGIN
        UPDATE [accounting].[tbl_ReceiptVouchers]
           SET [CreatedBy] = N'00000000-0000-0000-0000-000000000000'
         WHERE [CreatedBy] IS NULL OR TRY_CONVERT(uniqueidentifier, [CreatedBy]) IS NULL;
        ALTER TABLE [accounting].[tbl_ReceiptVouchers] ALTER COLUMN [CreatedBy] uniqueidentifier NOT NULL;
    END;

    IF COL_LENGTH(N'accounting.tbl_ReceiptVouchers', N'CreatedAt') IS NOT NULL
    BEGIN
        ALTER TABLE [accounting].[tbl_ReceiptVouchers] ALTER COLUMN [CreatedAt] datetime2(3) NOT NULL;
        EXEC sys.sp_rename N'[accounting].[tbl_ReceiptVouchers].[CreatedAt]', N'CreatedAtUtc', N'COLUMN';
    END;

END;
-- tbl_SupplierAccounts
IF OBJECT_ID(N'[accounting].[tbl_SupplierAccounts]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_SupplierAccounts', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_SupplierAccounts] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_SupplierAccounts', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_SupplierAccounts] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_SupplierAccounts', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_SupplierAccounts] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_SupplierAccounts', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_SupplierAccounts] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_SupplierAccounts', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_SupplierAccounts] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_SupplierAccounts', N'CreatedAt') IS NOT NULL
    BEGIN
        ALTER TABLE [accounting].[tbl_SupplierAccounts] ALTER COLUMN [CreatedAt] datetime2(3) NOT NULL;
        EXEC sys.sp_rename N'[accounting].[tbl_SupplierAccounts].[CreatedAt]', N'CreatedAtUtc', N'COLUMN';
    END;

END;
-- tbl_CustomerAccounts
IF OBJECT_ID(N'[accounting].[tbl_CustomerAccounts]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_CustomerAccounts', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CustomerAccounts] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_CustomerAccounts', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CustomerAccounts] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_CustomerAccounts', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CustomerAccounts] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_CustomerAccounts', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CustomerAccounts] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_CustomerAccounts', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CustomerAccounts] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_CustomerAccounts', N'CreatedAt') IS NOT NULL
    BEGIN
        ALTER TABLE [accounting].[tbl_CustomerAccounts] ALTER COLUMN [CreatedAt] datetime2(3) NOT NULL;
        EXEC sys.sp_rename N'[accounting].[tbl_CustomerAccounts].[CreatedAt]', N'CreatedAtUtc', N'COLUMN';
    END;

END;
-- tbl_CostCenters
IF OBJECT_ID(N'[accounting].[tbl_CostCenters]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_CostCenters', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CostCenters] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_CostCenters', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CostCenters] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_CostCenters', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CostCenters] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_CostCenters', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CostCenters] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_CostCenters', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CostCenters] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_CostCenters', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_CostCenters] DROP COLUMN [CreatedAt];

END;
-- tbl_PostingProfileLines
IF OBJECT_ID(N'[accounting].[tbl_PostingProfileLines]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_PostingProfileLines', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfileLines] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_PostingProfileLines', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfileLines] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_PostingProfileLines', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfileLines] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_PostingProfileLines', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfileLines] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_PostingProfileLines', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfileLines] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_PostingProfileLines', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfileLines] DROP COLUMN [CreatedAt];

END;
-- tbl_PostingProfiles
IF OBJECT_ID(N'[accounting].[tbl_PostingProfiles]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_PostingProfiles', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfiles] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_PostingProfiles', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfiles] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_PostingProfiles', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfiles] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_PostingProfiles', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfiles] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_PostingProfiles', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfiles] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_PostingProfiles', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_PostingProfiles] DROP COLUMN [CreatedAt];

END;
-- tbl_JournalEntryLines
IF OBJECT_ID(N'[accounting].[tbl_JournalEntryLines]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_JournalEntryLines', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_JournalEntryLines] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_JournalEntryLines', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_JournalEntryLines] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_JournalEntryLines', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_JournalEntryLines] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_JournalEntryLines', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_JournalEntryLines] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_JournalEntryLines', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_JournalEntryLines] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_JournalEntryLines', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_JournalEntryLines] DROP COLUMN [CreatedAt];

END;
-- tbl_JournalEntries
IF OBJECT_ID(N'[accounting].[tbl_JournalEntries]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_JournalEntries', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_JournalEntries] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_JournalEntries', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_JournalEntries] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_JournalEntries', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_JournalEntries] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_JournalEntries', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_JournalEntries] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_JournalEntries', N'CreatedBy') IS NOT NULL
    BEGIN
        UPDATE [accounting].[tbl_JournalEntries]
           SET [CreatedBy] = N'00000000-0000-0000-0000-000000000000'
         WHERE [CreatedBy] IS NULL OR TRY_CONVERT(uniqueidentifier, [CreatedBy]) IS NULL;
        ALTER TABLE [accounting].[tbl_JournalEntries] ALTER COLUMN [CreatedBy] uniqueidentifier NOT NULL;
    END;

    IF COL_LENGTH(N'accounting.tbl_JournalEntries', N'CreatedAt') IS NOT NULL
    BEGIN
        ALTER TABLE [accounting].[tbl_JournalEntries] ALTER COLUMN [CreatedAt] datetime2(3) NOT NULL;
        EXEC sys.sp_rename N'[accounting].[tbl_JournalEntries].[CreatedAt]', N'CreatedAtUtc', N'COLUMN';
    END;

END;
-- tbl_FiscalPeriods
IF OBJECT_ID(N'[accounting].[tbl_FiscalPeriods]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_FiscalPeriods', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalPeriods] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_FiscalPeriods', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalPeriods] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_FiscalPeriods', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalPeriods] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_FiscalPeriods', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalPeriods] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_FiscalPeriods', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalPeriods] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_FiscalPeriods', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalPeriods] DROP COLUMN [CreatedAt];

END;
-- tbl_FiscalYears
IF OBJECT_ID(N'[accounting].[tbl_FiscalYears]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_FiscalYears', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalYears] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_FiscalYears', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalYears] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_FiscalYears', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalYears] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_FiscalYears', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalYears] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_FiscalYears', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalYears] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_FiscalYears', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_FiscalYears] DROP COLUMN [CreatedAt];

END;
-- tbl_Accounts
IF OBJECT_ID(N'[accounting].[tbl_Accounts]', N'U') IS NOT NULL
BEGIN

    IF COL_LENGTH(N'accounting.tbl_Accounts', N'UpdatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_Accounts] DROP COLUMN [UpdatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_Accounts', N'CreatedFromDevice') IS NOT NULL
        ALTER TABLE [accounting].[tbl_Accounts] DROP COLUMN [CreatedFromDevice];

    IF COL_LENGTH(N'accounting.tbl_Accounts', N'UpdatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_Accounts] DROP COLUMN [UpdatedBy];

    IF COL_LENGTH(N'accounting.tbl_Accounts', N'UpdatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_Accounts] DROP COLUMN [UpdatedAt];

    IF COL_LENGTH(N'accounting.tbl_Accounts', N'CreatedBy') IS NOT NULL
        ALTER TABLE [accounting].[tbl_Accounts] DROP COLUMN [CreatedBy];

    IF COL_LENGTH(N'accounting.tbl_Accounts', N'CreatedAt') IS NOT NULL
        ALTER TABLE [accounting].[tbl_Accounts] DROP COLUMN [CreatedAt];

END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[accounting].[tbl_Expenses]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[Expenses]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_Expenses]', N'Expenses', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_ExpenseTypes]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[ExpenseTypes]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_ExpenseTypes]', N'ExpenseTypes', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_CashShifts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[CashShifts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_CashShifts]', N'CashShifts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_BankAccounts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[BankAccounts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_BankAccounts]', N'BankAccounts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_CashAccounts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[CashAccounts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_CashAccounts]', N'CashAccounts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_PaymentAllocations]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[PaymentAllocations]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_PaymentAllocations]', N'PaymentAllocations', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_PaymentVoucherLines]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[PaymentVoucherLines]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_PaymentVoucherLines]', N'PaymentVoucherLines', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_PaymentVouchers]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[PaymentVouchers]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_PaymentVouchers]', N'PaymentVouchers', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_ReceiptVoucherLines]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[ReceiptVoucherLines]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_ReceiptVoucherLines]', N'ReceiptVoucherLines', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_ReceiptVouchers]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[ReceiptVouchers]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_ReceiptVouchers]', N'ReceiptVouchers', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_SupplierAccounts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[SupplierAccounts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_SupplierAccounts]', N'SupplierAccounts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_CustomerAccounts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[CustomerAccounts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_CustomerAccounts]', N'CustomerAccounts', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_CostCenters]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[CostCenters]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_CostCenters]', N'CostCenters', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_PostingProfileLines]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[PostingProfileLines]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_PostingProfileLines]', N'PostingProfileLines', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_PostingProfiles]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[PostingProfiles]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_PostingProfiles]', N'PostingProfiles', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_JournalEntryLines]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[JournalEntryLines]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_JournalEntryLines]', N'JournalEntryLines', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_JournalEntries]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[JournalEntries]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_JournalEntries]', N'JournalEntries', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_FiscalPeriods]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[FiscalPeriods]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_FiscalPeriods]', N'FiscalPeriods', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_FiscalYears]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[FiscalYears]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_FiscalYears]', N'FiscalYears', N'OBJECT';
IF OBJECT_ID(N'[accounting].[tbl_Accounts]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[accounting].[Accounts]', N'U') IS NULL
    EXEC sys.sp_rename N'[accounting].[tbl_Accounts]', N'Accounts', N'OBJECT';
""");
    }
}
