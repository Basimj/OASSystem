using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260929130000_PurchasingCoreInitial")]
public sealed class PurchasingCoreInitial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* Purchasing document sequences. Gaps are intentional and codes are never reused. */
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'PurchaseRequestCodeSequence' AND schema_id=SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[PurchaseRequestCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'PurchaseOrderCodeSequence' AND schema_id=SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[PurchaseOrderCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'PurchaseReceiptCodeSequence' AND schema_id=SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[PurchaseReceiptCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'PurchaseInvoiceCodeSequence' AND schema_id=SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE [dbo].[PurchaseInvoiceCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');

IF OBJECT_ID(N'[dbo].[tbl_SupplierCatalogItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_SupplierCatalogItems](
        [Id] uniqueidentifier NOT NULL,
        [SupplierId] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [SupplierProductCode] nvarchar(64) NULL,
        [SupplierProductName] nvarchar(200) NULL,
        [PurchaseUnitId] uniqueidentifier NOT NULL,
        [UnitConversionFactor] decimal(18,6) NOT NULL,
        [LeadTimeDays] int NULL,
        [MinimumOrderQuantity] decimal(18,3) NOT NULL,
        [IsPreferred] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_SupplierCatalogItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SupplierCatalogItems_UnitConversionFactor_Positive] CHECK ([UnitConversionFactor] > 0),
        CONSTRAINT [CK_SupplierCatalogItems_MinimumOrderQuantity_NonNegative] CHECK ([MinimumOrderQuantity] >= 0),
        CONSTRAINT [CK_SupplierCatalogItems_LeadTimeDays_NonNegative] CHECK ([LeadTimeDays] IS NULL OR [LeadTimeDays] >= 0),
        CONSTRAINT [FK_SupplierCatalogItems_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[tbl_Suppliers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplierCatalogItems_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplierCatalogItems_Units_PurchaseUnitId] FOREIGN KEY ([PurchaseUnitId]) REFERENCES [dbo].[tbl_Units]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_SupplierCatalogItems_Supplier_ProductVariant_PurchaseUnit] ON [dbo].[tbl_SupplierCatalogItems]([SupplierId],[ProductVariantId],[PurchaseUnitId]);
CREATE INDEX [IX_SupplierCatalogItems_SupplierId] ON [dbo].[tbl_SupplierCatalogItems]([SupplierId]);
CREATE INDEX [IX_SupplierCatalogItems_ProductVariantId] ON [dbo].[tbl_SupplierCatalogItems]([ProductVariantId]);
CREATE INDEX [IX_SupplierCatalogItems_IsActive] ON [dbo].[tbl_SupplierCatalogItems]([IsActive]);

IF OBJECT_ID(N'[dbo].[tbl_SupplierPriceHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_SupplierPriceHistory](
        [Id] uniqueidentifier NOT NULL,[SupplierCatalogItemId] uniqueidentifier NOT NULL,[CurrencyId] uniqueidentifier NOT NULL,
        [UnitPrice] decimal(19,4) NOT NULL,[EffectiveFrom] date NOT NULL,[EffectiveTo] date NULL,[IsCurrent] bit NOT NULL,[Notes] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_SupplierPriceHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SupplierPriceHistory_UnitPrice_NonNegative] CHECK ([UnitPrice] >= 0),
        CONSTRAINT [CK_SupplierPriceHistory_EffectiveDates] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [FK_SupplierPriceHistory_CatalogItem] FOREIGN KEY ([SupplierCatalogItemId]) REFERENCES [dbo].[tbl_SupplierCatalogItems]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplierPriceHistory_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION
    );
END;
CREATE INDEX [IX_SupplierPriceHistory_CatalogItemId] ON [dbo].[tbl_SupplierPriceHistory]([SupplierCatalogItemId]);
CREATE UNIQUE INDEX [UX_SupplierPriceHistory_Current_Catalog_Currency] ON [dbo].[tbl_SupplierPriceHistory]([SupplierCatalogItemId],[CurrencyId]) WHERE [IsCurrent] = 1;

IF OBJECT_ID(N'[dbo].[tbl_PurchaseRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseRequests](
        [Id] uniqueidentifier NOT NULL,[RequestCode] nvarchar(40) NOT NULL,[RequestType] tinyint NOT NULL,[Status] tinyint NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,[CustomerOrderId] uniqueidentifier NULL,[RequestDate] date NOT NULL,[RequiredDate] date NULL,
        [Reason] nvarchar(500) NULL,[Notes] nvarchar(1000) NULL,[RequestedBy] nvarchar(64) NULL,[SubmittedBy] nvarchar(64) NULL,[SubmittedAt] datetimeoffset NULL,
        [ApprovedBy] nvarchar(64) NULL,[ApprovedAt] datetimeoffset NULL,[RejectedBy] nvarchar(64) NULL,[RejectedAt] datetimeoffset NULL,[RejectionReason] nvarchar(500) NULL,
        [CancelledBy] nvarchar(64) NULL,[CancelledAt] datetimeoffset NULL,[CancellationReason] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseRequests_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[tbl_Warehouses]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseRequests_CustomerOrders_CustomerOrderId] FOREIGN KEY ([CustomerOrderId]) REFERENCES [dbo].[tbl_CustomerOrders]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_PurchaseRequests_RequestCode] ON [dbo].[tbl_PurchaseRequests]([RequestCode]);
CREATE INDEX [IX_PurchaseRequests_Status] ON [dbo].[tbl_PurchaseRequests]([Status]);
CREATE INDEX [IX_PurchaseRequests_RequestDate] ON [dbo].[tbl_PurchaseRequests]([RequestDate]);
CREATE INDEX [IX_PurchaseRequests_WarehouseId] ON [dbo].[tbl_PurchaseRequests]([WarehouseId]);

IF OBJECT_ID(N'[dbo].[tbl_PurchaseRequestLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseRequestLines](
        [Id] uniqueidentifier NOT NULL,[PurchaseRequestId] uniqueidentifier NOT NULL,[LineSequence] int NOT NULL,[ProductVariantId] uniqueidentifier NOT NULL,
        [RequestedQuantity] decimal(18,3) NOT NULL,[RequiredDate] date NULL,[CustomerOrderLineId] uniqueidentifier NULL,[PreferredSupplierId] uniqueidentifier NULL,[Notes] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseRequestLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseRequestLines_RequestedQuantity_Positive] CHECK ([RequestedQuantity] > 0),
        CONSTRAINT [CK_PurchaseRequestLines_LineSequence_Positive] CHECK ([LineSequence] > 0),
        CONSTRAINT [FK_PurchaseRequestLines_Requests_PurchaseRequestId] FOREIGN KEY ([PurchaseRequestId]) REFERENCES [dbo].[tbl_PurchaseRequests]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseRequestLines_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseRequestLines_CustomerOrderLines_CustomerOrderLineId] FOREIGN KEY ([CustomerOrderLineId]) REFERENCES [dbo].[tbl_CustomerOrderLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseRequestLines_Suppliers_PreferredSupplierId] FOREIGN KEY ([PreferredSupplierId]) REFERENCES [dbo].[tbl_Suppliers]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_PurchaseRequestLines_Request_LineSequence] ON [dbo].[tbl_PurchaseRequestLines]([PurchaseRequestId],[LineSequence]);
