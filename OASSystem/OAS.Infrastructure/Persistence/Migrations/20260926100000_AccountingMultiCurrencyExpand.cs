using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260926100000_AccountingMultiCurrencyExpand")]
public sealed class AccountingMultiCurrencyExpand : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateReferenceTables(migrationBuilder);
        AddExpandColumns(migrationBuilder);
        AddIndexesForeignKeysAndChecks(migrationBuilder);
    }

    private static void CreateReferenceTables(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* ============================================================
   1) Reference tables
   ============================================================ */

IF OBJECT_ID(N'[dbo].[tbl_Currencies]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_Currencies]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(8) NOT NULL,
        [NameAr] nvarchar(100) NOT NULL,
        [NameEn] nvarchar(100) NULL,
        [Symbol] nvarchar(12) NULL,
        [DecimalPlaces] tinyint NOT NULL,
        [IsActive] bit NOT NULL,

        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,

        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_Currencies]
            PRIMARY KEY ([Id]),

        CONSTRAINT [CK_Currencies_DecimalPlaces]
            CHECK ([DecimalPlaces] BETWEEN 0 AND 6)
    );

    CREATE UNIQUE INDEX [UX_Currencies_Code]
        ON [dbo].[tbl_Currencies]([Code]);

    CREATE INDEX [IX_Currencies_IsActive]
        ON [dbo].[tbl_Currencies]([IsActive]);
END;


IF OBJECT_ID(N'[dbo].[tbl_ExchangeRates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_ExchangeRates]
    (
        [Id] uniqueidentifier NOT NULL,
        [CurrencyId] uniqueidentifier NOT NULL,
        [RateDate] date NOT NULL,
        [Rate] decimal(19,8) NOT NULL,
        [RateType] tinyint NOT NULL,
        [IsActive] bit NOT NULL,

        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,

        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_ExchangeRates]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_ExchangeRates_Currency]
            FOREIGN KEY ([CurrencyId])
            REFERENCES [dbo].[tbl_Currencies]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [CK_ExchangeRates_Rate_Positive]
            CHECK ([Rate] > 0)
    );

    CREATE UNIQUE INDEX [UX_ExchangeRates_Currency_Date_Type]
        ON [dbo].[tbl_ExchangeRates]
        (
            [CurrencyId],
            [RateDate],
            [RateType]
        );
END;


IF OBJECT_ID(N'[dbo].[tbl_AccountingSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_AccountingSettings]
    (
        [Id] uniqueidentifier NOT NULL,
        [BaseCurrencyId] uniqueidentifier NOT NULL,

        [EmployeeParentAccountId] uniqueidentifier NULL,
        [CashParentAccountId] uniqueidentifier NULL,
        [BankParentAccountId] uniqueidentifier NULL,

        [ExchangeGainAccountId] uniqueidentifier NULL,
        [ExchangeLossAccountId] uniqueidentifier NULL,

        [DefaultExchangeRateType] tinyint NOT NULL,

        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,

        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_AccountingSettings]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_AccountingSettings_BaseCurrency]
            FOREIGN KEY ([BaseCurrencyId])
            REFERENCES [dbo].[tbl_Currencies]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_AccountingSettings_EmployeeParentAccount]
            FOREIGN KEY ([EmployeeParentAccountId])
            REFERENCES [dbo].[tbl_Accounts]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_AccountingSettings_CashParentAccount]
            FOREIGN KEY ([CashParentAccountId])
            REFERENCES [dbo].[tbl_Accounts]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_AccountingSettings_BankParentAccount]
            FOREIGN KEY ([BankParentAccountId])
            REFERENCES [dbo].[tbl_Accounts]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_AccountingSettings_ExchangeGainAccount]
            FOREIGN KEY ([ExchangeGainAccountId])
            REFERENCES [dbo].[tbl_Accounts]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_AccountingSettings_ExchangeLossAccount]
            FOREIGN KEY ([ExchangeLossAccountId])
            REFERENCES [dbo].[tbl_Accounts]([Id])
            ON DELETE NO ACTION
    );
END;


