using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260917080000_AccountingInitial")]
public sealed class AccountingInitial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Schema
        migrationBuilder.Sql("""
IF SCHEMA_ID(N'accounting') IS NULL
    EXEC(N'CREATE SCHEMA [accounting]');
""");

        // 2. Tables
        migrationBuilder.Sql("""
-- 2.1 Accounts
IF OBJECT_ID(N'[accounting].[Accounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[Accounts]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [NameAr] nvarchar(150) NOT NULL,
        [NameEn] nvarchar(150) NULL,
        [ParentAccountId] uniqueidentifier NULL,
        [Level] tinyint NOT NULL,
        [AccountClass] tinyint NOT NULL,
        [AccountType] tinyint NOT NULL,
        [NormalBalance] tinyint NOT NULL,
        [IsPostingAccount] bit NOT NULL,
        [IsControlAccount] bit NOT NULL,
        [AllowManualPosting] bit NOT NULL,
        [IsSystemAccount] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [EffectiveDate] date NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Accounts_ParentAccount] FOREIGN KEY ([ParentAccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.2 FiscalYears
IF OBJECT_ID(N'[accounting].[FiscalYears]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[FiscalYears]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Status] tinyint NOT NULL,
        [ClosedAtUtc] datetime2(3) NULL,
        [ClosedBy] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_FiscalYears] PRIMARY KEY ([Id])
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.3 FiscalPeriods
IF OBJECT_ID(N'[accounting].[FiscalPeriods]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[FiscalPeriods]
    (
        [Id] uniqueidentifier NOT NULL,
        [FiscalYearId] uniqueidentifier NOT NULL,
        [PeriodNumber] tinyint NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Status] tinyint NOT NULL,
        [SalesLocked] bit NOT NULL,
        [InventoryLocked] bit NOT NULL,
        [AccountingLocked] bit NOT NULL,
        [ClosedAtUtc] datetime2(3) NULL,
        [ClosedBy] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_FiscalPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FiscalPeriods_FiscalYear] FOREIGN KEY ([FiscalYearId])
            REFERENCES [accounting].[FiscalYears]([Id]) ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.8 CostCenters
IF OBJECT_ID(N'[accounting].[CostCenters]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[CostCenters]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [NameAr] nvarchar(150) NOT NULL,
        [NameEn] nvarchar(150) NULL,
        [ParentCostCenterId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_CostCenters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CostCenters_ParentCostCenter] FOREIGN KEY ([ParentCostCenterId])
            REFERENCES [accounting].[CostCenters]([Id]) ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.4 JournalEntries
IF OBJECT_ID(N'[accounting].[JournalEntries]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[JournalEntries]
    (
        [Id] uniqueidentifier NOT NULL,
        [JournalNumber] nvarchar(40) NOT NULL,
        [JournalType] tinyint NOT NULL,
        [PostingDate] date NOT NULL,
        [DocumentDate] date NOT NULL,
        [FiscalPeriodId] uniqueidentifier NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [SourceModule] nvarchar(50) NULL,
        [SourceDocumentType] nvarchar(50) NULL,
        [SourceDocumentId] uniqueidentifier NULL,
        [Status] tinyint NOT NULL,
        [ReversedJournalId] uniqueidentifier NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [ApprovedBy] uniqueidentifier NULL,
        [ApprovedAtUtc] datetime2(3) NULL,
        [PostedBy] uniqueidentifier NULL,
        [PostedAtUtc] datetime2(3) NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_JournalEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JournalEntries_FiscalPeriod] FOREIGN KEY ([FiscalPeriodId])
            REFERENCES [accounting].[FiscalPeriods]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JournalEntries_ReversedJournal] FOREIGN KEY ([ReversedJournalId])
            REFERENCES [accounting].[JournalEntries]([Id]) ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.5 JournalEntryLines
IF OBJECT_ID(N'[accounting].[JournalEntryLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[JournalEntryLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [JournalEntryId] uniqueidentifier NOT NULL,
        [LineNumber] int NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [DebitAmount] decimal(19,4) NOT NULL,
        [CreditAmount] decimal(19,4) NOT NULL,
        [Description] nvarchar(300) NULL,
        [CustomerId] uniqueidentifier NULL,
        [SupplierId] uniqueidentifier NULL,
        [CostCenterId] uniqueidentifier NULL,
        [ProductVariantId] uniqueidentifier NULL,
        [WarehouseId] uniqueidentifier NULL,

        CONSTRAINT [PK_JournalEntryLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JournalEntryLines_JournalEntry] FOREIGN KEY ([JournalEntryId])
            REFERENCES [accounting].[JournalEntries]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_JournalEntryLines_Account] FOREIGN KEY ([AccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JournalEntryLines_CostCenter] FOREIGN KEY ([CostCenterId])
            REFERENCES [accounting].[CostCenters]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_JournalEntryLines_DebitAmount] CHECK ([DebitAmount] >= 0),
        CONSTRAINT [CK_JournalEntryLines_CreditAmount] CHECK ([CreditAmount] >= 0),
        CONSTRAINT [CK_JournalEntryLines_DebitOrCredit] CHECK (
            ([DebitAmount] > 0 AND [CreditAmount] = 0) OR
            ([CreditAmount] > 0 AND [DebitAmount] = 0)
        )
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.6 PostingProfiles
IF OBJECT_ID(N'[accounting].[PostingProfiles]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[PostingProfiles]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(40) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Module] nvarchar(50) NOT NULL,
        [DocumentType] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_PostingProfiles] PRIMARY KEY ([Id])
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.7 PostingProfileLines
IF OBJECT_ID(N'[accounting].[PostingProfileLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[PostingProfileLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [PostingProfileId] uniqueidentifier NOT NULL,
        [AccountRole] nvarchar(50) NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [IsRequired] bit NOT NULL,

        CONSTRAINT [PK_PostingProfileLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PostingProfileLines_PostingProfile] FOREIGN KEY ([PostingProfileId])
            REFERENCES [accounting].[PostingProfiles]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PostingProfileLines_Account] FOREIGN KEY ([AccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.16 CashAccounts
IF OBJECT_ID(N'[accounting].[CashAccounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[CashAccounts]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_CashAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CashAccounts_Account] FOREIGN KEY ([AccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.17 BankAccounts
IF OBJECT_ID(N'[accounting].[BankAccounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[BankAccounts]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [BankName] nvarchar(150) NOT NULL,
        [AccountName] nvarchar(150) NOT NULL,
        [AccountNumber] nvarchar(100) NOT NULL,
        [IBAN] nvarchar(50) NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_BankAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BankAccounts_Account] FOREIGN KEY ([AccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.9 CustomerAccounts
IF OBJECT_ID(N'[accounting].[CustomerAccounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[CustomerAccounts]
    (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [ControlAccountId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_CustomerAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerAccounts_Account] FOREIGN KEY ([AccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerAccounts_ControlAccount] FOREIGN KEY ([ControlAccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.10 SupplierAccounts
IF OBJECT_ID(N'[accounting].[SupplierAccounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[SupplierAccounts]
    (
        [Id] uniqueidentifier NOT NULL,
        [SupplierId] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [ControlAccountId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_SupplierAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplierAccounts_Account] FOREIGN KEY ([AccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplierAccounts_ControlAccount] FOREIGN KEY ([ControlAccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.11 ReceiptVouchers
IF OBJECT_ID(N'[accounting].[ReceiptVouchers]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[ReceiptVouchers]
    (
        [Id] uniqueidentifier NOT NULL,
        [VoucherNumber] nvarchar(40) NOT NULL,
        [VoucherDate] date NOT NULL,
        [PartyType] tinyint NOT NULL,
        [CustomerId] uniqueidentifier NULL,
        [ReceivedFrom] nvarchar(200) NULL,
        [PaymentMethod] tinyint NOT NULL,
        [CashAccountId] uniqueidentifier NULL,
        [BankAccountId] uniqueidentifier NULL,
        [TotalAmount] decimal(19,4) NOT NULL,
        [Status] tinyint NOT NULL,
        [Description] nvarchar(500) NULL,
        [JournalEntryId] uniqueidentifier NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [PostedBy] uniqueidentifier NULL,
        [PostedAtUtc] datetime2(3) NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_ReceiptVouchers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReceiptVouchers_CashAccount] FOREIGN KEY ([CashAccountId])
            REFERENCES [accounting].[CashAccounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReceiptVouchers_BankAccount] FOREIGN KEY ([BankAccountId])
            REFERENCES [accounting].[BankAccounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReceiptVouchers_JournalEntry] FOREIGN KEY ([JournalEntryId])
            REFERENCES [accounting].[JournalEntries]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_ReceiptVouchers_TotalAmount] CHECK ([TotalAmount] > 0)
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.12 ReceiptVoucherLines
IF OBJECT_ID(N'[accounting].[ReceiptVoucherLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[ReceiptVoucherLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [ReceiptVoucherId] uniqueidentifier NOT NULL,
        [LineNumber] int NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [Amount] decimal(19,4) NOT NULL,
        [ReferenceType] nvarchar(50) NULL,
        [ReferenceId] uniqueidentifier NULL,
        [Description] nvarchar(300) NULL,

        CONSTRAINT [PK_ReceiptVoucherLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReceiptVoucherLines_ReceiptVoucher] FOREIGN KEY ([ReceiptVoucherId])
            REFERENCES [accounting].[ReceiptVouchers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ReceiptVoucherLines_Account] FOREIGN KEY ([AccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_ReceiptVoucherLines_Amount] CHECK ([Amount] > 0)
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.13 PaymentVouchers
IF OBJECT_ID(N'[accounting].[PaymentVouchers]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[PaymentVouchers]
    (
        [Id] uniqueidentifier NOT NULL,
        [VoucherNumber] nvarchar(40) NOT NULL,
        [VoucherDate] date NOT NULL,
        [PartyType] tinyint NOT NULL,
        [SupplierId] uniqueidentifier NULL,
        [BeneficiaryName] nvarchar(200) NULL,
        [PaymentMethod] tinyint NOT NULL,
        [CashAccountId] uniqueidentifier NULL,
        [BankAccountId] uniqueidentifier NULL,
        [TotalAmount] decimal(19,4) NOT NULL,
        [Status] tinyint NOT NULL,
        [Description] nvarchar(500) NULL,
        [JournalEntryId] uniqueidentifier NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [PostedBy] uniqueidentifier NULL,
        [PostedAtUtc] datetime2(3) NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_PaymentVouchers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentVouchers_CashAccount] FOREIGN KEY ([CashAccountId])
            REFERENCES [accounting].[CashAccounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentVouchers_BankAccount] FOREIGN KEY ([BankAccountId])
            REFERENCES [accounting].[BankAccounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentVouchers_JournalEntry] FOREIGN KEY ([JournalEntryId])
            REFERENCES [accounting].[JournalEntries]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_PaymentVouchers_TotalAmount] CHECK ([TotalAmount] > 0)
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.14 PaymentVoucherLines
IF OBJECT_ID(N'[accounting].[PaymentVoucherLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[PaymentVoucherLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [PaymentVoucherId] uniqueidentifier NOT NULL,
        [LineNumber] int NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [Amount] decimal(19,4) NOT NULL,
        [ReferenceType] nvarchar(50) NULL,
        [ReferenceId] uniqueidentifier NULL,
        [Description] nvarchar(300) NULL,

        CONSTRAINT [PK_PaymentVoucherLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentVoucherLines_PaymentVoucher] FOREIGN KEY ([PaymentVoucherId])
            REFERENCES [accounting].[PaymentVouchers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PaymentVoucherLines_Account] FOREIGN KEY ([AccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_PaymentVoucherLines_Amount] CHECK ([Amount] > 0)
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.15 PaymentAllocations
IF OBJECT_ID(N'[accounting].[PaymentAllocations]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[PaymentAllocations]
    (
        [Id] uniqueidentifier NOT NULL,
        [PaymentSourceType] tinyint NOT NULL,
        [PaymentSourceId] uniqueidentifier NOT NULL,
        [TargetDocumentType] tinyint NOT NULL,
        [TargetDocumentId] uniqueidentifier NOT NULL,
        [AllocatedAmount] decimal(19,4) NOT NULL,
        [AllocatedAtUtc] datetime2(3) NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,

        CONSTRAINT [PK_PaymentAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PaymentAllocations_AllocatedAmount] CHECK ([AllocatedAmount] > 0)
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.18 CashShifts
IF OBJECT_ID(N'[accounting].[CashShifts]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[CashShifts]
    (
        [Id] uniqueidentifier NOT NULL,
        [ShiftNumber] nvarchar(40) NOT NULL,
        [CashAccountId] uniqueidentifier NOT NULL,
        [OpenedBy] uniqueidentifier NOT NULL,
        [OpenedAtUtc] datetime2(3) NOT NULL,
        [OpeningBalance] decimal(19,4) NOT NULL,
        [ExpectedClosingBalance] decimal(19,4) NULL,
        [ActualClosingBalance] decimal(19,4) NULL,
        [DifferenceAmount] decimal(19,4) NULL,
        [ClosedBy] uniqueidentifier NULL,
        [ClosedAtUtc] datetime2(3) NULL,
        [Status] tinyint NOT NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_CashShifts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CashShifts_CashAccount] FOREIGN KEY ([CashAccountId])
            REFERENCES [accounting].[CashAccounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_CashShifts_OpeningBalance] CHECK ([OpeningBalance] >= 0)
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.19 ExpenseTypes
IF OBJECT_ID(N'[accounting].[ExpenseTypes]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[ExpenseTypes]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [NameAr] nvarchar(150) NOT NULL,
        [NameEn] nvarchar(150) NULL,
        [DefaultExpenseAccountId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_ExpenseTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExpenseTypes_DefaultExpenseAccount] FOREIGN KEY ([DefaultExpenseAccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
-- 2.20 Expenses
IF OBJECT_ID(N'[accounting].[Expenses]', N'U') IS NULL
BEGIN
    CREATE TABLE [accounting].[Expenses]
    (
        [Id] uniqueidentifier NOT NULL,
        [ExpenseNumber] nvarchar(40) NOT NULL,
        [ExpenseDate] date NOT NULL,
        [ExpenseTypeId] uniqueidentifier NOT NULL,
        [ExpenseAccountId] uniqueidentifier NOT NULL,
        [Beneficiary] nvarchar(200) NULL,
        [Amount] decimal(19,4) NOT NULL,
        [PaymentMethod] tinyint NOT NULL,
        [CashAccountId] uniqueidentifier NULL,
        [BankAccountId] uniqueidentifier NULL,
        [Description] nvarchar(500) NULL,
        [Status] tinyint NOT NULL,
        [JournalEntryId] uniqueidentifier NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [PostedAtUtc] datetime2(3) NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_Expenses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Expenses_ExpenseType] FOREIGN KEY ([ExpenseTypeId])
            REFERENCES [accounting].[ExpenseTypes]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Expenses_ExpenseAccount] FOREIGN KEY ([ExpenseAccountId])
            REFERENCES [accounting].[Accounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Expenses_CashAccount] FOREIGN KEY ([CashAccountId])
            REFERENCES [accounting].[CashAccounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Expenses_BankAccount] FOREIGN KEY ([BankAccountId])
            REFERENCES [accounting].[BankAccounts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Expenses_JournalEntry] FOREIGN KEY ([JournalEntryId])
            REFERENCES [accounting].[JournalEntries]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_Expenses_Amount] CHECK ([Amount] > 0)
    );
END;
""");

        // 3. Unique & Performance Indexes
        migrationBuilder.Sql("""
-- Accounts indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Accounts_Code' AND object_id = OBJECT_ID(N'[accounting].[Accounts]'))
    CREATE UNIQUE INDEX [UX_Accounts_Code] ON [accounting].[Accounts]([Code]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Accounts_ParentAccountId' AND object_id = OBJECT_ID(N'[accounting].[Accounts]'))
    CREATE INDEX [IX_Accounts_ParentAccountId] ON [accounting].[Accounts]([ParentAccountId]);

-- FiscalYears indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FiscalYears_Code' AND object_id = OBJECT_ID(N'[accounting].[FiscalYears]'))
    CREATE UNIQUE INDEX [UX_FiscalYears_Code] ON [accounting].[FiscalYears]([Code]);

-- FiscalPeriods indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FiscalPeriods_FiscalYearId_PeriodNumber' AND object_id = OBJECT_ID(N'[accounting].[FiscalPeriods]'))
    CREATE UNIQUE INDEX [UX_FiscalPeriods_FiscalYearId_PeriodNumber] ON [accounting].[FiscalPeriods]([FiscalYearId], [PeriodNumber]);

-- CostCenters indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CostCenters_Code' AND object_id = OBJECT_ID(N'[accounting].[CostCenters]'))
    CREATE UNIQUE INDEX [UX_CostCenters_Code] ON [accounting].[CostCenters]([Code]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CostCenters_ParentCostCenterId' AND object_id = OBJECT_ID(N'[accounting].[CostCenters]'))
    CREATE INDEX [IX_CostCenters_ParentCostCenterId] ON [accounting].[CostCenters]([ParentCostCenterId]);

-- JournalEntries indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_JournalEntries_JournalNumber' AND object_id = OBJECT_ID(N'[accounting].[JournalEntries]'))
    CREATE UNIQUE INDEX [UX_JournalEntries_JournalNumber] ON [accounting].[JournalEntries]([JournalNumber]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JournalEntries_FiscalPeriodId' AND object_id = OBJECT_ID(N'[accounting].[JournalEntries]'))
    CREATE INDEX [IX_JournalEntries_FiscalPeriodId] ON [accounting].[JournalEntries]([FiscalPeriodId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JournalEntries_PostingDate' AND object_id = OBJECT_ID(N'[accounting].[JournalEntries]'))
    CREATE INDEX [IX_JournalEntries_PostingDate] ON [accounting].[JournalEntries]([PostingDate]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JournalEntries_Status' AND object_id = OBJECT_ID(N'[accounting].[JournalEntries]'))
    CREATE INDEX [IX_JournalEntries_Status] ON [accounting].[JournalEntries]([Status]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JournalEntries_ReversedJournalId' AND object_id = OBJECT_ID(N'[accounting].[JournalEntries]'))
    CREATE INDEX [IX_JournalEntries_ReversedJournalId] ON [accounting].[JournalEntries]([ReversedJournalId]);

-- JournalEntryLines indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JournalEntryLines_JournalEntryId' AND object_id = OBJECT_ID(N'[accounting].[JournalEntryLines]'))
    CREATE INDEX [IX_JournalEntryLines_JournalEntryId] ON [accounting].[JournalEntryLines]([JournalEntryId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JournalEntryLines_AccountId' AND object_id = OBJECT_ID(N'[accounting].[JournalEntryLines]'))
    CREATE INDEX [IX_JournalEntryLines_AccountId] ON [accounting].[JournalEntryLines]([AccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JournalEntryLines_CostCenterId' AND object_id = OBJECT_ID(N'[accounting].[JournalEntryLines]'))
    CREATE INDEX [IX_JournalEntryLines_CostCenterId] ON [accounting].[JournalEntryLines]([CostCenterId]);

-- PostingProfiles indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PostingProfiles_Code' AND object_id = OBJECT_ID(N'[accounting].[PostingProfiles]'))
    CREATE UNIQUE INDEX [UX_PostingProfiles_Code] ON [accounting].[PostingProfiles]([Code]);

-- PostingProfileLines indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PostingProfileLines_PostingProfileId' AND object_id = OBJECT_ID(N'[accounting].[PostingProfileLines]'))
    CREATE INDEX [IX_PostingProfileLines_PostingProfileId] ON [accounting].[PostingProfileLines]([PostingProfileId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PostingProfileLines_AccountId' AND object_id = OBJECT_ID(N'[accounting].[PostingProfileLines]'))
    CREATE INDEX [IX_PostingProfileLines_AccountId] ON [accounting].[PostingProfileLines]([AccountId]);

-- CashAccounts indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CashAccounts_Code' AND object_id = OBJECT_ID(N'[accounting].[CashAccounts]'))
    CREATE UNIQUE INDEX [UX_CashAccounts_Code] ON [accounting].[CashAccounts]([Code]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CashAccounts_AccountId' AND object_id = OBJECT_ID(N'[accounting].[CashAccounts]'))
    CREATE INDEX [IX_CashAccounts_AccountId] ON [accounting].[CashAccounts]([AccountId]);

-- BankAccounts indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BankAccounts_Code' AND object_id = OBJECT_ID(N'[accounting].[BankAccounts]'))
    CREATE UNIQUE INDEX [UX_BankAccounts_Code] ON [accounting].[BankAccounts]([Code]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BankAccounts_AccountId' AND object_id = OBJECT_ID(N'[accounting].[BankAccounts]'))
    CREATE INDEX [IX_BankAccounts_AccountId] ON [accounting].[BankAccounts]([AccountId]);

-- CustomerAccounts indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CustomerAccounts_CustomerId' AND object_id = OBJECT_ID(N'[accounting].[CustomerAccounts]'))
    CREATE UNIQUE INDEX [UX_CustomerAccounts_CustomerId] ON [accounting].[CustomerAccounts]([CustomerId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerAccounts_AccountId' AND object_id = OBJECT_ID(N'[accounting].[CustomerAccounts]'))
    CREATE INDEX [IX_CustomerAccounts_AccountId] ON [accounting].[CustomerAccounts]([AccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerAccounts_ControlAccountId' AND object_id = OBJECT_ID(N'[accounting].[CustomerAccounts]'))
    CREATE INDEX [IX_CustomerAccounts_ControlAccountId] ON [accounting].[CustomerAccounts]([ControlAccountId]);

-- SupplierAccounts indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SupplierAccounts_SupplierId' AND object_id = OBJECT_ID(N'[accounting].[SupplierAccounts]'))
    CREATE UNIQUE INDEX [UX_SupplierAccounts_SupplierId] ON [accounting].[SupplierAccounts]([SupplierId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SupplierAccounts_AccountId' AND object_id = OBJECT_ID(N'[accounting].[SupplierAccounts]'))
    CREATE INDEX [IX_SupplierAccounts_AccountId] ON [accounting].[SupplierAccounts]([AccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SupplierAccounts_ControlAccountId' AND object_id = OBJECT_ID(N'[accounting].[SupplierAccounts]'))
    CREATE INDEX [IX_SupplierAccounts_ControlAccountId] ON [accounting].[SupplierAccounts]([ControlAccountId]);

-- ReceiptVouchers indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ReceiptVouchers_VoucherNumber' AND object_id = OBJECT_ID(N'[accounting].[ReceiptVouchers]'))
    CREATE UNIQUE INDEX [UX_ReceiptVouchers_VoucherNumber] ON [accounting].[ReceiptVouchers]([VoucherNumber]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptVouchers_VoucherDate' AND object_id = OBJECT_ID(N'[accounting].[ReceiptVouchers]'))
    CREATE INDEX [IX_ReceiptVouchers_VoucherDate] ON [accounting].[ReceiptVouchers]([VoucherDate]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptVouchers_CustomerId' AND object_id = OBJECT_ID(N'[accounting].[ReceiptVouchers]'))
    CREATE INDEX [IX_ReceiptVouchers_CustomerId] ON [accounting].[ReceiptVouchers]([CustomerId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptVouchers_CashAccountId' AND object_id = OBJECT_ID(N'[accounting].[ReceiptVouchers]'))
    CREATE INDEX [IX_ReceiptVouchers_CashAccountId] ON [accounting].[ReceiptVouchers]([CashAccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptVouchers_BankAccountId' AND object_id = OBJECT_ID(N'[accounting].[ReceiptVouchers]'))
    CREATE INDEX [IX_ReceiptVouchers_BankAccountId] ON [accounting].[ReceiptVouchers]([BankAccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptVouchers_JournalEntryId' AND object_id = OBJECT_ID(N'[accounting].[ReceiptVouchers]'))
    CREATE INDEX [IX_ReceiptVouchers_JournalEntryId] ON [accounting].[ReceiptVouchers]([JournalEntryId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptVouchers_Status' AND object_id = OBJECT_ID(N'[accounting].[ReceiptVouchers]'))
    CREATE INDEX [IX_ReceiptVouchers_Status] ON [accounting].[ReceiptVouchers]([Status]);

-- ReceiptVoucherLines indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptVoucherLines_ReceiptVoucherId' AND object_id = OBJECT_ID(N'[accounting].[ReceiptVoucherLines]'))
    CREATE INDEX [IX_ReceiptVoucherLines_ReceiptVoucherId] ON [accounting].[ReceiptVoucherLines]([ReceiptVoucherId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptVoucherLines_AccountId' AND object_id = OBJECT_ID(N'[accounting].[ReceiptVoucherLines]'))
    CREATE INDEX [IX_ReceiptVoucherLines_AccountId] ON [accounting].[ReceiptVoucherLines]([AccountId]);

-- PaymentVouchers indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PaymentVouchers_VoucherNumber' AND object_id = OBJECT_ID(N'[accounting].[PaymentVouchers]'))
    CREATE UNIQUE INDEX [UX_PaymentVouchers_VoucherNumber] ON [accounting].[PaymentVouchers]([VoucherNumber]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentVouchers_VoucherDate' AND object_id = OBJECT_ID(N'[accounting].[PaymentVouchers]'))
    CREATE INDEX [IX_PaymentVouchers_VoucherDate] ON [accounting].[PaymentVouchers]([VoucherDate]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentVouchers_SupplierId' AND object_id = OBJECT_ID(N'[accounting].[PaymentVouchers]'))
    CREATE INDEX [IX_PaymentVouchers_SupplierId] ON [accounting].[PaymentVouchers]([SupplierId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentVouchers_CashAccountId' AND object_id = OBJECT_ID(N'[accounting].[PaymentVouchers]'))
    CREATE INDEX [IX_PaymentVouchers_CashAccountId] ON [accounting].[PaymentVouchers]([CashAccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentVouchers_BankAccountId' AND object_id = OBJECT_ID(N'[accounting].[PaymentVouchers]'))
    CREATE INDEX [IX_PaymentVouchers_BankAccountId] ON [accounting].[PaymentVouchers]([BankAccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentVouchers_JournalEntryId' AND object_id = OBJECT_ID(N'[accounting].[PaymentVouchers]'))
    CREATE INDEX [IX_PaymentVouchers_JournalEntryId] ON [accounting].[PaymentVouchers]([JournalEntryId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentVouchers_Status' AND object_id = OBJECT_ID(N'[accounting].[PaymentVouchers]'))
    CREATE INDEX [IX_PaymentVouchers_Status] ON [accounting].[PaymentVouchers]([Status]);

-- PaymentVoucherLines indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentVoucherLines_PaymentVoucherId' AND object_id = OBJECT_ID(N'[accounting].[PaymentVoucherLines]'))
    CREATE INDEX [IX_PaymentVoucherLines_PaymentVoucherId] ON [accounting].[PaymentVoucherLines]([PaymentVoucherId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentVoucherLines_AccountId' AND object_id = OBJECT_ID(N'[accounting].[PaymentVoucherLines]'))
    CREATE INDEX [IX_PaymentVoucherLines_AccountId] ON [accounting].[PaymentVoucherLines]([AccountId]);

-- PaymentAllocations indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentAllocations_PaymentSourceId' AND object_id = OBJECT_ID(N'[accounting].[PaymentAllocations]'))
    CREATE INDEX [IX_PaymentAllocations_PaymentSourceId] ON [accounting].[PaymentAllocations]([PaymentSourceId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentAllocations_TargetDocumentId' AND object_id = OBJECT_ID(N'[accounting].[PaymentAllocations]'))
    CREATE INDEX [IX_PaymentAllocations_TargetDocumentId] ON [accounting].[PaymentAllocations]([TargetDocumentId]);

-- CashShifts indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CashShifts_ShiftNumber' AND object_id = OBJECT_ID(N'[accounting].[CashShifts]'))
    CREATE UNIQUE INDEX [UX_CashShifts_ShiftNumber] ON [accounting].[CashShifts]([ShiftNumber]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CashShifts_CashAccountId' AND object_id = OBJECT_ID(N'[accounting].[CashShifts]'))
    CREATE INDEX [IX_CashShifts_CashAccountId] ON [accounting].[CashShifts]([CashAccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CashShifts_OpenedAtUtc' AND object_id = OBJECT_ID(N'[accounting].[CashShifts]'))
    CREATE INDEX [IX_CashShifts_OpenedAtUtc] ON [accounting].[CashShifts]([OpenedAtUtc]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CashShifts_Status' AND object_id = OBJECT_ID(N'[accounting].[CashShifts]'))
    CREATE INDEX [IX_CashShifts_Status] ON [accounting].[CashShifts]([Status]);

-- ExpenseTypes indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ExpenseTypes_Code' AND object_id = OBJECT_ID(N'[accounting].[ExpenseTypes]'))
    CREATE UNIQUE INDEX [UX_ExpenseTypes_Code] ON [accounting].[ExpenseTypes]([Code]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ExpenseTypes_DefaultExpenseAccountId' AND object_id = OBJECT_ID(N'[accounting].[ExpenseTypes]'))
    CREATE INDEX [IX_ExpenseTypes_DefaultExpenseAccountId] ON [accounting].[ExpenseTypes]([DefaultExpenseAccountId]);

-- Expenses indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Expenses_ExpenseNumber' AND object_id = OBJECT_ID(N'[accounting].[Expenses]'))
    CREATE UNIQUE INDEX [UX_Expenses_ExpenseNumber] ON [accounting].[Expenses]([ExpenseNumber]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_ExpenseDate' AND object_id = OBJECT_ID(N'[accounting].[Expenses]'))
    CREATE INDEX [IX_Expenses_ExpenseDate] ON [accounting].[Expenses]([ExpenseDate]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_ExpenseTypeId' AND object_id = OBJECT_ID(N'[accounting].[Expenses]'))
    CREATE INDEX [IX_Expenses_ExpenseTypeId] ON [accounting].[Expenses]([ExpenseTypeId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_ExpenseAccountId' AND object_id = OBJECT_ID(N'[accounting].[Expenses]'))
    CREATE INDEX [IX_Expenses_ExpenseAccountId] ON [accounting].[Expenses]([ExpenseAccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_CashAccountId' AND object_id = OBJECT_ID(N'[accounting].[Expenses]'))
    CREATE INDEX [IX_Expenses_CashAccountId] ON [accounting].[Expenses]([CashAccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_BankAccountId' AND object_id = OBJECT_ID(N'[accounting].[Expenses]'))
    CREATE INDEX [IX_Expenses_BankAccountId] ON [accounting].[Expenses]([BankAccountId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_JournalEntryId' AND object_id = OBJECT_ID(N'[accounting].[Expenses]'))
    CREATE INDEX [IX_Expenses_JournalEntryId] ON [accounting].[Expenses]([JournalEntryId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_Status' AND object_id = OBJECT_ID(N'[accounting].[Expenses]'))
    CREATE INDEX [IX_Expenses_Status] ON [accounting].[Expenses]([Status]);
""");

        // 4. Initial Sequences for 2026
        migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_JournalEntry_2026' AND schema_id = SCHEMA_ID(N'accounting'))