CREATE INDEX [IX_PurchaseRequestLines_ProductVariantId] ON [dbo].[tbl_PurchaseRequestLines]([ProductVariantId]);

IF OBJECT_ID(N'[dbo].[tbl_PurchaseOrders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseOrders](
        [Id] uniqueidentifier NOT NULL,[PurchaseOrderCode] nvarchar(40) NOT NULL,[SupplierId] uniqueidentifier NOT NULL,[DestinationWarehouseId] uniqueidentifier NOT NULL,
        [OrderDate] date NOT NULL,[ExpectedDeliveryDate] date NULL,[CurrencyId] uniqueidentifier NOT NULL,[ExchangeRate] decimal(19,8) NOT NULL,[ExchangeRateDate] date NOT NULL,
        [TaxCalculationMode] tinyint NOT NULL,[Status] tinyint NOT NULL,[Subtotal] decimal(19,4) NOT NULL,[DiscountAmount] decimal(19,4) NOT NULL,[TaxAmount] decimal(19,4) NOT NULL,[TotalAmount] decimal(19,4) NOT NULL,
        [PaymentTermDays] int NOT NULL,[Notes] nvarchar(1000) NULL,[SubmittedBy] nvarchar(64) NULL,[SubmittedAt] datetimeoffset NULL,[ApprovedBy] nvarchar(64) NULL,[ApprovedAt] datetimeoffset NULL,
        [RejectedBy] nvarchar(64) NULL,[RejectedAt] datetimeoffset NULL,[RejectionReason] nvarchar(500) NULL,[SentBy] nvarchar(64) NULL,[SentAt] datetimeoffset NULL,
        [ClosedBy] nvarchar(64) NULL,[ClosedAt] datetimeoffset NULL,[CancelledBy] nvarchar(64) NULL,[CancelledAt] datetimeoffset NULL,[CancellationReason] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrders_ExchangeRate_Positive] CHECK ([ExchangeRate] > 0),
        CONSTRAINT [CK_PurchaseOrders_PaymentTermDays_NonNegative] CHECK ([PaymentTermDays] >= 0),
        CONSTRAINT [CK_PurchaseOrders_Totals_NonNegative] CHECK ([Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0),
        CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[tbl_Suppliers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrders_Warehouses_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [dbo].[tbl_Warehouses]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrders_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_PurchaseOrders_PurchaseOrderCode] ON [dbo].[tbl_PurchaseOrders]([PurchaseOrderCode]);
CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [dbo].[tbl_PurchaseOrders]([SupplierId]);
CREATE INDEX [IX_PurchaseOrders_Status] ON [dbo].[tbl_PurchaseOrders]([Status]);
CREATE INDEX [IX_PurchaseOrders_OrderDate] ON [dbo].[tbl_PurchaseOrders]([OrderDate]);
CREATE INDEX [IX_PurchaseOrders_DestinationWarehouseId] ON [dbo].[tbl_PurchaseOrders]([DestinationWarehouseId]);

IF OBJECT_ID(N'[dbo].[tbl_PurchaseOrderLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseOrderLines](
        [Id] uniqueidentifier NOT NULL,[PurchaseOrderId] uniqueidentifier NOT NULL,[LineSequence] int NOT NULL,[ProductVariantId] uniqueidentifier NOT NULL,[SupplierCatalogItemId] uniqueidentifier NULL,
        [PurchaseUnitId] uniqueidentifier NOT NULL,[UnitConversionFactor] decimal(18,6) NOT NULL,[ProductCodeSnapshot] nvarchar(64) NULL,[ProductNameSnapshot] nvarchar(200) NOT NULL,[UnitNameSnapshot] nvarchar(100) NULL,
        [OrderedQuantity] decimal(18,3) NOT NULL,[BaseQuantity] decimal(18,3) NOT NULL,[UnitPrice] decimal(19,4) NOT NULL,[DiscountAmount] decimal(19,4) NOT NULL,[NetAmount] decimal(19,4) NOT NULL,
        [TaxRate] decimal(9,6) NOT NULL,[TaxAmount] decimal(19,4) NOT NULL,[FinalAmount] decimal(19,4) NOT NULL,[ExpectedDeliveryDate] date NULL,[Notes] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrderLines_Quantities_Positive] CHECK ([OrderedQuantity] > 0 AND [BaseQuantity] > 0 AND [UnitConversionFactor] > 0),
        CONSTRAINT [CK_PurchaseOrderLines_Amounts_NonNegative] CHECK ([UnitPrice] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] >= 0 AND [TaxAmount] >= 0 AND [FinalAmount] >= 0),
        CONSTRAINT [FK_PurchaseOrderLines_Orders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [dbo].[tbl_PurchaseOrders]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderLines_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderLines_SupplierCatalogItems_SupplierCatalogItemId] FOREIGN KEY ([SupplierCatalogItemId]) REFERENCES [dbo].[tbl_SupplierCatalogItems]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderLines_Units_PurchaseUnitId] FOREIGN KEY ([PurchaseUnitId]) REFERENCES [dbo].[tbl_Units]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_PurchaseOrderLines_Order_LineSequence] ON [dbo].[tbl_PurchaseOrderLines]([PurchaseOrderId],[LineSequence]);