IF OBJECT_ID(N'[dbo].[tbl_EmployeeAccounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_EmployeeAccounts]
    (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,

        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,

        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_EmployeeAccounts]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_EmployeeAccounts_Employee]
            FOREIGN KEY ([EmployeeId])
            REFERENCES [hr].[Employees]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_EmployeeAccounts_Account]
            FOREIGN KEY ([AccountId])
            REFERENCES [dbo].[tbl_Accounts]([Id])
            ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [UX_EmployeeAccounts_EmployeeId]
        ON [dbo].[tbl_EmployeeAccounts]([EmployeeId]);

    CREATE UNIQUE INDEX [UX_EmployeeAccounts_AccountId]
        ON [dbo].[tbl_EmployeeAccounts]([AccountId]);
END;
""");
    }

    private static void AddExpandColumns(MigrationBuilder migrationBuilder)
    {
        /*
         * IMPORTANT:
         * Keep this SQL command limited to ADD COLUMN operations.
         *
         * SQL Server compiles a batch before executing it. If CREATE INDEX,
         * FOREIGN KEY or CHECK statements that reference a newly-added column
         * exist in the same batch, SQL Server can raise error 207
         * "Invalid column name".
         *
         * All objects that use the new columns are therefore created in the
         * following migrationBuilder.Sql command.
         */

        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* ============================================================
   2) Cash / Bank currency support
   ============================================================ */

IF COL_LENGTH(N'dbo.tbl_CashAccounts', N'CurrencyId') IS NULL
    ALTER TABLE [dbo].[tbl_CashAccounts]
        ADD [CurrencyId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_BankAccounts', N'CurrencyId') IS NULL
    ALTER TABLE [dbo].[tbl_BankAccounts]
        ADD [CurrencyId] uniqueidentifier NULL;


/* ============================================================
   3) Voucher headers - legacy fields remain during Expand
   ============================================================ */

IF COL_LENGTH(N'dbo.tbl_ReceiptVouchers', N'BaseCurrencyId') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVouchers]
        ADD [BaseCurrencyId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVouchers', N'BaseCurrencyCodeSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVouchers]
        ADD [BaseCurrencyCodeSnapshot] nvarchar(8) NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVouchers', N'BaseCurrencyDecimalPlacesSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVouchers]
        ADD [BaseCurrencyDecimalPlacesSnapshot] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVouchers', N'BaseTotalAmount') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVouchers]
        ADD [BaseTotalAmount] decimal(19,4) NULL;


IF COL_LENGTH(N'dbo.tbl_PaymentVouchers', N'BaseCurrencyId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVouchers]
        ADD [BaseCurrencyId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVouchers', N'BaseCurrencyCodeSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVouchers]
        ADD [BaseCurrencyCodeSnapshot] nvarchar(8) NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVouchers', N'BaseCurrencyDecimalPlacesSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVouchers]
        ADD [BaseCurrencyDecimalPlacesSnapshot] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVouchers', N'BaseTotalAmount') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVouchers]
        ADD [BaseTotalAmount] decimal(19,4) NULL;


/* ============================================================
   4) Receipt settlement-line fields
   ============================================================ */

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'PartyType') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [PartyType] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'CustomerId') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [CustomerId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'SupplierId') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [SupplierId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'EmployeeId') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [EmployeeId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'PartyNameSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [PartyNameSnapshot] nvarchar(200) NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'CounterpartyAccountId') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [CounterpartyAccountId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'PaymentMethod') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [PaymentMethod] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'CashAccountId') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [CashAccountId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'BankAccountId') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [BankAccountId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'SettlementAccountId') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [SettlementAccountId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'CurrencyId') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [CurrencyId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'CurrencyCodeSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [CurrencyCodeSnapshot] nvarchar(8) NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'CurrencySymbolSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [CurrencySymbolSnapshot] nvarchar(12) NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'CurrencyDecimalPlacesSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [CurrencyDecimalPlacesSnapshot] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'ExchangeRate') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [ExchangeRate] decimal(19,8) NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'ExchangeRateDate') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [ExchangeRateDate] date NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'ExchangeRateType') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [ExchangeRateType] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'ExchangeRateSource') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [ExchangeRateSource] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'BaseAmount') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [BaseAmount] decimal(19,4) NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'ReferenceNumber') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [ReferenceNumber] nvarchar(100) NULL;

IF COL_LENGTH(N'dbo.tbl_ReceiptVoucherLines', N'ReferenceDate') IS NULL
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD [ReferenceDate] date NULL;


/* ============================================================
   5) Payment settlement-line fields
   ============================================================ */

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'PartyType') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [PartyType] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'CustomerId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [CustomerId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'SupplierId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [SupplierId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'EmployeeId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [EmployeeId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'PartyNameSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [PartyNameSnapshot] nvarchar(200) NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'CounterpartyAccountId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [CounterpartyAccountId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'PaymentMethod') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [PaymentMethod] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'CashAccountId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [CashAccountId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'BankAccountId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [BankAccountId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'SettlementAccountId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [SettlementAccountId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'CurrencyId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [CurrencyId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'CurrencyCodeSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [CurrencyCodeSnapshot] nvarchar(8) NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'CurrencySymbolSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [CurrencySymbolSnapshot] nvarchar(12) NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'CurrencyDecimalPlacesSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [CurrencyDecimalPlacesSnapshot] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'ExchangeRate') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [ExchangeRate] decimal(19,8) NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'ExchangeRateDate') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [ExchangeRateDate] date NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'ExchangeRateType') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [ExchangeRateType] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'ExchangeRateSource') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [ExchangeRateSource] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'BaseAmount') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [BaseAmount] decimal(19,4) NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'ReferenceNumber') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [ReferenceNumber] nvarchar(100) NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentVoucherLines', N'ReferenceDate') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD [ReferenceDate] date NULL;


/* ============================================================
   6) Journal multi-currency fields
   ============================================================ */

IF COL_LENGTH(N'dbo.tbl_JournalEntries', N'BaseCurrencyId') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntries]
        ADD [BaseCurrencyId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntries', N'BaseCurrencyCodeSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntries]
        ADD [BaseCurrencyCodeSnapshot] nvarchar(8) NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntries', N'BaseCurrencyDecimalPlacesSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntries]
        ADD [BaseCurrencyDecimalPlacesSnapshot] tinyint NULL;


IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'TransactionCurrencyId') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [TransactionCurrencyId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'TransactionCurrencyCodeSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [TransactionCurrencyCodeSnapshot] nvarchar(8) NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'TransactionCurrencyDecimalPlacesSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [TransactionCurrencyDecimalPlacesSnapshot] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'TransactionDebitAmount') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [TransactionDebitAmount] decimal(19,4) NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'TransactionCreditAmount') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [TransactionCreditAmount] decimal(19,4) NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'ExchangeRate') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [ExchangeRate] decimal(19,8) NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'ExchangeRateDate') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [ExchangeRateDate] date NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'ExchangeRateType') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [ExchangeRateType] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'ExchangeRateSource') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [ExchangeRateSource] tinyint NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'SourceDocumentLineId') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [SourceDocumentLineId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'EmployeeId') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [EmployeeId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_JournalEntryLines', N'PartyNameSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD [PartyNameSnapshot] nvarchar(200) NULL;


/* ============================================================
   7) PaymentAllocation line-level source and currency snapshot
   ============================================================ */

IF COL_LENGTH(N'dbo.tbl_PaymentAllocations', N'ReceiptVoucherLineId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        ADD [ReceiptVoucherLineId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentAllocations', N'PaymentVoucherLineId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        ADD [PaymentVoucherLineId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentAllocations', N'CurrencyId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        ADD [CurrencyId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentAllocations', N'CurrencyCodeSnapshot') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        ADD [CurrencyCodeSnapshot] nvarchar(8) NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentAllocations', N'ExchangeRate') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        ADD [ExchangeRate] decimal(19,8) NULL;

IF COL_LENGTH(N'dbo.tbl_PaymentAllocations', N'BaseAllocatedAmount') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        ADD [BaseAllocatedAmount] decimal(19,4) NULL;
""");
    }

    private static void AddIndexesForeignKeysAndChecks(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* ============================================================
   8) Cash / Bank indexes and FKs
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CashAccounts_CurrencyId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CashAccounts]')
)
    CREATE INDEX [IX_CashAccounts_CurrencyId]
        ON [dbo].[tbl_CashAccounts]([CurrencyId]);

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_CashAccounts_Currency'
)
    ALTER TABLE [dbo].[tbl_CashAccounts]
        ADD CONSTRAINT [FK_CashAccounts_Currency]
        FOREIGN KEY ([CurrencyId])
        REFERENCES [dbo].[tbl_Currencies]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UX_CashAccounts_DefaultPerCurrency'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CashAccounts]')
)
    CREATE UNIQUE INDEX [UX_CashAccounts_DefaultPerCurrency]
        ON [dbo].[tbl_CashAccounts]([CurrencyId])
        WHERE [IsDefault] = 1
          AND [IsActive] = 1
          AND [CurrencyId] IS NOT NULL;


IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_BankAccounts_CurrencyId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_BankAccounts]')
)
    CREATE INDEX [IX_BankAccounts_CurrencyId]
        ON [dbo].[tbl_BankAccounts]([CurrencyId]);

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_BankAccounts_Currency'
)
    ALTER TABLE [dbo].[tbl_BankAccounts]
        ADD CONSTRAINT [FK_BankAccounts_Currency]
        FOREIGN KEY ([CurrencyId])
        REFERENCES [dbo].[tbl_Currencies]([Id])
        ON DELETE NO ACTION;


/* ============================================================
   9) Voucher / Journal header FKs and indexes
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ReceiptVouchers_BaseCurrency'
)
    ALTER TABLE [dbo].[tbl_ReceiptVouchers]
        ADD CONSTRAINT [FK_ReceiptVouchers_BaseCurrency]
        FOREIGN KEY ([BaseCurrencyId])
        REFERENCES [dbo].[tbl_Currencies]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ReceiptVouchers_BaseCurrencyId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_ReceiptVouchers]')
)
    CREATE INDEX [IX_ReceiptVouchers_BaseCurrencyId]
        ON [dbo].[tbl_ReceiptVouchers]([BaseCurrencyId]);


IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentVouchers_BaseCurrency'
)
    ALTER TABLE [dbo].[tbl_PaymentVouchers]
        ADD CONSTRAINT [FK_PaymentVouchers_BaseCurrency]
        FOREIGN KEY ([BaseCurrencyId])
        REFERENCES [dbo].[tbl_Currencies]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_PaymentVouchers_BaseCurrencyId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentVouchers]')
)
    CREATE INDEX [IX_PaymentVouchers_BaseCurrencyId]
        ON [dbo].[tbl_PaymentVouchers]([BaseCurrencyId]);


IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_JournalEntries_BaseCurrency'
)
    ALTER TABLE [dbo].[tbl_JournalEntries]
        ADD CONSTRAINT [FK_JournalEntries_BaseCurrency]
        FOREIGN KEY ([BaseCurrencyId])
        REFERENCES [dbo].[tbl_Currencies]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_JournalEntries_BaseCurrencyId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_JournalEntries]')
)
    CREATE INDEX [IX_JournalEntries_BaseCurrencyId]
        ON [dbo].[tbl_JournalEntries]([BaseCurrencyId]);


/* ============================================================
   10) Receipt Voucher Line indexes / FKs
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_ReceiptVoucherLines_CurrencyId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_ReceiptVoucherLines]')
)
    CREATE INDEX [IX_ReceiptVoucherLines_CurrencyId]
        ON [dbo].[tbl_ReceiptVoucherLines]([CurrencyId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_ReceiptVoucherLines_CustomerId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_ReceiptVoucherLines]')
)
    CREATE INDEX [IX_ReceiptVoucherLines_CustomerId]
        ON [dbo].[tbl_ReceiptVoucherLines]([CustomerId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_ReceiptVoucherLines_SupplierId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_ReceiptVoucherLines]')
)
    CREATE INDEX [IX_ReceiptVoucherLines_SupplierId]
        ON [dbo].[tbl_ReceiptVoucherLines]([SupplierId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_ReceiptVoucherLines_EmployeeId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_ReceiptVoucherLines]')
)
    CREATE INDEX [IX_ReceiptVoucherLines_EmployeeId]
        ON [dbo].[tbl_ReceiptVoucherLines]([EmployeeId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_ReceiptVoucherLines_CounterpartyAccountId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_ReceiptVoucherLines]')
)
    CREATE INDEX [IX_ReceiptVoucherLines_CounterpartyAccountId]
        ON [dbo].[tbl_ReceiptVoucherLines]([CounterpartyAccountId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_ReceiptVoucherLines_SettlementAccountId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_ReceiptVoucherLines]')
)
    CREATE INDEX [IX_ReceiptVoucherLines_SettlementAccountId]
        ON [dbo].[tbl_ReceiptVoucherLines]([SettlementAccountId]);


IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ReceiptVoucherLines_Customer'
)
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD CONSTRAINT [FK_ReceiptVoucherLines_Customer]
        FOREIGN KEY ([CustomerId])
        REFERENCES [dbo].[tbl_Customers]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ReceiptVoucherLines_Supplier'
)
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD CONSTRAINT [FK_ReceiptVoucherLines_Supplier]
        FOREIGN KEY ([SupplierId])
        REFERENCES [dbo].[tbl_Suppliers]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ReceiptVoucherLines_Employee'
)
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD CONSTRAINT [FK_ReceiptVoucherLines_Employee]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [hr].[Employees]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ReceiptVoucherLines_CounterpartyAccount'
)
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD CONSTRAINT [FK_ReceiptVoucherLines_CounterpartyAccount]
        FOREIGN KEY ([CounterpartyAccountId])
        REFERENCES [dbo].[tbl_Accounts]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ReceiptVoucherLines_SettlementAccount'
)
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD CONSTRAINT [FK_ReceiptVoucherLines_SettlementAccount]
        FOREIGN KEY ([SettlementAccountId])
        REFERENCES [dbo].[tbl_Accounts]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ReceiptVoucherLines_CashAccount'
)
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD CONSTRAINT [FK_ReceiptVoucherLines_CashAccount]
        FOREIGN KEY ([CashAccountId])
        REFERENCES [dbo].[tbl_CashAccounts]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ReceiptVoucherLines_BankAccount'
)
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD CONSTRAINT [FK_ReceiptVoucherLines_BankAccount]
        FOREIGN KEY ([BankAccountId])
        REFERENCES [dbo].[tbl_BankAccounts]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ReceiptVoucherLines_Currency'
)
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        ADD CONSTRAINT [FK_ReceiptVoucherLines_Currency]
        FOREIGN KEY ([CurrencyId])
        REFERENCES [dbo].[tbl_Currencies]([Id])
        ON DELETE NO ACTION;


/* ============================================================
   11) Payment Voucher Line indexes / FKs
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_PaymentVoucherLines_CurrencyId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentVoucherLines]')
)
    CREATE INDEX [IX_PaymentVoucherLines_CurrencyId]
        ON [dbo].[tbl_PaymentVoucherLines]([CurrencyId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_PaymentVoucherLines_CustomerId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentVoucherLines]')
)
    CREATE INDEX [IX_PaymentVoucherLines_CustomerId]
        ON [dbo].[tbl_PaymentVoucherLines]([CustomerId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_PaymentVoucherLines_SupplierId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentVoucherLines]')
)
    CREATE INDEX [IX_PaymentVoucherLines_SupplierId]
        ON [dbo].[tbl_PaymentVoucherLines]([SupplierId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_PaymentVoucherLines_EmployeeId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentVoucherLines]')
)
    CREATE INDEX [IX_PaymentVoucherLines_EmployeeId]
        ON [dbo].[tbl_PaymentVoucherLines]([EmployeeId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_PaymentVoucherLines_CounterpartyAccountId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentVoucherLines]')
)
    CREATE INDEX [IX_PaymentVoucherLines_CounterpartyAccountId]
        ON [dbo].[tbl_PaymentVoucherLines]([CounterpartyAccountId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_PaymentVoucherLines_SettlementAccountId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentVoucherLines]')
)
    CREATE INDEX [IX_PaymentVoucherLines_SettlementAccountId]
        ON [dbo].[tbl_PaymentVoucherLines]([SettlementAccountId]);


IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentVoucherLines_Customer'
)
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD CONSTRAINT [FK_PaymentVoucherLines_Customer]
        FOREIGN KEY ([CustomerId])
        REFERENCES [dbo].[tbl_Customers]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentVoucherLines_Supplier'
)
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD CONSTRAINT [FK_PaymentVoucherLines_Supplier]
        FOREIGN KEY ([SupplierId])
        REFERENCES [dbo].[tbl_Suppliers]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentVoucherLines_Employee'
)
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD CONSTRAINT [FK_PaymentVoucherLines_Employee]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [hr].[Employees]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentVoucherLines_CounterpartyAccount'
)
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD CONSTRAINT [FK_PaymentVoucherLines_CounterpartyAccount]
        FOREIGN KEY ([CounterpartyAccountId])
        REFERENCES [dbo].[tbl_Accounts]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentVoucherLines_SettlementAccount'
)
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD CONSTRAINT [FK_PaymentVoucherLines_SettlementAccount]
        FOREIGN KEY ([SettlementAccountId])
        REFERENCES [dbo].[tbl_Accounts]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentVoucherLines_CashAccount'
)
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD CONSTRAINT [FK_PaymentVoucherLines_CashAccount]
        FOREIGN KEY ([CashAccountId])
        REFERENCES [dbo].[tbl_CashAccounts]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentVoucherLines_BankAccount'
)
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD CONSTRAINT [FK_PaymentVoucherLines_BankAccount]
        FOREIGN KEY ([BankAccountId])
        REFERENCES [dbo].[tbl_BankAccounts]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentVoucherLines_Currency'
)
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        ADD CONSTRAINT [FK_PaymentVoucherLines_Currency]
        FOREIGN KEY ([CurrencyId])
        REFERENCES [dbo].[tbl_Currencies]([Id])
        ON DELETE NO ACTION;


/* ============================================================
   12) Journal Line indexes / FKs
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_JournalEntryLines_TransactionCurrencyId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]')
)
    CREATE INDEX [IX_JournalEntryLines_TransactionCurrencyId]
        ON [dbo].[tbl_JournalEntryLines]([TransactionCurrencyId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_JournalEntryLines_EmployeeId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]')
)
    CREATE INDEX [IX_JournalEntryLines_EmployeeId]
        ON [dbo].[tbl_JournalEntryLines]([EmployeeId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_JournalEntryLines_SourceDocumentLineId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_JournalEntryLines]')
)
    CREATE INDEX [IX_JournalEntryLines_SourceDocumentLineId]
        ON [dbo].[tbl_JournalEntryLines]([SourceDocumentLineId]);


IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_JournalEntryLines_TransactionCurrency'
)
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD CONSTRAINT [FK_JournalEntryLines_TransactionCurrency]
        FOREIGN KEY ([TransactionCurrencyId])
        REFERENCES [dbo].[tbl_Currencies]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_JournalEntryLines_Employee'
)
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        ADD CONSTRAINT [FK_JournalEntryLines_Employee]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [hr].[Employees]([Id])
        ON DELETE NO ACTION;


/* ============================================================
   13) PaymentAllocation indexes / FKs
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_PaymentAllocations_ReceiptVoucherLineId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]')
)
    CREATE INDEX [IX_PaymentAllocations_ReceiptVoucherLineId]
        ON [dbo].[tbl_PaymentAllocations]([ReceiptVoucherLineId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_PaymentAllocations_PaymentVoucherLineId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]')
)
    CREATE INDEX [IX_PaymentAllocations_PaymentVoucherLineId]
        ON [dbo].[tbl_PaymentAllocations]([PaymentVoucherLineId]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_PaymentAllocations_CurrencyId'
      AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]')
)
    CREATE INDEX [IX_PaymentAllocations_CurrencyId]
        ON [dbo].[tbl_PaymentAllocations]([CurrencyId]);


IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentAllocations_ReceiptVoucherLine'
)
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        ADD CONSTRAINT [FK_PaymentAllocations_ReceiptVoucherLine]
        FOREIGN KEY ([ReceiptVoucherLineId])
        REFERENCES [dbo].[tbl_ReceiptVoucherLines]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentAllocations_PaymentVoucherLine'
)
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        ADD CONSTRAINT [FK_PaymentAllocations_PaymentVoucherLine]
        FOREIGN KEY ([PaymentVoucherLineId])
        REFERENCES [dbo].[tbl_PaymentVoucherLines]([Id])
        ON DELETE NO ACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_PaymentAllocations_Currency'
)
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        ADD CONSTRAINT [FK_PaymentAllocations_Currency]
        FOREIGN KEY ([CurrencyId])
        REFERENCES [dbo].[tbl_Currencies]([Id])
        ON DELETE NO ACTION;


/* ============================================================
   14) Expand-safe check constraints
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_ReceiptVoucherLines_ExchangeRate_Positive'
)
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        WITH CHECK ADD CONSTRAINT [CK_ReceiptVoucherLines_ExchangeRate_Positive]
        CHECK ([ExchangeRate] IS NULL OR [ExchangeRate] > 0);

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_ReceiptVoucherLines_BaseAmount_Positive'
)
    ALTER TABLE [dbo].[tbl_ReceiptVoucherLines]
        WITH CHECK ADD CONSTRAINT [CK_ReceiptVoucherLines_BaseAmount_Positive]
        CHECK ([BaseAmount] IS NULL OR [BaseAmount] > 0);


IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_PaymentVoucherLines_ExchangeRate_Positive'
)
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        WITH CHECK ADD CONSTRAINT [CK_PaymentVoucherLines_ExchangeRate_Positive]
        CHECK ([ExchangeRate] IS NULL OR [ExchangeRate] > 0);

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_PaymentVoucherLines_BaseAmount_Positive'
)
    ALTER TABLE [dbo].[tbl_PaymentVoucherLines]
        WITH CHECK ADD CONSTRAINT [CK_PaymentVoucherLines_BaseAmount_Positive]
        CHECK ([BaseAmount] IS NULL OR [BaseAmount] > 0);


IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_JournalEntryLines_ExchangeRate'
)
    ALTER TABLE [dbo].[tbl_JournalEntryLines]
        WITH CHECK ADD CONSTRAINT [CK_JournalEntryLines_ExchangeRate]
        CHECK ([ExchangeRate] IS NULL OR [ExchangeRate] > 0);


IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_PaymentAllocations_BaseAmount_Positive'
)
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        WITH CHECK ADD CONSTRAINT [CK_PaymentAllocations_BaseAmount_Positive]
        CHECK
        (
            [BaseAllocatedAmount] IS NULL
            OR [BaseAllocatedAmount] > 0
        );

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_PaymentAllocations_ExchangeRate_Positive'
)
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        WITH CHECK ADD CONSTRAINT [CK_PaymentAllocations_ExchangeRate_Positive]
        CHECK
        (
            [ExchangeRate] IS NULL
            OR [ExchangeRate] > 0
        );

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_PaymentAllocations_TypedSource'
)
    ALTER TABLE [dbo].[tbl_PaymentAllocations]
        WITH CHECK ADD CONSTRAINT [CK_PaymentAllocations_TypedSource]
        CHECK
        (
            [ReceiptVoucherLineId] IS NULL
            OR [PaymentVoucherLineId] IS NULL
        );
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        /*
         * Expand migration is intentionally non-destructive.
         *
         * Historical financial data can depend on the newly-added tables and
         * columns. Removing them automatically from Down could destroy data.
         *
         * A rollback must therefore be a deliberate DBA operation after
         * verifying that no multi-currency documents or mappings exist.
         */
    }
}