BEGIN
    EXEC(N'CREATE SEQUENCE [accounting].[Seq_JournalEntry_2026] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
END;

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_ReceiptVoucher_2026' AND schema_id = SCHEMA_ID(N'accounting'))
BEGIN
    EXEC(N'CREATE SEQUENCE [accounting].[Seq_ReceiptVoucher_2026] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
END;

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_PaymentVoucher_2026' AND schema_id = SCHEMA_ID(N'accounting'))
BEGIN
    EXEC(N'CREATE SEQUENCE [accounting].[Seq_PaymentVoucher_2026] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
END;

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_Expense_2026' AND schema_id = SCHEMA_ID(N'accounting'))
BEGIN
    EXEC(N'CREATE SEQUENCE [accounting].[Seq_Expense_2026] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
END;

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_CashShift_2026' AND schema_id = SCHEMA_ID(N'accounting'))
BEGIN
    EXEC(N'CREATE SEQUENCE [accounting].[Seq_CashShift_2026] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop sequences
        migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_JournalEntry_2026' AND schema_id = SCHEMA_ID(N'accounting'))
    DROP SEQUENCE [accounting].[Seq_JournalEntry_2026];

IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_ReceiptVoucher_2026' AND schema_id = SCHEMA_ID(N'accounting'))
    DROP SEQUENCE [accounting].[Seq_ReceiptVoucher_2026];

IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_PaymentVoucher_2026' AND schema_id = SCHEMA_ID(N'accounting'))
    DROP SEQUENCE [accounting].[Seq_PaymentVoucher_2026];

IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_Expense_2026' AND schema_id = SCHEMA_ID(N'accounting'))
    DROP SEQUENCE [accounting].[Seq_Expense_2026];

IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'Seq_CashShift_2026' AND schema_id = SCHEMA_ID(N'accounting'))
    DROP SEQUENCE [accounting].[Seq_CashShift_2026];
""");

        // Drop tables in reverse dependency order
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[accounting].[Expenses]', N'U') IS NOT NULL
    DROP TABLE [accounting].[Expenses];

IF OBJECT_ID(N'[accounting].[ExpenseTypes]', N'U') IS NOT NULL
    DROP TABLE [accounting].[ExpenseTypes];

IF OBJECT_ID(N'[accounting].[CashShifts]', N'U') IS NOT NULL
    DROP TABLE [accounting].[CashShifts];

IF OBJECT_ID(N'[accounting].[PaymentAllocations]', N'U') IS NOT NULL
    DROP TABLE [accounting].[PaymentAllocations];

IF OBJECT_ID(N'[accounting].[PaymentVoucherLines]', N'U') IS NOT NULL
    DROP TABLE [accounting].[PaymentVoucherLines];

IF OBJECT_ID(N'[accounting].[PaymentVouchers]', N'U') IS NOT NULL
    DROP TABLE [accounting].[PaymentVouchers];

IF OBJECT_ID(N'[accounting].[ReceiptVoucherLines]', N'U') IS NOT NULL
    DROP TABLE [accounting].[ReceiptVoucherLines];

IF OBJECT_ID(N'[accounting].[ReceiptVouchers]', N'U') IS NOT NULL
    DROP TABLE [accounting].[ReceiptVouchers];

IF OBJECT_ID(N'[accounting].[SupplierAccounts]', N'U') IS NOT NULL
    DROP TABLE [accounting].[SupplierAccounts];

IF OBJECT_ID(N'[accounting].[CustomerAccounts]', N'U') IS NOT NULL
    DROP TABLE [accounting].[CustomerAccounts];

IF OBJECT_ID(N'[accounting].[BankAccounts]', N'U') IS NOT NULL
    DROP TABLE [accounting].[BankAccounts];

IF OBJECT_ID(N'[accounting].[CashAccounts]', N'U') IS NOT NULL
    DROP TABLE [accounting].[CashAccounts];

IF OBJECT_ID(N'[accounting].[PostingProfileLines]', N'U') IS NOT NULL
    DROP TABLE [accounting].[PostingProfileLines];

IF OBJECT_ID(N'[accounting].[PostingProfiles]', N'U') IS NOT NULL
    DROP TABLE [accounting].[PostingProfiles];

IF OBJECT_ID(N'[accounting].[JournalEntryLines]', N'U') IS NOT NULL
    DROP TABLE [accounting].[JournalEntryLines];

IF OBJECT_ID(N'[accounting].[JournalEntries]', N'U') IS NOT NULL
    DROP TABLE [accounting].[JournalEntries];

IF OBJECT_ID(N'[accounting].[CostCenters]', N'U') IS NOT NULL
    DROP TABLE [accounting].[CostCenters];

IF OBJECT_ID(N'[accounting].[FiscalPeriods]', N'U') IS NOT NULL
    DROP TABLE [accounting].[FiscalPeriods];

IF OBJECT_ID(N'[accounting].[FiscalYears]', N'U') IS NOT NULL
    DROP TABLE [accounting].[FiscalYears];

IF OBJECT_ID(N'[accounting].[Accounts]', N'U') IS NOT NULL
    DROP TABLE [accounting].[Accounts];
""");
    }
}