CREATE INDEX [IX_PurchaseOrderLines_ProductVariantId] ON [dbo].[tbl_PurchaseOrderLines]([ProductVariantId]);

IF OBJECT_ID(N'[dbo].[tbl_PurchaseOrderLineSources]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseOrderLineSources](
        [Id] uniqueidentifier NOT NULL,[PurchaseOrderLineId] uniqueidentifier NOT NULL,[PurchaseRequestLineId] uniqueidentifier NOT NULL,[AllocatedQuantity] decimal(18,3) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseOrderLineSources] PRIMARY KEY ([Id]),CONSTRAINT [CK_PurchaseOrderLineSources_AllocatedQuantity_Positive] CHECK ([AllocatedQuantity] > 0),
        CONSTRAINT [FK_PurchaseOrderLineSources_OrderLines_PurchaseOrderLineId] FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [dbo].[tbl_PurchaseOrderLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderLineSources_RequestLines_PurchaseRequestLineId] FOREIGN KEY ([PurchaseRequestLineId]) REFERENCES [dbo].[tbl_PurchaseRequestLines]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_PurchaseOrderLineSources_OrderLine_RequestLine] ON [dbo].[tbl_PurchaseOrderLineSources]([PurchaseOrderLineId],[PurchaseRequestLineId]);
