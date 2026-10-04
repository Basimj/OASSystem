using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261003113000_AddReturnsCommissionsOpticalProduction")]
public sealed class AddReturnsCommissionsOpticalProduction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* ============================================================
   1) Stable document sequences
   ============================================================ */
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'PurchaseReturnCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[PurchaseReturnCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'SalesReturnCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[SalesReturnCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'CommissionStatementCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[CommissionStatementCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'OpticalProductionJobCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[OpticalProductionJobCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');

/* ============================================================
   2) Return/commission support on existing source documents
   ============================================================ */
IF COL_LENGTH(N'dbo.tbl_SalesInvoices', N'SalesEmployeeId') IS NULL
    ALTER TABLE [dbo].[tbl_SalesInvoices] ADD [SalesEmployeeId] uniqueidentifier NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_SalesEmployeeId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    EXEC(N'CREATE INDEX [IX_SalesInvoices_SalesEmployeeId] ON [dbo].[tbl_SalesInvoices]([SalesEmployeeId]);');

-- If this column was added manually/partially in an older development database,
-- remove orphan values before validating the foreign key.
EXEC(N'UPDATE si
   SET [SalesEmployeeId] = NULL
FROM [dbo].[tbl_SalesInvoices] si
WHERE si.[SalesEmployeeId] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [hr].[Employees] e WHERE e.[Id] = si.[SalesEmployeeId]);');

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalesInvoices_Employees_SalesEmployeeId' AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
BEGIN
    EXEC(N'ALTER TABLE [dbo].[tbl_SalesInvoices] WITH CHECK ADD CONSTRAINT [FK_SalesInvoices_Employees_SalesEmployeeId]
        FOREIGN KEY ([SalesEmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION;
    ALTER TABLE [dbo].[tbl_SalesInvoices] CHECK CONSTRAINT [FK_SalesInvoices_Employees_SalesEmployeeId];');
END;

IF COL_LENGTH(N'dbo.tbl_SalesInvoiceLines', N'ReturnedQuantity') IS NULL
    ALTER TABLE [dbo].[tbl_SalesInvoiceLines] ADD [ReturnedQuantity] decimal(18,3) NOT NULL CONSTRAINT [DF_SalesInvoiceLines_ReturnedQuantity] DEFAULT (0);
ELSE IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
    WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]')
      AND c.[name] = N'ReturnedQuantity'
)
    EXEC(N'ALTER TABLE [dbo].[tbl_SalesInvoiceLines]
        ADD CONSTRAINT [DF_SalesInvoiceLines_ReturnedQuantity] DEFAULT (0) FOR [ReturnedQuantity];');

-- Normalize legacy/manual values before adding the constraint.
EXEC(N'UPDATE [dbo].[tbl_SalesInvoiceLines]
   SET [ReturnedQuantity] = CASE
       WHEN [ReturnedQuantity] < 0 THEN 0
       WHEN [ReturnedQuantity] > [Quantity] THEN [Quantity]
       ELSE [ReturnedQuantity]
   END;');

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_SalesInvoiceLines_ReturnedQuantity_Valid' AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]'))
    EXEC(N'ALTER TABLE [dbo].[tbl_SalesInvoiceLines] WITH CHECK ADD CONSTRAINT [CK_SalesInvoiceLines_ReturnedQuantity_Valid]
        CHECK ([ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]);');

IF COL_LENGTH(N'dbo.tbl_PurchaseReceiptLines', N'ReturnedQuantity') IS NULL
    ALTER TABLE [dbo].[tbl_PurchaseReceiptLines] ADD [ReturnedQuantity] decimal(18,3) NOT NULL CONSTRAINT [DF_PurchaseReceiptLines_ReturnedQuantity] DEFAULT (0);
ELSE IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
    WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[tbl_PurchaseReceiptLines]')
      AND c.[name] = N'ReturnedQuantity'
)
    EXEC(N'ALTER TABLE [dbo].[tbl_PurchaseReceiptLines]
        ADD CONSTRAINT [DF_PurchaseReceiptLines_ReturnedQuantity] DEFAULT (0) FOR [ReturnedQuantity];');

EXEC(N'UPDATE [dbo].[tbl_PurchaseReceiptLines]
   SET [ReturnedQuantity] = CASE
       WHEN [ReturnedQuantity] < 0 THEN 0
       WHEN [ReturnedQuantity] > [AcceptedQuantity] THEN [AcceptedQuantity]
       ELSE [ReturnedQuantity]
   END;');

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PurchaseReceiptLines_ReturnedQuantity_Valid' AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_PurchaseReceiptLines]'))
    EXEC(N'ALTER TABLE [dbo].[tbl_PurchaseReceiptLines] WITH CHECK ADD CONSTRAINT [CK_PurchaseReceiptLines_ReturnedQuantity_Valid]
        CHECK ([ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [AcceptedQuantity]);');

/* ============================================================
   3) Sales returns
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_SalesReturns]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_SalesReturns]
    (
        [Id] uniqueidentifier NOT NULL,
        [ReturnCode] nvarchar(40) NOT NULL,
        [SalesInvoiceId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [ReturnDate] date NOT NULL,
        [PostingDate] date NOT NULL,
        [Status] tinyint NOT NULL,
        [CurrencyId] uniqueidentifier NOT NULL,
        [CurrencyCodeSnapshot] nvarchar(10) NOT NULL,
        [CurrencyDecimalPlacesSnapshot] tinyint NOT NULL,
        [ExchangeRate] decimal(19,8) NOT NULL,
        [ExchangeRateDate] date NOT NULL,
        [ExchangeRateType] tinyint NOT NULL,
        [ExchangeRateSource] tinyint NOT NULL,
        [BaseCurrencyId] uniqueidentifier NOT NULL,
        [BaseCurrencyCodeSnapshot] nvarchar(10) NOT NULL,
        [BaseCurrencyDecimalPlacesSnapshot] tinyint NOT NULL,
        [NetAmount] decimal(19,4) NOT NULL,
        [TaxAmount] decimal(19,4) NOT NULL,
        [TotalAmount] decimal(19,4) NOT NULL,
        [BaseNetAmount] decimal(19,4) NOT NULL,
        [BaseTaxAmount] decimal(19,4) NOT NULL,
        [BaseTotalAmount] decimal(19,4) NOT NULL,
        [Reason] nvarchar(1000) NULL,
        [JournalEntryId] uniqueidentifier NULL,
        [ConfirmedAtUtc] datetimeoffset NULL,
        [ConfirmedBy] nvarchar(64) NULL,
        [PostedAtUtc] datetimeoffset NULL,
        [PostedBy] nvarchar(64) NULL,
        [CancelledAtUtc] datetimeoffset NULL,
        [CancelledBy] nvarchar(64) NULL,
        [CancellationReason] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_SalesReturns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesReturns_ExchangeRate_Positive] CHECK ([ExchangeRate] > 0),
        CONSTRAINT [CK_SalesReturns_Amounts_NonNegative] CHECK ([NetAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0 AND [BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0),
        CONSTRAINT [FK_SalesReturns_SalesInvoices_SalesInvoiceId] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [dbo].[tbl_SalesInvoices]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesReturns_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[tbl_Customers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesReturns_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesReturns_BaseCurrencies_BaseCurrencyId] FOREIGN KEY ([BaseCurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesReturns_JournalEntries_JournalEntryId] FOREIGN KEY ([JournalEntryId]) REFERENCES [dbo].[tbl_JournalEntries]([Id]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SalesReturns_ReturnCode' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesReturns]'))
    CREATE UNIQUE INDEX [UX_SalesReturns_ReturnCode] ON [dbo].[tbl_SalesReturns]([ReturnCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesReturns_SalesInvoiceId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesReturns]'))
    CREATE INDEX [IX_SalesReturns_SalesInvoiceId] ON [dbo].[tbl_SalesReturns]([SalesInvoiceId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesReturns_CustomerId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesReturns]'))
    CREATE INDEX [IX_SalesReturns_CustomerId] ON [dbo].[tbl_SalesReturns]([CustomerId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesReturns_PostingDate' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesReturns]'))
    CREATE INDEX [IX_SalesReturns_PostingDate] ON [dbo].[tbl_SalesReturns]([PostingDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesReturns_Status' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesReturns]'))
    CREATE INDEX [IX_SalesReturns_Status] ON [dbo].[tbl_SalesReturns]([Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesReturns_JournalEntryId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesReturns]'))
    CREATE INDEX [IX_SalesReturns_JournalEntryId] ON [dbo].[tbl_SalesReturns]([JournalEntryId]);

IF OBJECT_ID(N'[dbo].[tbl_SalesReturnLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_SalesReturnLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [SalesReturnId] uniqueidentifier NOT NULL,
        [LineNumber] int NOT NULL,
        [SalesInvoiceLineId] uniqueidentifier NOT NULL,
        [LineType] tinyint NOT NULL,
        [ProductVariantId] uniqueidentifier NULL,
        [WarehouseId] uniqueidentifier NULL,
        [ProductCodeSnapshot] nvarchar(64) NULL,
        [ProductNameSnapshot] nvarchar(250) NOT NULL,
        [Quantity] decimal(19,3) NOT NULL,
        [NetAmount] decimal(19,4) NOT NULL,
        [TaxAmount] decimal(19,4) NOT NULL,
        [FinalAmount] decimal(19,4) NOT NULL,
        [BaseNetAmount] decimal(19,4) NOT NULL,
        [BaseTaxAmount] decimal(19,4) NOT NULL,
        [BaseFinalAmount] decimal(19,4) NOT NULL,
        [UnitCostSnapshot] decimal(19,4) NULL,
        [TotalCostSnapshot] decimal(19,4) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_SalesReturnLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesReturnLines_Quantity_Positive] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_SalesReturnLines_Amounts_NonNegative] CHECK ([NetAmount] >= 0 AND [TaxAmount] >= 0 AND [FinalAmount] >= 0 AND [BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseFinalAmount] >= 0),
        CONSTRAINT [FK_SalesReturnLines_SalesReturns_SalesReturnId] FOREIGN KEY ([SalesReturnId]) REFERENCES [dbo].[tbl_SalesReturns]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesReturnLines_SalesInvoiceLines_SalesInvoiceLineId] FOREIGN KEY ([SalesInvoiceLineId]) REFERENCES [dbo].[tbl_SalesInvoiceLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesReturnLines_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesReturnLines_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[tbl_Warehouses]([Id]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SalesReturnLines_Return_LineNumber' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesReturnLines]'))
    CREATE UNIQUE INDEX [UX_SalesReturnLines_Return_LineNumber] ON [dbo].[tbl_SalesReturnLines]([SalesReturnId],[LineNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesReturnLines_SalesInvoiceLineId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesReturnLines]'))
    CREATE INDEX [IX_SalesReturnLines_SalesInvoiceLineId] ON [dbo].[tbl_SalesReturnLines]([SalesInvoiceLineId]);

/* ============================================================
   4) Purchase returns
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_PurchaseReturns]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseReturns]
    (
        [Id] uniqueidentifier NOT NULL,
        [ReturnCode] nvarchar(40) NOT NULL,
        [PurchaseReceiptId] uniqueidentifier NOT NULL,
        [PurchaseInvoiceId] uniqueidentifier NULL,
        [SupplierId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [ReturnDate] date NOT NULL,
        [PostingDate] date NOT NULL,
        [Status] tinyint NOT NULL,
        [ReceiptCostBaseAmount] decimal(19,4) NOT NULL,
        [SupplierNetBaseAmount] decimal(19,4) NOT NULL,
        [SupplierTaxBaseAmount] decimal(19,4) NOT NULL,
        [SupplierGrossBaseAmount] decimal(19,4) NOT NULL,
        [InventoryCostBaseAmount] decimal(19,4) NOT NULL,
        [PurchasePriceVarianceBaseAmount] decimal(19,4) NOT NULL,
        [Reason] nvarchar(1000) NULL,
        [JournalEntryId] uniqueidentifier NULL,
        [ConfirmedAt] datetimeoffset NULL,
        [ConfirmedBy] nvarchar(64) NULL,
        [PostedAt] datetimeoffset NULL,
        [PostedBy] nvarchar(64) NULL,
        [CancelledAt] datetimeoffset NULL,
        [CancelledBy] nvarchar(64) NULL,
        [CancellationReason] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseReturns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseReturns_Amounts_NonNegative] CHECK ([ReceiptCostBaseAmount] >= 0 AND [SupplierNetBaseAmount] >= 0 AND [SupplierTaxBaseAmount] >= 0 AND [SupplierGrossBaseAmount] >= 0 AND [InventoryCostBaseAmount] >= 0),
        CONSTRAINT [FK_PurchaseReturns_Receipts_PurchaseReceiptId] FOREIGN KEY ([PurchaseReceiptId]) REFERENCES [dbo].[tbl_PurchaseReceipts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReturns_Invoices_PurchaseInvoiceId] FOREIGN KEY ([PurchaseInvoiceId]) REFERENCES [dbo].[tbl_PurchaseInvoices]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReturns_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[tbl_Suppliers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReturns_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[tbl_Warehouses]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReturns_Journals_JournalEntryId] FOREIGN KEY ([JournalEntryId]) REFERENCES [dbo].[tbl_JournalEntries]([Id]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PurchaseReturns_ReturnCode' AND object_id = OBJECT_ID(N'[dbo].[tbl_PurchaseReturns]'))
    CREATE UNIQUE INDEX [UX_PurchaseReturns_ReturnCode] ON [dbo].[tbl_PurchaseReturns]([ReturnCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseReturns_PurchaseReceiptId' AND object_id = OBJECT_ID(N'[dbo].[tbl_PurchaseReturns]'))
    CREATE INDEX [IX_PurchaseReturns_PurchaseReceiptId] ON [dbo].[tbl_PurchaseReturns]([PurchaseReceiptId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseReturns_PurchaseInvoiceId' AND object_id = OBJECT_ID(N'[dbo].[tbl_PurchaseReturns]'))
    CREATE INDEX [IX_PurchaseReturns_PurchaseInvoiceId] ON [dbo].[tbl_PurchaseReturns]([PurchaseInvoiceId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseReturns_SupplierId' AND object_id = OBJECT_ID(N'[dbo].[tbl_PurchaseReturns]'))
    CREATE INDEX [IX_PurchaseReturns_SupplierId] ON [dbo].[tbl_PurchaseReturns]([SupplierId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseReturns_Status' AND object_id = OBJECT_ID(N'[dbo].[tbl_PurchaseReturns]'))
    CREATE INDEX [IX_PurchaseReturns_Status] ON [dbo].[tbl_PurchaseReturns]([Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseReturns_PostingDate' AND object_id = OBJECT_ID(N'[dbo].[tbl_PurchaseReturns]'))
    CREATE INDEX [IX_PurchaseReturns_PostingDate] ON [dbo].[tbl_PurchaseReturns]([PostingDate]);

IF OBJECT_ID(N'[dbo].[tbl_PurchaseReturnLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseReturnLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [PurchaseReturnId] uniqueidentifier NOT NULL,
        [LineNumber] int NOT NULL,
        [PurchaseReceiptLineId] uniqueidentifier NOT NULL,
        [PurchaseInvoiceLineId] uniqueidentifier NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [BaseQuantity] decimal(18,3) NOT NULL,
        [ReceiptUnitCostBase] decimal(19,4) NOT NULL,
        [ReceiptCostBaseAmount] decimal(19,4) NOT NULL,
        [SupplierNetBaseAmount] decimal(19,4) NOT NULL,
        [SupplierTaxBaseAmount] decimal(19,4) NOT NULL,
        [SupplierGrossBaseAmount] decimal(19,4) NOT NULL,
        [InventoryUnitCostBase] decimal(19,4) NULL,
        [InventoryCostBaseAmount] decimal(19,4) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseReturnLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseReturnLines_Quantity_Positive] CHECK ([Quantity] > 0 AND [BaseQuantity] > 0),
        CONSTRAINT [CK_PurchaseReturnLines_Amounts_NonNegative] CHECK ([ReceiptUnitCostBase] >= 0 AND [ReceiptCostBaseAmount] >= 0 AND [SupplierNetBaseAmount] >= 0 AND [SupplierTaxBaseAmount] >= 0 AND [SupplierGrossBaseAmount] >= 0 AND ([InventoryUnitCostBase] IS NULL OR [InventoryUnitCostBase] >= 0) AND ([InventoryCostBaseAmount] IS NULL OR [InventoryCostBaseAmount] >= 0)),
        CONSTRAINT [FK_PurchaseReturnLines_Returns_PurchaseReturnId] FOREIGN KEY ([PurchaseReturnId]) REFERENCES [dbo].[tbl_PurchaseReturns]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReturnLines_ReceiptLines_PurchaseReceiptLineId] FOREIGN KEY ([PurchaseReceiptLineId]) REFERENCES [dbo].[tbl_PurchaseReceiptLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReturnLines_InvoiceLines_PurchaseInvoiceLineId] FOREIGN KEY ([PurchaseInvoiceLineId]) REFERENCES [dbo].[tbl_PurchaseInvoiceLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReturnLines_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PurchaseReturnLines_Return_LineNumber' AND object_id = OBJECT_ID(N'[dbo].[tbl_PurchaseReturnLines]'))
    CREATE UNIQUE INDEX [UX_PurchaseReturnLines_Return_LineNumber] ON [dbo].[tbl_PurchaseReturnLines]([PurchaseReturnId],[LineNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseReturnLines_ReceiptLineId' AND object_id = OBJECT_ID(N'[dbo].[tbl_PurchaseReturnLines]'))
    CREATE INDEX [IX_PurchaseReturnLines_ReceiptLineId] ON [dbo].[tbl_PurchaseReturnLines]([PurchaseReceiptLineId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseReturnLines_InvoiceLineId' AND object_id = OBJECT_ID(N'[dbo].[tbl_PurchaseReturnLines]'))
    CREATE INDEX [IX_PurchaseReturnLines_InvoiceLineId] ON [dbo].[tbl_PurchaseReturnLines]([PurchaseInvoiceLineId]);

/* ============================================================
   5) Commission engine
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_CommissionRules]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_CommissionRules]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(40) NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [EmployeeId] uniqueidentifier NULL,
        [RatePercent] decimal(9,4) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_CommissionRules] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CommissionRules_Rate] CHECK ([RatePercent] >= 0 AND [RatePercent] <= 100),
        CONSTRAINT [FK_CommissionRules_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CommissionRules_Code' AND object_id = OBJECT_ID(N'[dbo].[tbl_CommissionRules]'))
    CREATE UNIQUE INDEX [UX_CommissionRules_Code] ON [dbo].[tbl_CommissionRules]([Code]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CommissionRules_Resolution' AND object_id = OBJECT_ID(N'[dbo].[tbl_CommissionRules]'))
    CREATE INDEX [IX_CommissionRules_Resolution] ON [dbo].[tbl_CommissionRules]([EmployeeId],[EffectiveFrom],[EffectiveTo],[IsActive]);

IF OBJECT_ID(N'[dbo].[tbl_CommissionStatements]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_CommissionStatements]
    (
        [Id] uniqueidentifier NOT NULL,
        [StatementCode] nvarchar(40) NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [FromDate] date NOT NULL,
        [ToDate] date NOT NULL,
        [Status] tinyint NOT NULL,
        [SalesBaseAmount] decimal(19,4) NOT NULL,
        [ReturnsBaseAmount] decimal(19,4) NOT NULL,
        [CommissionBaseAmount] decimal(19,4) NOT NULL,
        [CalculatedAtUtc] datetimeoffset NULL,
        [CalculatedBy] nvarchar(64) NULL,
        [FinalizedAtUtc] datetimeoffset NULL,
        [FinalizedBy] nvarchar(64) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_CommissionStatements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CommissionStatements_Period] CHECK ([ToDate] >= [FromDate]),
        CONSTRAINT [FK_CommissionStatements_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CommissionStatements_StatementCode' AND object_id = OBJECT_ID(N'[dbo].[tbl_CommissionStatements]'))
    CREATE UNIQUE INDEX [UX_CommissionStatements_StatementCode] ON [dbo].[tbl_CommissionStatements]([StatementCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CommissionStatements_Employee_Period' AND object_id = OBJECT_ID(N'[dbo].[tbl_CommissionStatements]'))
    CREATE INDEX [IX_CommissionStatements_Employee_Period] ON [dbo].[tbl_CommissionStatements]([EmployeeId],[FromDate],[ToDate]);

IF OBJECT_ID(N'[dbo].[tbl_CommissionEntries]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_CommissionEntries]
    (
        [Id] uniqueidentifier NOT NULL,
        [CommissionStatementId] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [SourceDocumentType] nvarchar(40) NOT NULL,
        [SourceDocumentId] uniqueidentifier NOT NULL,
        [SourceLineId] uniqueidentifier NOT NULL,
        [SalesInvoiceId] uniqueidentifier NOT NULL,
        [OriginalSalesInvoiceLineId] uniqueidentifier NOT NULL,
        [SalesReturnId] uniqueidentifier NULL,
        [SourceDate] date NOT NULL,
        [BaseSalesAmount] decimal(19,4) NOT NULL,
        [RatePercent] decimal(9,4) NOT NULL,
        [CommissionBaseAmount] decimal(19,4) NOT NULL,
        [CommissionRuleId] uniqueidentifier NOT NULL,
        [RuleCodeSnapshot] nvarchar(40) NOT NULL,
        [RuleNameSnapshot] nvarchar(160) NOT NULL,
        [IsReversal] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        CONSTRAINT [PK_tbl_CommissionEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CommissionEntries_Statements_CommissionStatementId] FOREIGN KEY ([CommissionStatementId]) REFERENCES [dbo].[tbl_CommissionStatements]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CommissionEntries_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CommissionEntries_CommissionRules_CommissionRuleId] FOREIGN KEY ([CommissionRuleId]) REFERENCES [dbo].[tbl_CommissionRules]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CommissionEntries_SalesInvoices_SalesInvoiceId] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [dbo].[tbl_SalesInvoices]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CommissionEntries_SalesReturns_SalesReturnId] FOREIGN KEY ([SalesReturnId]) REFERENCES [dbo].[tbl_SalesReturns]([Id]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CommissionEntries_SourceLine' AND object_id = OBJECT_ID(N'[dbo].[tbl_CommissionEntries]'))
    CREATE UNIQUE INDEX [UX_CommissionEntries_SourceLine] ON [dbo].[tbl_CommissionEntries]([SourceDocumentType],[SourceLineId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CommissionEntries_OriginalSalesInvoiceLineId' AND object_id = OBJECT_ID(N'[dbo].[tbl_CommissionEntries]'))
    CREATE INDEX [IX_CommissionEntries_OriginalSalesInvoiceLineId] ON [dbo].[tbl_CommissionEntries]([OriginalSalesInvoiceLineId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CommissionEntries_EmployeeId' AND object_id = OBJECT_ID(N'[dbo].[tbl_CommissionEntries]'))
    CREATE INDEX [IX_CommissionEntries_EmployeeId] ON [dbo].[tbl_CommissionEntries]([EmployeeId]);

/* ============================================================
   6) Optical production / quality control / breakage / remake
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_OpticalProductionJobs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_OpticalProductionJobs]
    (
        [Id] uniqueidentifier NOT NULL,
        [JobCode] nvarchar(40) NOT NULL,
        [SalesInvoiceId] uniqueidentifier NOT NULL,
        [SalesInvoiceLineId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [JobDate] date NOT NULL,
        [TargetDate] date NULL,
        [Status] tinyint NOT NULL,
        [InventoryTransactionId] uniqueidentifier NULL,
        [MaterialCostBase] decimal(19,4) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [RemakeOfJobId] uniqueidentifier NULL,
        [RemakeNumber] int NOT NULL,
        [LastQcResult] tinyint NULL,
        [QcAttemptCount] int NOT NULL,
        [FailedQcCount] int NOT NULL,
        [LastQcNotes] nvarchar(1000) NULL,
        [LastQcWasBreakage] bit NOT NULL,
        [ReleasedAtUtc] datetimeoffset NULL,
        [StartedAtUtc] datetimeoffset NULL,
        [QcAtUtc] datetimeoffset NULL,
        [FailedAtUtc] datetimeoffset NULL,
        [CompletedAtUtc] datetimeoffset NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_OpticalProductionJobs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OpticalProductionJobs_RemakeNumber] CHECK ([RemakeNumber] >= 0),
        CONSTRAINT [CK_OpticalProductionJobs_QcCounters] CHECK ([QcAttemptCount] >= 0 AND [FailedQcCount] >= 0 AND [FailedQcCount] <= [QcAttemptCount]),
        CONSTRAINT [CK_OpticalProductionJobs_MaterialCost] CHECK ([MaterialCostBase] >= 0),
        CONSTRAINT [FK_OpticalProductionJobs_SalesInvoices_SalesInvoiceId] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [dbo].[tbl_SalesInvoices]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalProductionJobs_SalesInvoiceLines_SalesInvoiceLineId] FOREIGN KEY ([SalesInvoiceLineId]) REFERENCES [dbo].[tbl_SalesInvoiceLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalProductionJobs_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[tbl_Customers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalProductionJobs_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[tbl_Warehouses]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalProductionJobs_InventoryTransactions_InventoryTransactionId] FOREIGN KEY ([InventoryTransactionId]) REFERENCES [dbo].[tbl_InventoryTransactions]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalProductionJobs_RemakeOfJobId] FOREIGN KEY ([RemakeOfJobId]) REFERENCES [dbo].[tbl_OpticalProductionJobs]([Id]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OpticalProductionJobs_JobCode' AND object_id = OBJECT_ID(N'[dbo].[tbl_OpticalProductionJobs]'))
    CREATE UNIQUE INDEX [UX_OpticalProductionJobs_JobCode] ON [dbo].[tbl_OpticalProductionJobs]([JobCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OpticalProductionJobs_ActiveInvoiceLine' AND object_id = OBJECT_ID(N'[dbo].[tbl_OpticalProductionJobs]'))
    CREATE UNIQUE INDEX [UX_OpticalProductionJobs_ActiveInvoiceLine] ON [dbo].[tbl_OpticalProductionJobs]([SalesInvoiceLineId]) WHERE [IsActive] = 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OpticalProductionJobs_Status' AND object_id = OBJECT_ID(N'[dbo].[tbl_OpticalProductionJobs]'))
    CREATE INDEX [IX_OpticalProductionJobs_Status] ON [dbo].[tbl_OpticalProductionJobs]([Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OpticalProductionJobs_RemakeOfJobId' AND object_id = OBJECT_ID(N'[dbo].[tbl_OpticalProductionJobs]'))
    CREATE INDEX [IX_OpticalProductionJobs_RemakeOfJobId] ON [dbo].[tbl_OpticalProductionJobs]([RemakeOfJobId]);

IF OBJECT_ID(N'[dbo].[tbl_OpticalProductionMaterials]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_OpticalProductionMaterials]
    (
        [Id] uniqueidentifier NOT NULL,
        [OpticalProductionJobId] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [UnitCostSnapshot] decimal(19,4) NULL,
        [TotalCostSnapshot] decimal(19,4) NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_tbl_OpticalProductionMaterials] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OpticalProductionMaterials_Qty] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_OpticalProductionMaterials_Jobs_OpticalProductionJobId] FOREIGN KEY ([OpticalProductionJobId]) REFERENCES [dbo].[tbl_OpticalProductionJobs]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalProductionMaterials_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OpticalProductionMaterials_Job_Variant' AND object_id = OBJECT_ID(N'[dbo].[tbl_OpticalProductionMaterials]'))
    CREATE UNIQUE INDEX [UX_OpticalProductionMaterials_Job_Variant] ON [dbo].[tbl_OpticalProductionMaterials]([OpticalProductionJobId],[ProductVariantId]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

IF OBJECT_ID(N'[dbo].[tbl_OpticalProductionMaterials]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_OpticalProductionMaterials];
IF OBJECT_ID(N'[dbo].[tbl_OpticalProductionJobs]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_OpticalProductionJobs];
IF OBJECT_ID(N'[dbo].[tbl_CommissionEntries]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_CommissionEntries];
IF OBJECT_ID(N'[dbo].[tbl_CommissionStatements]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_CommissionStatements];
IF OBJECT_ID(N'[dbo].[tbl_CommissionRules]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_CommissionRules];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseReturnLines]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseReturnLines];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseReturns]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseReturns];
IF OBJECT_ID(N'[dbo].[tbl_SalesReturnLines]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_SalesReturnLines];
IF OBJECT_ID(N'[dbo].[tbl_SalesReturns]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_SalesReturns];

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalesInvoices_Employees_SalesEmployeeId')
    ALTER TABLE [dbo].[tbl_SalesInvoices] DROP CONSTRAINT [FK_SalesInvoices_Employees_SalesEmployeeId];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_SalesEmployeeId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    DROP INDEX [IX_SalesInvoices_SalesEmployeeId] ON [dbo].[tbl_SalesInvoices];
IF COL_LENGTH(N'dbo.tbl_SalesInvoices', N'SalesEmployeeId') IS NOT NULL
    ALTER TABLE [dbo].[tbl_SalesInvoices] DROP COLUMN [SalesEmployeeId];

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_SalesInvoiceLines_ReturnedQuantity_Valid')
    ALTER TABLE [dbo].[tbl_SalesInvoiceLines] DROP CONSTRAINT [CK_SalesInvoiceLines_ReturnedQuantity_Valid];
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_SalesInvoiceLines_ReturnedQuantity')
    ALTER TABLE [dbo].[tbl_SalesInvoiceLines] DROP CONSTRAINT [DF_SalesInvoiceLines_ReturnedQuantity];
IF COL_LENGTH(N'dbo.tbl_SalesInvoiceLines', N'ReturnedQuantity') IS NOT NULL
    ALTER TABLE [dbo].[tbl_SalesInvoiceLines] DROP COLUMN [ReturnedQuantity];

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PurchaseReceiptLines_ReturnedQuantity_Valid')
    ALTER TABLE [dbo].[tbl_PurchaseReceiptLines] DROP CONSTRAINT [CK_PurchaseReceiptLines_ReturnedQuantity_Valid];
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_PurchaseReceiptLines_ReturnedQuantity')
    ALTER TABLE [dbo].[tbl_PurchaseReceiptLines] DROP CONSTRAINT [DF_PurchaseReceiptLines_ReturnedQuantity];
IF COL_LENGTH(N'dbo.tbl_PurchaseReceiptLines', N'ReturnedQuantity') IS NOT NULL
    ALTER TABLE [dbo].[tbl_PurchaseReceiptLines] DROP COLUMN [ReturnedQuantity];

IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'OpticalProductionJobCodeSequence' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[OpticalProductionJobCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'CommissionStatementCodeSequence' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[CommissionStatementCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'SalesReturnCodeSequence' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[SalesReturnCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'PurchaseReturnCodeSequence' AND schema_id = SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[PurchaseReturnCodeSequence];
""");
    }
}
