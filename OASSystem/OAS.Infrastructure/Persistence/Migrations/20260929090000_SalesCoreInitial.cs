using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260929090000_SalesCoreInitial")]
public sealed class SalesCoreInitial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* ============================================================
   1) Sales document sequences
   Sequence gaps are intentional and reserved codes are not reused.
   ============================================================ */
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'PrescriptionCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[PrescriptionCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'CustomerOrderCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[CustomerOrderCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'SalesInvoiceCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[SalesInvoiceCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');

/* ============================================================
   2) Prescriptions
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_Prescriptions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_Prescriptions]
    (
        [Id] uniqueidentifier NOT NULL,
        [PrescriptionCode] nvarchar(40) NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [PrescriptionDate] date NOT NULL,
        [Status] tinyint NOT NULL,
        [PrescribedBy] nvarchar(150) NULL,
        [ClinicName] nvarchar(150) NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_Prescriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Prescriptions_Customers_CustomerId]
            FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[tbl_Customers]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Prescriptions_PrescriptionCode' AND object_id = OBJECT_ID(N'[dbo].[tbl_Prescriptions]'))
    CREATE UNIQUE INDEX [UX_Prescriptions_PrescriptionCode] ON [dbo].[tbl_Prescriptions]([PrescriptionCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Prescriptions_CustomerId' AND object_id = OBJECT_ID(N'[dbo].[tbl_Prescriptions]'))
    CREATE INDEX [IX_Prescriptions_CustomerId] ON [dbo].[tbl_Prescriptions]([CustomerId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Prescriptions_PrescriptionDate' AND object_id = OBJECT_ID(N'[dbo].[tbl_Prescriptions]'))
    CREATE INDEX [IX_Prescriptions_PrescriptionDate] ON [dbo].[tbl_Prescriptions]([PrescriptionDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Prescriptions_Status' AND object_id = OBJECT_ID(N'[dbo].[tbl_Prescriptions]'))
    CREATE INDEX [IX_Prescriptions_Status] ON [dbo].[tbl_Prescriptions]([Status]);

IF OBJECT_ID(N'[dbo].[tbl_PrescriptionRevisions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PrescriptionRevisions]
    (
        [Id] uniqueidentifier NOT NULL,
        [PrescriptionId] uniqueidentifier NOT NULL,
        [RevisionNumber] int NOT NULL,
        [EffectiveDate] date NOT NULL,
        [Reason] nvarchar(500) NULL,
        [IsCurrent] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PrescriptionRevisions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PrescriptionRevisions_RevisionNumber_Positive] CHECK ([RevisionNumber] > 0),
        CONSTRAINT [FK_PrescriptionRevisions_Prescriptions_PrescriptionId]
            FOREIGN KEY ([PrescriptionId]) REFERENCES [dbo].[tbl_Prescriptions]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PrescriptionRevisions_Prescription_Revision' AND object_id = OBJECT_ID(N'[dbo].[tbl_PrescriptionRevisions]'))
    CREATE UNIQUE INDEX [UX_PrescriptionRevisions_Prescription_Revision]
        ON [dbo].[tbl_PrescriptionRevisions]([PrescriptionId], [RevisionNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PrescriptionRevisions_Current' AND object_id = OBJECT_ID(N'[dbo].[tbl_PrescriptionRevisions]'))
    CREATE UNIQUE INDEX [UX_PrescriptionRevisions_Current]
        ON [dbo].[tbl_PrescriptionRevisions]([PrescriptionId]) WHERE [IsCurrent] = 1;

IF OBJECT_ID(N'[dbo].[tbl_PrescriptionEyeDetails]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PrescriptionEyeDetails]
    (
        [Id] uniqueidentifier NOT NULL,
        [PrescriptionRevisionId] uniqueidentifier NOT NULL,
        [Eye] tinyint NOT NULL,
        [SPH] decimal(6,2) NULL,
        [CYL] decimal(6,2) NULL,
        [Axis] smallint NULL,
        [ADD] decimal(6,2) NULL,
        [Prism] decimal(6,2) NULL,
        [PrismBase] tinyint NULL,
        [PD] decimal(6,2) NULL,
        [MonocularPD] decimal(6,2) NULL,
        [VA] nvarchar(20) NULL,
        [FittingHeight] decimal(6,2) NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PrescriptionEyeDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PrescriptionEyeDetails_Axis] CHECK ([Axis] IS NULL OR ([Axis] >= 0 AND [Axis] <= 180)),
        CONSTRAINT [CK_PrescriptionEyeDetails_ADD] CHECK ([ADD] IS NULL OR [ADD] >= 0),
        CONSTRAINT [CK_PrescriptionEyeDetails_Prism] CHECK ([Prism] IS NULL OR [Prism] >= 0),
        CONSTRAINT [CK_PrescriptionEyeDetails_PD] CHECK ([PD] IS NULL OR [PD] > 0),
        CONSTRAINT [CK_PrescriptionEyeDetails_MonocularPD] CHECK ([MonocularPD] IS NULL OR [MonocularPD] > 0),
        CONSTRAINT [CK_PrescriptionEyeDetails_FittingHeight] CHECK ([FittingHeight] IS NULL OR [FittingHeight] > 0),
        CONSTRAINT [FK_PrescriptionEyeDetails_Revisions_PrescriptionRevisionId]
            FOREIGN KEY ([PrescriptionRevisionId]) REFERENCES [dbo].[tbl_PrescriptionRevisions]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PrescriptionEyeDetails_Revision_Eye' AND object_id = OBJECT_ID(N'[dbo].[tbl_PrescriptionEyeDetails]'))
    CREATE UNIQUE INDEX [UX_PrescriptionEyeDetails_Revision_Eye]
        ON [dbo].[tbl_PrescriptionEyeDetails]([PrescriptionRevisionId], [Eye]);

/* ============================================================
   3) Customer orders
   Exchange-rate fields are included because TASK-SALES-01 later
   requires Order and Invoice to preserve the applied rate snapshot.
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_CustomerOrders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_CustomerOrders]
    (
        [Id] uniqueidentifier NOT NULL,
        [OrderCode] nvarchar(40) NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [PrescriptionRevisionId] uniqueidentifier NULL,
        [OrderDate] date NOT NULL,
        [RequiredDate] date NULL,
        [Status] tinyint NOT NULL,
        [CurrencyId] uniqueidentifier NOT NULL,
        [CurrencyCodeSnapshot] nvarchar(10) NOT NULL,
        [CurrencySymbolSnapshot] nvarchar(10) NULL,
        [CurrencyDecimalPlacesSnapshot] tinyint NOT NULL,
        [ExchangeRate] decimal(19,8) NOT NULL,
        [ExchangeRateDate] date NOT NULL,
        [ExchangeRateType] tinyint NOT NULL,
        [ExchangeRateSource] tinyint NOT NULL,
        [TaxCalculationMode] tinyint NOT NULL,
        [PaymentTermType] tinyint NOT NULL,
        [PaymentTermDaysSnapshot] int NOT NULL,
        [Subtotal] decimal(19,4) NOT NULL,
        [DiscountAmount] decimal(19,4) NOT NULL,
        [TaxAmount] decimal(19,4) NOT NULL,
        [TotalAmount] decimal(19,4) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [ConfirmedAtUtc] datetimeoffset NULL,
        [ConfirmedBy] nvarchar(64) NULL,
        [CancelledAtUtc] datetimeoffset NULL,
        [CancelledBy] nvarchar(64) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_CustomerOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerOrders_ExchangeRate_Positive] CHECK ([ExchangeRate] > 0),
        CONSTRAINT [CK_CustomerOrders_PaymentTermDays_NonNegative] CHECK ([PaymentTermDaysSnapshot] >= 0),
        CONSTRAINT [CK_CustomerOrders_Totals_NonNegative] CHECK ([Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0),
        CONSTRAINT [FK_CustomerOrders_Customers_CustomerId]
            FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[tbl_Customers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerOrders_PrescriptionRevisions_PrescriptionRevisionId]
            FOREIGN KEY ([PrescriptionRevisionId]) REFERENCES [dbo].[tbl_PrescriptionRevisions]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerOrders_Currencies_CurrencyId]
            FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CustomerOrders_OrderCode' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrders]'))
    CREATE UNIQUE INDEX [UX_CustomerOrders_OrderCode] ON [dbo].[tbl_CustomerOrders]([OrderCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerOrders_CustomerId' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrders]'))
    CREATE INDEX [IX_CustomerOrders_CustomerId] ON [dbo].[tbl_CustomerOrders]([CustomerId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerOrders_OrderDate' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrders]'))
    CREATE INDEX [IX_CustomerOrders_OrderDate] ON [dbo].[tbl_CustomerOrders]([OrderDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerOrders_Status' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrders]'))
    CREATE INDEX [IX_CustomerOrders_Status] ON [dbo].[tbl_CustomerOrders]([Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerOrders_PrescriptionRevisionId' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrders]'))
    CREATE INDEX [IX_CustomerOrders_PrescriptionRevisionId] ON [dbo].[tbl_CustomerOrders]([PrescriptionRevisionId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerOrders_CurrencyId' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrders]'))
    CREATE INDEX [IX_CustomerOrders_CurrencyId] ON [dbo].[tbl_CustomerOrders]([CurrencyId]);

IF OBJECT_ID(N'[dbo].[tbl_CustomerOrderLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_CustomerOrderLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [CustomerOrderId] uniqueidentifier NOT NULL,
        [LineNumber] int NOT NULL,
        [GroupId] uniqueidentifier NULL,
        [LineType] tinyint NOT NULL,
        [ProductVariantId] uniqueidentifier NULL,
        [WarehouseId] uniqueidentifier NULL,
        [DescriptionSnapshot] nvarchar(500) NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [BaseUnitPrice] decimal(19,4) NOT NULL,
        [ActualUnitPrice] decimal(19,4) NOT NULL,
        [DiscountType] tinyint NOT NULL,
        [DiscountValue] decimal(19,4) NULL,
        [DiscountAmount] decimal(19,4) NOT NULL,
        [TaxRate] decimal(9,4) NULL,
        [TaxAmount] decimal(19,4) NOT NULL,
        [NetAmount] decimal(19,4) NOT NULL,
        [FinalAmount] decimal(19,4) NOT NULL,
        [PrescriptionRevisionId] uniqueidentifier NULL,
        [PrescriptionEye] tinyint NULL,
        [RequiresProduction] bit NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_CustomerOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerOrderLines_Quantity_Positive] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_CustomerOrderLines_BaseUnitPrice_NonNegative] CHECK ([BaseUnitPrice] >= 0),
        CONSTRAINT [CK_CustomerOrderLines_ActualUnitPrice_NonNegative] CHECK ([ActualUnitPrice] >= 0),
        CONSTRAINT [CK_CustomerOrderLines_DiscountAmount_NonNegative] CHECK ([DiscountAmount] >= 0),
        CONSTRAINT [CK_CustomerOrderLines_DiscountWithinGross] CHECK ([DiscountAmount] <= ([Quantity] * [ActualUnitPrice])),
        CONSTRAINT [CK_CustomerOrderLines_TaxAmount_NonNegative] CHECK ([TaxAmount] >= 0),
        CONSTRAINT [CK_CustomerOrderLines_NetAmount_NonNegative] CHECK ([NetAmount] >= 0),
        CONSTRAINT [CK_CustomerOrderLines_FinalAmount_NonNegative] CHECK ([FinalAmount] >= 0),
        CONSTRAINT [FK_CustomerOrderLines_Orders_CustomerOrderId]
            FOREIGN KEY ([CustomerOrderId]) REFERENCES [dbo].[tbl_CustomerOrders]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerOrderLines_ProductVariants_ProductVariantId]
            FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerOrderLines_Warehouses_WarehouseId]
            FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[tbl_Warehouses]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerOrderLines_PrescriptionRevisions_PrescriptionRevisionId]
            FOREIGN KEY ([PrescriptionRevisionId]) REFERENCES [dbo].[tbl_PrescriptionRevisions]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CustomerOrderLines_Order_LineNumber' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrderLines]'))
    CREATE UNIQUE INDEX [UX_CustomerOrderLines_Order_LineNumber] ON [dbo].[tbl_CustomerOrderLines]([CustomerOrderId], [LineNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerOrderLines_ProductVariantId' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrderLines]'))
    CREATE INDEX [IX_CustomerOrderLines_ProductVariantId] ON [dbo].[tbl_CustomerOrderLines]([ProductVariantId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerOrderLines_WarehouseId' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrderLines]'))
    CREATE INDEX [IX_CustomerOrderLines_WarehouseId] ON [dbo].[tbl_CustomerOrderLines]([WarehouseId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerOrderLines_PrescriptionRevisionId' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrderLines]'))
    CREATE INDEX [IX_CustomerOrderLines_PrescriptionRevisionId] ON [dbo].[tbl_CustomerOrderLines]([PrescriptionRevisionId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerOrderLines_GroupId' AND object_id = OBJECT_ID(N'[dbo].[tbl_CustomerOrderLines]'))
    CREATE INDEX [IX_CustomerOrderLines_GroupId] ON [dbo].[tbl_CustomerOrderLines]([GroupId]);

/* ============================================================
   4) Inventory stock reservations (Inventory-owned)
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_StockReservations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_StockReservations]
    (
        [Id] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [SourceModule] nvarchar(50) NOT NULL,
        [SourceDocumentType] nvarchar(50) NOT NULL,
        [SourceDocumentId] uniqueidentifier NOT NULL,
        [SourceLineId] uniqueidentifier NOT NULL,
        [Status] tinyint NOT NULL,
        [ReservedAtUtc] datetimeoffset NOT NULL,
        [ReleasedAtUtc] datetimeoffset NULL,
        [ConsumedAtUtc] datetimeoffset NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_StockReservations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockReservations_Quantity_Positive] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_StockReservations_ProductVariants_ProductVariantId]
            FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockReservations_Warehouses_WarehouseId]
            FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[tbl_Warehouses]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockReservations_Warehouse_Product_Status' AND object_id = OBJECT_ID(N'[dbo].[tbl_StockReservations]'))
    CREATE INDEX [IX_StockReservations_Warehouse_Product_Status]
        ON [dbo].[tbl_StockReservations]([WarehouseId], [ProductVariantId], [Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockReservations_Source' AND object_id = OBJECT_ID(N'[dbo].[tbl_StockReservations]'))
    CREATE INDEX [IX_StockReservations_Source]
        ON [dbo].[tbl_StockReservations]([SourceModule], [SourceDocumentType], [SourceDocumentId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockReservations_SourceLineId' AND object_id = OBJECT_ID(N'[dbo].[tbl_StockReservations]'))
    CREATE INDEX [IX_StockReservations_SourceLineId]
        ON [dbo].[tbl_StockReservations]([SourceLineId]);

/* ============================================================
   5) Sales invoices
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_SalesInvoices]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_SalesInvoices]
    (
        [Id] uniqueidentifier NOT NULL,
        [InvoiceCode] nvarchar(40) NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [CustomerOrderId] uniqueidentifier NULL,
        [PrescriptionRevisionId] uniqueidentifier NULL,
        [InvoiceDate] date NOT NULL,
        [PostingDate] date NOT NULL,
        [Status] tinyint NOT NULL,
        [CurrencyId] uniqueidentifier NOT NULL,
        [CurrencyCodeSnapshot] nvarchar(10) NOT NULL,
        [CurrencySymbolSnapshot] nvarchar(10) NULL,
        [CurrencyDecimalPlacesSnapshot] tinyint NOT NULL,
        [ExchangeRate] decimal(19,8) NOT NULL,
        [ExchangeRateDate] date NOT NULL,
        [ExchangeRateType] tinyint NOT NULL,
        [ExchangeRateSource] tinyint NOT NULL,
        [TaxCalculationMode] tinyint NOT NULL,
        [PaymentTermType] tinyint NOT NULL,
        [PaymentTermDaysSnapshot] int NOT NULL,
        [DueDate] date NULL,
        [BaseCurrencyId] uniqueidentifier NOT NULL,
        [BaseCurrencyCodeSnapshot] nvarchar(10) NOT NULL,
        [BaseCurrencyDecimalPlacesSnapshot] tinyint NOT NULL,
        [Subtotal] decimal(19,4) NOT NULL,
        [DiscountAmount] decimal(19,4) NOT NULL,
        [TaxAmount] decimal(19,4) NOT NULL,
        [TotalAmount] decimal(19,4) NOT NULL,
        [BaseSubtotal] decimal(19,4) NOT NULL,
        [BaseDiscountAmount] decimal(19,4) NOT NULL,
        [BaseTaxAmount] decimal(19,4) NOT NULL,
        [BaseTotalAmount] decimal(19,4) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [JournalEntryId] uniqueidentifier NULL,
        [ConfirmedAtUtc] datetimeoffset NULL,
        [ConfirmedBy] nvarchar(64) NULL,
        [PostedAtUtc] datetimeoffset NULL,
        [PostedBy] nvarchar(64) NULL,
        [CancelledAtUtc] datetimeoffset NULL,
        [CancelledBy] nvarchar(64) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_SalesInvoices] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesInvoices_ExchangeRate_Positive] CHECK ([ExchangeRate] > 0),
        CONSTRAINT [CK_SalesInvoices_PaymentTermDays_NonNegative] CHECK ([PaymentTermDaysSnapshot] >= 0),
        CONSTRAINT [CK_SalesInvoices_Totals_NonNegative] CHECK ([Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0),
        CONSTRAINT [CK_SalesInvoices_BaseTotals_NonNegative] CHECK ([BaseSubtotal] >= 0 AND [BaseDiscountAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0),
        CONSTRAINT [FK_SalesInvoices_Customers_CustomerId]
            FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[tbl_Customers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesInvoices_CustomerOrders_CustomerOrderId]
            FOREIGN KEY ([CustomerOrderId]) REFERENCES [dbo].[tbl_CustomerOrders]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesInvoices_PrescriptionRevisions_PrescriptionRevisionId]
            FOREIGN KEY ([PrescriptionRevisionId]) REFERENCES [dbo].[tbl_PrescriptionRevisions]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesInvoices_Currencies_CurrencyId]
            FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesInvoices_BaseCurrencies_BaseCurrencyId]
            FOREIGN KEY ([BaseCurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesInvoices_JournalEntries_JournalEntryId]
            FOREIGN KEY ([JournalEntryId]) REFERENCES [dbo].[tbl_JournalEntries]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SalesInvoices_InvoiceCode' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    CREATE UNIQUE INDEX [UX_SalesInvoices_InvoiceCode] ON [dbo].[tbl_SalesInvoices]([InvoiceCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_CustomerId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    CREATE INDEX [IX_SalesInvoices_CustomerId] ON [dbo].[tbl_SalesInvoices]([CustomerId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_InvoiceDate' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    CREATE INDEX [IX_SalesInvoices_InvoiceDate] ON [dbo].[tbl_SalesInvoices]([InvoiceDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_PostingDate' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    CREATE INDEX [IX_SalesInvoices_PostingDate] ON [dbo].[tbl_SalesInvoices]([PostingDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_Status' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    CREATE INDEX [IX_SalesInvoices_Status] ON [dbo].[tbl_SalesInvoices]([Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_CustomerOrderId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    CREATE INDEX [IX_SalesInvoices_CustomerOrderId] ON [dbo].[tbl_SalesInvoices]([CustomerOrderId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_JournalEntryId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    CREATE INDEX [IX_SalesInvoices_JournalEntryId] ON [dbo].[tbl_SalesInvoices]([JournalEntryId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_PrescriptionRevisionId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    CREATE INDEX [IX_SalesInvoices_PrescriptionRevisionId] ON [dbo].[tbl_SalesInvoices]([PrescriptionRevisionId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_CurrencyId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    CREATE INDEX [IX_SalesInvoices_CurrencyId] ON [dbo].[tbl_SalesInvoices]([CurrencyId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoices_BaseCurrencyId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoices]'))
    CREATE INDEX [IX_SalesInvoices_BaseCurrencyId] ON [dbo].[tbl_SalesInvoices]([BaseCurrencyId]);

IF OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_SalesInvoiceLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [SalesInvoiceId] uniqueidentifier NOT NULL,
        [LineNumber] int NOT NULL,
        [CustomerOrderLineId] uniqueidentifier NULL,
        [GroupId] uniqueidentifier NULL,
        [LineType] tinyint NOT NULL,
        [ProductVariantId] uniqueidentifier NULL,
        [WarehouseId] uniqueidentifier NULL,
        [ProductCodeSnapshot] nvarchar(100) NULL,
        [ProductNameSnapshot] nvarchar(250) NOT NULL,
        [DescriptionSnapshot] nvarchar(500) NOT NULL,
        [UnitSnapshot] nvarchar(100) NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [BaseUnitPrice] decimal(19,4) NOT NULL,
        [ActualUnitPrice] decimal(19,4) NOT NULL,
        [DiscountType] tinyint NOT NULL,
        [DiscountValue] decimal(19,4) NULL,
        [DiscountAmount] decimal(19,4) NOT NULL,
        [TaxRate] decimal(9,4) NULL,
        [TaxAmount] decimal(19,4) NOT NULL,
        [NetAmount] decimal(19,4) NOT NULL,
        [FinalAmount] decimal(19,4) NOT NULL,
        [BaseNetAmount] decimal(19,4) NOT NULL,
        [BaseTaxAmount] decimal(19,4) NOT NULL,
        [BaseFinalAmount] decimal(19,4) NOT NULL,
        [UnitCostSnapshot] decimal(19,4) NULL,
        [TotalCostSnapshot] decimal(19,4) NULL,
        [PrescriptionRevisionId] uniqueidentifier NULL,
        [PrescriptionEye] tinyint NULL,
        [RequiresProduction] bit NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_SalesInvoiceLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesInvoiceLines_Quantity_Positive] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_SalesInvoiceLines_BaseUnitPrice_NonNegative] CHECK ([BaseUnitPrice] >= 0),
        CONSTRAINT [CK_SalesInvoiceLines_ActualUnitPrice_NonNegative] CHECK ([ActualUnitPrice] >= 0),
        CONSTRAINT [CK_SalesInvoiceLines_DiscountAmount_NonNegative] CHECK ([DiscountAmount] >= 0),
        CONSTRAINT [CK_SalesInvoiceLines_DiscountWithinGross] CHECK ([DiscountAmount] <= ([Quantity] * [ActualUnitPrice])),
        CONSTRAINT [CK_SalesInvoiceLines_TaxAmount_NonNegative] CHECK ([TaxAmount] >= 0),
        CONSTRAINT [CK_SalesInvoiceLines_NetAmount_NonNegative] CHECK ([NetAmount] >= 0),
        CONSTRAINT [CK_SalesInvoiceLines_FinalAmount_NonNegative] CHECK ([FinalAmount] >= 0),
        CONSTRAINT [CK_SalesInvoiceLines_BaseAmounts_NonNegative] CHECK ([BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseFinalAmount] >= 0),
        CONSTRAINT [CK_SalesInvoiceLines_CostSnapshots_NonNegative] CHECK (([UnitCostSnapshot] IS NULL OR [UnitCostSnapshot] >= 0) AND ([TotalCostSnapshot] IS NULL OR [TotalCostSnapshot] >= 0)),
        CONSTRAINT [FK_SalesInvoiceLines_Invoices_SalesInvoiceId]
            FOREIGN KEY ([SalesInvoiceId]) REFERENCES [dbo].[tbl_SalesInvoices]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesInvoiceLines_CustomerOrderLines_CustomerOrderLineId]
            FOREIGN KEY ([CustomerOrderLineId]) REFERENCES [dbo].[tbl_CustomerOrderLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesInvoiceLines_ProductVariants_ProductVariantId]
            FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesInvoiceLines_Warehouses_WarehouseId]
            FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[tbl_Warehouses]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesInvoiceLines_PrescriptionRevisions_PrescriptionRevisionId]
            FOREIGN KEY ([PrescriptionRevisionId]) REFERENCES [dbo].[tbl_PrescriptionRevisions]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SalesInvoiceLines_Invoice_LineNumber' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]'))
    CREATE UNIQUE INDEX [UX_SalesInvoiceLines_Invoice_LineNumber] ON [dbo].[tbl_SalesInvoiceLines]([SalesInvoiceId], [LineNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoiceLines_SalesInvoiceId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]'))
    CREATE INDEX [IX_SalesInvoiceLines_SalesInvoiceId] ON [dbo].[tbl_SalesInvoiceLines]([SalesInvoiceId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoiceLines_ProductVariantId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]'))
    CREATE INDEX [IX_SalesInvoiceLines_ProductVariantId] ON [dbo].[tbl_SalesInvoiceLines]([ProductVariantId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoiceLines_WarehouseId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]'))
    CREATE INDEX [IX_SalesInvoiceLines_WarehouseId] ON [dbo].[tbl_SalesInvoiceLines]([WarehouseId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoiceLines_CustomerOrderLineId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]'))
    CREATE INDEX [IX_SalesInvoiceLines_CustomerOrderLineId] ON [dbo].[tbl_SalesInvoiceLines]([CustomerOrderLineId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoiceLines_PrescriptionRevisionId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]'))
    CREATE INDEX [IX_SalesInvoiceLines_PrescriptionRevisionId] ON [dbo].[tbl_SalesInvoiceLines]([PrescriptionRevisionId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoiceLines_GroupId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]'))
    CREATE INDEX [IX_SalesInvoiceLines_GroupId] ON [dbo].[tbl_SalesInvoiceLines]([GroupId]);

IF OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLinePrescriptionSnapshots]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_SalesInvoiceLinePrescriptionSnapshots]
    (
        [Id] uniqueidentifier NOT NULL,
        [SalesInvoiceLineId] uniqueidentifier NOT NULL,
        [PrescriptionRevisionId] uniqueidentifier NOT NULL,
        [Eye] tinyint NOT NULL,
        [SPH] decimal(6,2) NULL,
        [CYL] decimal(6,2) NULL,
        [Axis] smallint NULL,
        [ADD] decimal(6,2) NULL,
        [Prism] decimal(6,2) NULL,
        [PrismBase] tinyint NULL,
        [PD] decimal(6,2) NULL,
        [MonocularPD] decimal(6,2) NULL,
        [VA] nvarchar(20) NULL,
        [FittingHeight] decimal(6,2) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_SalesInvoiceLinePrescriptionSnapshots] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesInvoicePrescriptionSnapshots_Axis] CHECK ([Axis] IS NULL OR ([Axis] >= 0 AND [Axis] <= 180)),
        CONSTRAINT [CK_SalesInvoicePrescriptionSnapshots_ADD] CHECK ([ADD] IS NULL OR [ADD] >= 0),
        CONSTRAINT [CK_SalesInvoicePrescriptionSnapshots_Prism] CHECK ([Prism] IS NULL OR [Prism] >= 0),
        CONSTRAINT [CK_SalesInvoicePrescriptionSnapshots_PD] CHECK ([PD] IS NULL OR [PD] > 0),
        CONSTRAINT [CK_SalesInvoicePrescriptionSnapshots_MonocularPD] CHECK ([MonocularPD] IS NULL OR [MonocularPD] > 0),
        CONSTRAINT [CK_SalesInvoicePrescriptionSnapshots_FittingHeight] CHECK ([FittingHeight] IS NULL OR [FittingHeight] > 0),
        CONSTRAINT [FK_SalesInvoicePrescriptionSnapshots_Lines_SalesInvoiceLineId]
            FOREIGN KEY ([SalesInvoiceLineId]) REFERENCES [dbo].[tbl_SalesInvoiceLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesInvoicePrescriptionSnapshots_Revisions_PrescriptionRevisionId]
            FOREIGN KEY ([PrescriptionRevisionId]) REFERENCES [dbo].[tbl_PrescriptionRevisions]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SalesInvoicePrescriptionSnapshots_LineId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLinePrescriptionSnapshots]'))
    CREATE UNIQUE INDEX [UX_SalesInvoicePrescriptionSnapshots_LineId] ON [dbo].[tbl_SalesInvoiceLinePrescriptionSnapshots]([SalesInvoiceLineId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesInvoicePrescriptionSnapshots_PrescriptionRevisionId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLinePrescriptionSnapshots]'))
    CREATE INDEX [IX_SalesInvoicePrescriptionSnapshots_PrescriptionRevisionId] ON [dbo].[tbl_SalesInvoiceLinePrescriptionSnapshots]([PrescriptionRevisionId]);

IF OBJECT_ID(N'[dbo].[tbl_SalesPriceOverrides]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_SalesPriceOverrides]
    (
        [Id] uniqueidentifier NOT NULL,
        [SalesInvoiceId] uniqueidentifier NOT NULL,
        [SalesInvoiceLineId] uniqueidentifier NOT NULL,
        [OriginalPrice] decimal(19,4) NOT NULL,
        [OverridePrice] decimal(19,4) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [Status] tinyint NOT NULL,
        [RequestedBy] nvarchar(64) NOT NULL,
        [RequestedAtUtc] datetimeoffset NOT NULL,
        [ApprovedBy] nvarchar(64) NULL,
        [ApprovedAtUtc] datetimeoffset NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_SalesPriceOverrides] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesPriceOverrides_Prices_NonNegative] CHECK ([OriginalPrice] >= 0 AND [OverridePrice] >= 0),
        CONSTRAINT [CK_SalesPriceOverrides_Prices_Different] CHECK ([OriginalPrice] <> [OverridePrice]),
        CONSTRAINT [FK_SalesPriceOverrides_Invoices_SalesInvoiceId]
            FOREIGN KEY ([SalesInvoiceId]) REFERENCES [dbo].[tbl_SalesInvoices]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesPriceOverrides_Lines_SalesInvoiceLineId]
            FOREIGN KEY ([SalesInvoiceLineId]) REFERENCES [dbo].[tbl_SalesInvoiceLines]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesPriceOverrides_SalesInvoiceId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesPriceOverrides]'))
    CREATE INDEX [IX_SalesPriceOverrides_SalesInvoiceId] ON [dbo].[tbl_SalesPriceOverrides]([SalesInvoiceId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesPriceOverrides_SalesInvoiceLineId' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesPriceOverrides]'))
    CREATE INDEX [IX_SalesPriceOverrides_SalesInvoiceLineId] ON [dbo].[tbl_SalesPriceOverrides]([SalesInvoiceLineId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesPriceOverrides_Line_Status' AND object_id = OBJECT_ID(N'[dbo].[tbl_SalesPriceOverrides]'))
    CREATE INDEX [IX_SalesPriceOverrides_Line_Status] ON [dbo].[tbl_SalesPriceOverrides]([SalesInvoiceLineId], [Status]);

/* ============================================================
   6) Existing-table indexes used by Sales integrations
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InventoryTransactions_Reference' AND object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]'))
    CREATE INDEX [IX_InventoryTransactions_Reference]
        ON [dbo].[tbl_InventoryTransactions]([ReferenceType], [ReferenceId]);

IF OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentAllocations_TargetDocument' AND object_id = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]'))
    CREATE INDEX [IX_PaymentAllocations_TargetDocument]
        ON [dbo].[tbl_PaymentAllocations]([TargetDocumentType], [TargetDocumentId]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

IF OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentAllocations_TargetDocument' AND object_id = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]'))
    DROP INDEX [IX_PaymentAllocations_TargetDocument] ON [dbo].[tbl_PaymentAllocations];

IF OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InventoryTransactions_Reference' AND object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]'))
    DROP INDEX [IX_InventoryTransactions_Reference] ON [dbo].[tbl_InventoryTransactions];

IF OBJECT_ID(N'[dbo].[tbl_SalesPriceOverrides]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_SalesPriceOverrides];

IF OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLinePrescriptionSnapshots]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_SalesInvoiceLinePrescriptionSnapshots];

IF OBJECT_ID(N'[dbo].[tbl_SalesInvoiceLines]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_SalesInvoiceLines];

IF OBJECT_ID(N'[dbo].[tbl_SalesInvoices]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_SalesInvoices];

IF OBJECT_ID(N'[dbo].[tbl_StockReservations]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_StockReservations];

IF OBJECT_ID(N'[dbo].[tbl_CustomerOrderLines]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_CustomerOrderLines];

IF OBJECT_ID(N'[dbo].[tbl_CustomerOrders]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_CustomerOrders];

IF OBJECT_ID(N'[dbo].[tbl_PrescriptionEyeDetails]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_PrescriptionEyeDetails];

IF OBJECT_ID(N'[dbo].[tbl_PrescriptionRevisions]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_PrescriptionRevisions];

IF OBJECT_ID(N'[dbo].[tbl_Prescriptions]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_Prescriptions];

IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'SalesInvoiceCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
    DROP SEQUENCE [dbo].[SalesInvoiceCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'CustomerOrderCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
    DROP SEQUENCE [dbo].[CustomerOrderCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'PrescriptionCodeSequence' AND schema_id = SCHEMA_ID(N'dbo'))
    DROP SEQUENCE [dbo].[PrescriptionCodeSequence];
""");
    }
}