CREATE INDEX [IX_PurchaseOrderLineSources_RequestLineId] ON [dbo].[tbl_PurchaseOrderLineSources]([PurchaseRequestLineId]);

IF OBJECT_ID(N'[dbo].[tbl_PurchaseReceipts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseReceipts](
        [Id] uniqueidentifier NOT NULL,[ReceiptCode] nvarchar(40) NOT NULL,[PurchaseOrderId] uniqueidentifier NOT NULL,[SupplierId] uniqueidentifier NOT NULL,[WarehouseId] uniqueidentifier NOT NULL,
        [ReceiptDate] date NOT NULL,[PostingDate] date NOT NULL,[SupplierDeliveryCode] nvarchar(100) NULL,[Status] tinyint NOT NULL,[InventoryTransactionId] uniqueidentifier NULL,[JournalEntryId] uniqueidentifier NULL,[Notes] nvarchar(1000) NULL,
        [ConfirmedBy] nvarchar(64) NULL,[ConfirmedAt] datetimeoffset NULL,[PostedBy] nvarchar(64) NULL,[PostedAt] datetimeoffset NULL,[CancelledBy] nvarchar(64) NULL,[CancelledAt] datetimeoffset NULL,[CancellationReason] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseReceipts_Orders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [dbo].[tbl_PurchaseOrders]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReceipts_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[tbl_Suppliers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReceipts_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[tbl_Warehouses]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReceipts_InventoryTransactions_InventoryTransactionId] FOREIGN KEY ([InventoryTransactionId]) REFERENCES [dbo].[tbl_InventoryTransactions]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReceipts_JournalEntries_JournalEntryId] FOREIGN KEY ([JournalEntryId]) REFERENCES [dbo].[tbl_JournalEntries]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_PurchaseReceipts_ReceiptCode] ON [dbo].[tbl_PurchaseReceipts]([ReceiptCode]);
CREATE INDEX [IX_PurchaseReceipts_PurchaseOrderId] ON [dbo].[tbl_PurchaseReceipts]([PurchaseOrderId]);
CREATE INDEX [IX_PurchaseReceipts_SupplierId] ON [dbo].[tbl_PurchaseReceipts]([SupplierId]);
CREATE INDEX [IX_PurchaseReceipts_PostingDate] ON [dbo].[tbl_PurchaseReceipts]([PostingDate]);
CREATE INDEX [IX_PurchaseReceipts_Status] ON [dbo].[tbl_PurchaseReceipts]([Status]);

IF OBJECT_ID(N'[dbo].[tbl_PurchaseReceiptLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseReceiptLines](
        [Id] uniqueidentifier NOT NULL,[PurchaseReceiptId] uniqueidentifier NOT NULL,[PurchaseOrderLineId] uniqueidentifier NOT NULL,[LineSequence] int NOT NULL,[ProductVariantId] uniqueidentifier NOT NULL,
        [OrderedQuantitySnapshot] decimal(18,3) NOT NULL,[PreviouslyReceivedQty] decimal(18,3) NOT NULL,[ReceivedQuantity] decimal(18,3) NOT NULL,[AcceptedQuantity] decimal(18,3) NOT NULL,[RejectedQuantity] decimal(18,3) NOT NULL,
        [BaseAcceptedQuantity] decimal(18,3) NOT NULL,[ActualUnitCost] decimal(19,4) NOT NULL,[TotalAcceptedCost] decimal(19,4) NOT NULL,[ExpiryDate] date NULL,[BatchCode] nvarchar(100) NULL,[Notes] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseReceiptLines] PRIMARY KEY ([Id]),CONSTRAINT [CK_PurchaseReceiptLines_ReceivedQuantity_Positive] CHECK ([ReceivedQuantity] > 0),
        CONSTRAINT [CK_PurchaseReceiptLines_Quantities_NonNegative] CHECK ([AcceptedQuantity] >= 0 AND [RejectedQuantity] >= 0 AND [BaseAcceptedQuantity] >= 0),
        CONSTRAINT [CK_PurchaseReceiptLines_ActualUnitCost_NonNegative] CHECK ([ActualUnitCost] >= 0),
        CONSTRAINT [FK_PurchaseReceiptLines_Receipts_PurchaseReceiptId] FOREIGN KEY ([PurchaseReceiptId]) REFERENCES [dbo].[tbl_PurchaseReceipts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReceiptLines_OrderLines_PurchaseOrderLineId] FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [dbo].[tbl_PurchaseOrderLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseReceiptLines_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_PurchaseReceiptLines_Receipt_LineSequence] ON [dbo].[tbl_PurchaseReceiptLines]([PurchaseReceiptId],[LineSequence]);
CREATE INDEX [IX_PurchaseReceiptLines_PurchaseOrderLineId] ON [dbo].[tbl_PurchaseReceiptLines]([PurchaseOrderLineId]);

IF OBJECT_ID(N'[dbo].[tbl_PurchaseInvoices]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseInvoices](
        [Id] uniqueidentifier NOT NULL,[PurchaseInvoiceCode] nvarchar(40) NOT NULL,[SupplierInvoiceCode] nvarchar(100) NULL,[SupplierId] uniqueidentifier NOT NULL,
        [InvoiceDate] date NOT NULL,[PostingDate] date NOT NULL,[CurrencyId] uniqueidentifier NOT NULL,[ExchangeRate] decimal(19,8) NOT NULL,[ExchangeRateDate] date NOT NULL,[TaxCalculationMode] tinyint NOT NULL,[Status] tinyint NOT NULL,
        [Subtotal] decimal(19,4) NOT NULL,[DiscountAmount] decimal(19,4) NOT NULL,[TaxAmount] decimal(19,4) NOT NULL,[TotalAmount] decimal(19,4) NOT NULL,[BaseSubtotal] decimal(19,4) NOT NULL,[BaseTaxAmount] decimal(19,4) NOT NULL,[BaseTotalAmount] decimal(19,4) NOT NULL,
        [JournalEntryId] uniqueidentifier NULL,[Notes] nvarchar(1000) NULL,[ConfirmedBy] nvarchar(64) NULL,[ConfirmedAt] datetimeoffset NULL,[PostedBy] nvarchar(64) NULL,[PostedAt] datetimeoffset NULL,[CancelledBy] nvarchar(64) NULL,[CancelledAt] datetimeoffset NULL,[CancellationReason] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseInvoices] PRIMARY KEY ([Id]),CONSTRAINT [CK_PurchaseInvoices_ExchangeRate_Positive] CHECK ([ExchangeRate] > 0),
        CONSTRAINT [CK_PurchaseInvoices_Totals_NonNegative] CHECK ([Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0 AND [BaseSubtotal] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseTotalAmount] >= 0),
        CONSTRAINT [FK_PurchaseInvoices_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[tbl_Suppliers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseInvoices_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseInvoices_JournalEntries_JournalEntryId] FOREIGN KEY ([JournalEntryId]) REFERENCES [dbo].[tbl_JournalEntries]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_PurchaseInvoices_PurchaseInvoiceCode] ON [dbo].[tbl_PurchaseInvoices]([PurchaseInvoiceCode]);
CREATE UNIQUE INDEX [UX_PurchaseInvoices_Supplier_SupplierInvoiceCode] ON [dbo].[tbl_PurchaseInvoices]([SupplierId],[SupplierInvoiceCode]) WHERE [SupplierInvoiceCode] IS NOT NULL;
CREATE INDEX [IX_PurchaseInvoices_SupplierId] ON [dbo].[tbl_PurchaseInvoices]([SupplierId]);
CREATE INDEX [IX_PurchaseInvoices_InvoiceDate] ON [dbo].[tbl_PurchaseInvoices]([InvoiceDate]);
CREATE INDEX [IX_PurchaseInvoices_PostingDate] ON [dbo].[tbl_PurchaseInvoices]([PostingDate]);
CREATE INDEX [IX_PurchaseInvoices_Status] ON [dbo].[tbl_PurchaseInvoices]([Status]);

IF OBJECT_ID(N'[dbo].[tbl_PurchaseInvoiceLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseInvoiceLines](
        [Id] uniqueidentifier NOT NULL,[PurchaseInvoiceId] uniqueidentifier NOT NULL,[LineSequence] int NOT NULL,[PurchaseOrderLineId] uniqueidentifier NULL,[ProductVariantId] uniqueidentifier NOT NULL,
        [ProductCodeSnapshot] nvarchar(64) NULL,[DescriptionSnapshot] nvarchar(250) NOT NULL,[Quantity] decimal(18,3) NOT NULL,[UnitPrice] decimal(19,4) NOT NULL,[GrossAmount] decimal(19,4) NOT NULL,[DiscountAmount] decimal(19,4) NOT NULL,[NetAmount] decimal(19,4) NOT NULL,
        [TaxRate] decimal(9,6) NOT NULL,[TaxAmount] decimal(19,4) NOT NULL,[FinalAmount] decimal(19,4) NOT NULL,[BaseNetAmount] decimal(19,4) NOT NULL,[BaseTaxAmount] decimal(19,4) NOT NULL,[BaseFinalAmount] decimal(19,4) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseInvoiceLines] PRIMARY KEY ([Id]),CONSTRAINT [CK_PurchaseInvoiceLines_Quantity_Positive] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_PurchaseInvoiceLines_Amounts_NonNegative] CHECK ([UnitPrice] >= 0 AND [GrossAmount] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] >= 0 AND [TaxAmount] >= 0 AND [FinalAmount] >= 0 AND [BaseNetAmount] >= 0 AND [BaseTaxAmount] >= 0 AND [BaseFinalAmount] >= 0),
        CONSTRAINT [FK_PurchaseInvoiceLines_Invoices_PurchaseInvoiceId] FOREIGN KEY ([PurchaseInvoiceId]) REFERENCES [dbo].[tbl_PurchaseInvoices]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseInvoiceLines_OrderLines_PurchaseOrderLineId] FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [dbo].[tbl_PurchaseOrderLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseInvoiceLines_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION
    );
END;
CREATE UNIQUE INDEX [UX_PurchaseInvoiceLines_Invoice_LineSequence] ON [dbo].[tbl_PurchaseInvoiceLines]([PurchaseInvoiceId],[LineSequence]);
CREATE INDEX [IX_PurchaseInvoiceLines_PurchaseOrderLineId] ON [dbo].[tbl_PurchaseInvoiceLines]([PurchaseOrderLineId]);
CREATE INDEX [IX_PurchaseInvoiceLines_ProductVariantId] ON [dbo].[tbl_PurchaseInvoiceLines]([ProductVariantId]);

IF OBJECT_ID(N'[dbo].[tbl_PurchaseInvoiceReceiptAllocations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_PurchaseInvoiceReceiptAllocations](
        [Id] uniqueidentifier NOT NULL,[PurchaseInvoiceLineId] uniqueidentifier NOT NULL,[PurchaseReceiptLineId] uniqueidentifier NOT NULL,
        [MatchedQuantity] decimal(18,3) NOT NULL,[MatchedNetAmount] decimal(19,4) NOT NULL,[QuantityVariance] decimal(18,3) NOT NULL,[PriceVarianceAmount] decimal(19,4) NOT NULL,[TaxVarianceAmount] decimal(19,4) NOT NULL,[MatchStatus] tinyint NOT NULL,
        [ApprovalReason] nvarchar(500) NULL,[ApprovedBy] nvarchar(64) NULL,[ApprovedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[UpdatedAt] datetimeoffset NULL,[UpdatedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_PurchaseInvoiceReceiptAllocations] PRIMARY KEY ([Id]),CONSTRAINT [CK_PurchaseInvoiceReceiptAllocations_MatchedQuantity_Positive] CHECK ([MatchedQuantity] > 0),
        CONSTRAINT [FK_PurchaseInvoiceReceiptAllocations_InvoiceLines_PurchaseInvoiceLineId] FOREIGN KEY ([PurchaseInvoiceLineId]) REFERENCES [dbo].[tbl_PurchaseInvoiceLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseInvoiceReceiptAllocations_ReceiptLines_PurchaseReceiptLineId] FOREIGN KEY ([PurchaseReceiptLineId]) REFERENCES [dbo].[tbl_PurchaseReceiptLines]([Id]) ON DELETE NO ACTION
    );
END;
CREATE INDEX [IX_PurchaseInvoiceReceiptAllocations_InvoiceLine] ON [dbo].[tbl_PurchaseInvoiceReceiptAllocations]([PurchaseInvoiceLineId]);
CREATE INDEX [IX_PurchaseInvoiceReceiptAllocations_ReceiptLine] ON [dbo].[tbl_PurchaseInvoiceReceiptAllocations]([PurchaseReceiptLineId]);
CREATE INDEX [IX_PurchaseInvoiceReceiptAllocations_MatchStatus] ON [dbo].[tbl_PurchaseInvoiceReceiptAllocations]([MatchStatus]);
CREATE UNIQUE INDEX [UX_PurchaseInvoiceReceiptAllocations_InvoiceLine_ReceiptLine] ON [dbo].[tbl_PurchaseInvoiceReceiptAllocations]([PurchaseInvoiceLineId],[PurchaseReceiptLineId]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF OBJECT_ID(N'[dbo].[tbl_PurchaseInvoiceReceiptAllocations]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseInvoiceReceiptAllocations];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseInvoiceLines]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseInvoiceLines];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseInvoices]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseInvoices];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseReceiptLines]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseReceiptLines];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseReceipts]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseReceipts];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseOrderLineSources]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseOrderLineSources];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseOrderLines]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseOrderLines];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseOrders]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseOrders];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseRequestLines]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseRequestLines];
IF OBJECT_ID(N'[dbo].[tbl_PurchaseRequests]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_PurchaseRequests];
IF OBJECT_ID(N'[dbo].[tbl_SupplierPriceHistory]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_SupplierPriceHistory];
IF OBJECT_ID(N'[dbo].[tbl_SupplierCatalogItems]',N'U') IS NOT NULL DROP TABLE [dbo].[tbl_SupplierCatalogItems];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'PurchaseInvoiceCodeSequence' AND schema_id=SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[PurchaseInvoiceCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'PurchaseReceiptCodeSequence' AND schema_id=SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[PurchaseReceiptCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'PurchaseOrderCodeSequence' AND schema_id=SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[PurchaseOrderCodeSequence];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name=N'PurchaseRequestCodeSequence' AND schema_id=SCHEMA_ID(N'dbo')) DROP SEQUENCE [dbo].[PurchaseRequestCodeSequence];
""");
    }
}
