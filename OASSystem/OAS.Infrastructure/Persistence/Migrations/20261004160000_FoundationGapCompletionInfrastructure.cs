using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261004160000_FoundationGapCompletionInfrastructure")]
public sealed class FoundationGapCompletionInfrastructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* ============================================================
   1) Precise inventory identity for lens variants
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_LensVariantDetails]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_LensVariantDetails]
    (
        [Id] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [SPH] decimal(6,2) NULL,
        [CYL] decimal(6,2) NULL,
        [ADD] decimal(6,2) NULL,
        [BaseCurve] decimal(6,2) NULL,
        [Diameter] decimal(6,2) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_LensVariantDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LensVariantDetails_ADD] CHECK ([ADD] IS NULL OR [ADD] >= 0),
        CONSTRAINT [CK_LensVariantDetails_BaseCurve] CHECK ([BaseCurve] IS NULL OR [BaseCurve] > 0),
        CONSTRAINT [CK_LensVariantDetails_Diameter] CHECK ([Diameter] IS NULL OR [Diameter] > 0),
        CONSTRAINT [FK_LensVariantDetails_ProductVariants_ProductVariantId]
            FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_LensVariantDetails_ProductVariantId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_LensVariantDetails]'))
    CREATE UNIQUE INDEX [UX_LensVariantDetails_ProductVariantId] ON [dbo].[tbl_LensVariantDetails]([ProductVariantId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_LensVariantDetails_SPH_CYL_ADD' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_LensVariantDetails]'))
    CREATE INDEX [IX_LensVariantDetails_SPH_CYL_ADD] ON [dbo].[tbl_LensVariantDetails]([SPH], [CYL], [ADD]);

/* ============================================================
   2) Immutable optical snapshot on customer-order lines
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_CustomerOrderLineOpticalSnapshots]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_CustomerOrderLineOpticalSnapshots]
    (
        [Id] uniqueidentifier NOT NULL,
        [CustomerOrderLineId] uniqueidentifier NOT NULL,
        [MeasurementSource] tinyint NOT NULL,
        [PrescriptionRevisionId] uniqueidentifier NULL,
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
        [LensTypeSnapshot] nvarchar(100) NULL,
        [MaterialSnapshot] nvarchar(100) NULL,
        [CoatingSnapshot] nvarchar(100) NULL,
        [RefractiveIndexSnapshot] decimal(5,3) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_CustomerOrderLineOpticalSnapshots] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerOrderLineOpticalSnapshots_Source] CHECK (([MeasurementSource] = 1 AND [PrescriptionRevisionId] IS NOT NULL) OR ([MeasurementSource] = 2)),
        CONSTRAINT [CK_CustomerOrderLineOpticalSnapshots_Axis] CHECK ([Axis] IS NULL OR ([Axis] >= 0 AND [Axis] <= 180)),
        CONSTRAINT [CK_CustomerOrderLineOpticalSnapshots_ADD] CHECK ([ADD] IS NULL OR [ADD] >= 0),
        CONSTRAINT [CK_CustomerOrderLineOpticalSnapshots_Prism] CHECK ([Prism] IS NULL OR [Prism] >= 0),
        CONSTRAINT [CK_CustomerOrderLineOpticalSnapshots_PD] CHECK ([PD] IS NULL OR [PD] > 0),
        CONSTRAINT [CK_CustomerOrderLineOpticalSnapshots_MonocularPD] CHECK ([MonocularPD] IS NULL OR [MonocularPD] > 0),
        CONSTRAINT [CK_CustomerOrderLineOpticalSnapshots_FittingHeight] CHECK ([FittingHeight] IS NULL OR [FittingHeight] > 0),
        CONSTRAINT [CK_CustomerOrderLineOpticalSnapshots_RefractiveIndex] CHECK ([RefractiveIndexSnapshot] IS NULL OR [RefractiveIndexSnapshot] > 0),
        CONSTRAINT [FK_CustomerOrderLineOpticalSnapshots_CustomerOrderLines_CustomerOrderLineId]
            FOREIGN KEY ([CustomerOrderLineId]) REFERENCES [dbo].[tbl_CustomerOrderLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerOrderLineOpticalSnapshots_PrescriptionRevisions_PrescriptionRevisionId]
            FOREIGN KEY ([PrescriptionRevisionId]) REFERENCES [dbo].[tbl_PrescriptionRevisions]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_CustomerOrderLineOpticalSnapshots_CustomerOrderLineId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerOrderLineOpticalSnapshots]'))
    CREATE UNIQUE INDEX [UX_CustomerOrderLineOpticalSnapshots_CustomerOrderLineId] ON [dbo].[tbl_CustomerOrderLineOpticalSnapshots]([CustomerOrderLineId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CustomerOrderLineOpticalSnapshots_PrescriptionRevisionId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerOrderLineOpticalSnapshots]'))
    CREATE INDEX [IX_CustomerOrderLineOpticalSnapshots_PrescriptionRevisionId] ON [dbo].[tbl_CustomerOrderLineOpticalSnapshots]([PrescriptionRevisionId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CustomerOrderLineOpticalSnapshots_Eye' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerOrderLineOpticalSnapshots]'))
    CREATE INDEX [IX_CustomerOrderLineOpticalSnapshots_Eye] ON [dbo].[tbl_CustomerOrderLineOpticalSnapshots]([Eye]);

/* ============================================================
   3) Customer demand scheduling
   ============================================================ */
IF COL_LENGTH(N'dbo.tbl_PurchaseRequestLines', N'ScheduledOrderAtUtc') IS NULL
    ALTER TABLE [dbo].[tbl_PurchaseRequestLines] ADD [ScheduledOrderAtUtc] datetimeoffset NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_PurchaseRequestLines_ScheduledOrderAtUtc' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PurchaseRequestLines]'))
    CREATE INDEX [IX_PurchaseRequestLines_ScheduledOrderAtUtc] ON [dbo].[tbl_PurchaseRequestLines]([ScheduledOrderAtUtc]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_PurchaseRequestLines_CustomerOrderLine_ProductVariant' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PurchaseRequestLines]'))
    CREATE INDEX [IX_PurchaseRequestLines_CustomerOrderLine_ProductVariant] ON [dbo].[tbl_PurchaseRequestLines]([CustomerOrderLineId], [ProductVariantId]);

/* ============================================================
   4) Customer advances and applications
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_CustomerAdvances]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_CustomerAdvances]
    (
        [Id] uniqueidentifier NOT NULL,
        [AdvanceNumber] nvarchar(40) NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [CustomerOrderId] uniqueidentifier NOT NULL,
        [ReceiptVoucherLineId] uniqueidentifier NOT NULL,
        [CurrencyId] uniqueidentifier NOT NULL,
        [CurrencyCodeSnapshot] nvarchar(8) NOT NULL,
        [CurrencySymbolSnapshot] nvarchar(12) NULL,
        [CurrencyDecimalPlacesSnapshot] tinyint NOT NULL,
        [BaseCurrencyId] uniqueidentifier NOT NULL,
        [BaseCurrencyCodeSnapshot] nvarchar(8) NOT NULL,
        [BaseCurrencyDecimalPlacesSnapshot] tinyint NOT NULL,
        [Amount] decimal(19,4) NOT NULL,
        [ExchangeRate] decimal(19,8) NOT NULL,
        [ExchangeRateDate] date NOT NULL,
        [ExchangeRateType] tinyint NOT NULL,
        [ExchangeRateSource] tinyint NOT NULL,
        [BaseAmount] decimal(19,4) NOT NULL,
        [AppliedAmount] decimal(19,4) NOT NULL,
        [BaseAppliedAmount] decimal(19,4) NOT NULL,
        [Status] tinyint NOT NULL,
        [ReceivedAtUtc] datetime2(3) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_CustomerAdvances] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerAdvances_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_CustomerAdvances_BaseAmount] CHECK ([BaseAmount] > 0),
        CONSTRAINT [CK_CustomerAdvances_ExchangeRate] CHECK ([ExchangeRate] > 0),
        CONSTRAINT [CK_CustomerAdvances_AppliedAmount] CHECK ([AppliedAmount] >= 0 AND [AppliedAmount] <= [Amount]),
        CONSTRAINT [CK_CustomerAdvances_BaseAppliedAmount] CHECK ([BaseAppliedAmount] >= 0 AND [BaseAppliedAmount] <= [BaseAmount]),
        CONSTRAINT [FK_CustomerAdvances_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[tbl_Customers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerAdvances_CustomerOrders_CustomerOrderId] FOREIGN KEY ([CustomerOrderId]) REFERENCES [dbo].[tbl_CustomerOrders]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerAdvances_ReceiptVoucherLines_ReceiptVoucherLineId] FOREIGN KEY ([ReceiptVoucherLineId]) REFERENCES [dbo].[tbl_ReceiptVoucherLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerAdvances_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerAdvances_BaseCurrencies_BaseCurrencyId] FOREIGN KEY ([BaseCurrencyId]) REFERENCES [dbo].[tbl_Currencies]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_CustomerAdvances_AdvanceNumber' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerAdvances]'))
    CREATE UNIQUE INDEX [UX_CustomerAdvances_AdvanceNumber] ON [dbo].[tbl_CustomerAdvances]([AdvanceNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_CustomerAdvances_ReceiptVoucherLineId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerAdvances]'))
    CREATE UNIQUE INDEX [UX_CustomerAdvances_ReceiptVoucherLineId] ON [dbo].[tbl_CustomerAdvances]([ReceiptVoucherLineId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CustomerAdvances_CustomerId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerAdvances]'))
    CREATE INDEX [IX_CustomerAdvances_CustomerId] ON [dbo].[tbl_CustomerAdvances]([CustomerId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CustomerAdvances_CustomerOrderId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerAdvances]'))
    CREATE INDEX [IX_CustomerAdvances_CustomerOrderId] ON [dbo].[tbl_CustomerAdvances]([CustomerOrderId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CustomerAdvances_Status' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerAdvances]'))
    CREATE INDEX [IX_CustomerAdvances_Status] ON [dbo].[tbl_CustomerAdvances]([Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CustomerAdvances_CurrencyId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerAdvances]'))
    CREATE INDEX [IX_CustomerAdvances_CurrencyId] ON [dbo].[tbl_CustomerAdvances]([CurrencyId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CustomerAdvances_BaseCurrencyId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerAdvances]'))
    CREATE INDEX [IX_CustomerAdvances_BaseCurrencyId] ON [dbo].[tbl_CustomerAdvances]([BaseCurrencyId]);

IF OBJECT_ID(N'[dbo].[tbl_CustomerAdvanceApplications]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_CustomerAdvanceApplications]
    (
        [Id] uniqueidentifier NOT NULL,
        [CustomerAdvanceId] uniqueidentifier NOT NULL,
        [SalesInvoiceId] uniqueidentifier NOT NULL,
        [Amount] decimal(19,4) NOT NULL,
        [BaseAmount] decimal(19,4) NOT NULL,
        [TargetBaseAmount] decimal(19,4) NOT NULL,
        [JournalEntryId] uniqueidentifier NOT NULL,
        [AppliedAtUtc] datetime2(3) NOT NULL,
        [AppliedBy] nvarchar(64) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_CustomerAdvanceApplications] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerAdvanceApplications_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_CustomerAdvanceApplications_BaseAmount] CHECK ([BaseAmount] > 0),
        CONSTRAINT [CK_CustomerAdvanceApplications_TargetBaseAmount] CHECK ([TargetBaseAmount] > 0),
        CONSTRAINT [FK_CustomerAdvanceApplications_CustomerAdvances_CustomerAdvanceId] FOREIGN KEY ([CustomerAdvanceId]) REFERENCES [dbo].[tbl_CustomerAdvances]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerAdvanceApplications_SalesInvoices_SalesInvoiceId] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [dbo].[tbl_SalesInvoices]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerAdvanceApplications_JournalEntries_JournalEntryId] FOREIGN KEY ([JournalEntryId]) REFERENCES [dbo].[tbl_JournalEntries]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CustomerAdvanceApplications_AdvanceId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerAdvanceApplications]'))
    CREATE INDEX [IX_CustomerAdvanceApplications_AdvanceId] ON [dbo].[tbl_CustomerAdvanceApplications]([CustomerAdvanceId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CustomerAdvanceApplications_SalesInvoiceId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerAdvanceApplications]'))
    CREATE INDEX [IX_CustomerAdvanceApplications_SalesInvoiceId] ON [dbo].[tbl_CustomerAdvanceApplications]([SalesInvoiceId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CustomerAdvanceApplications_JournalEntryId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_CustomerAdvanceApplications]'))
    CREATE INDEX [IX_CustomerAdvanceApplications_JournalEntryId] ON [dbo].[tbl_CustomerAdvanceApplications]([JournalEntryId]);

/* Typed source for customer-advance allocations. */
IF COL_LENGTH(N'dbo.tbl_PaymentAllocations', N'CustomerAdvanceApplicationId') IS NULL
    ALTER TABLE [dbo].[tbl_PaymentAllocations] ADD [CustomerAdvanceApplicationId] uniqueidentifier NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_PaymentAllocations_CustomerAdvanceApplicationId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]'))
    EXEC(N'CREATE INDEX [IX_PaymentAllocations_CustomerAdvanceApplicationId] ON [dbo].[tbl_PaymentAllocations]([CustomerAdvanceApplicationId]);');

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_PaymentAllocations_CustomerAdvanceApplication' AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]'))
BEGIN
    EXEC(N'ALTER TABLE [dbo].[tbl_PaymentAllocations] WITH CHECK ADD CONSTRAINT [FK_PaymentAllocations_CustomerAdvanceApplication] FOREIGN KEY ([CustomerAdvanceApplicationId]) REFERENCES [dbo].[tbl_CustomerAdvanceApplications]([Id]) ON DELETE NO ACTION;');
    ALTER TABLE [dbo].[tbl_PaymentAllocations] CHECK CONSTRAINT [FK_PaymentAllocations_CustomerAdvanceApplication];
END;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_PaymentAllocations_TypedSource' AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]'))
    ALTER TABLE [dbo].[tbl_PaymentAllocations] DROP CONSTRAINT [CK_PaymentAllocations_TypedSource];

EXEC(N'ALTER TABLE [dbo].[tbl_PaymentAllocations] WITH CHECK ADD CONSTRAINT [CK_PaymentAllocations_TypedSource] CHECK ((CASE WHEN [ReceiptVoucherLineId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [PaymentVoucherLineId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [CustomerAdvanceApplicationId] IS NULL THEN 0 ELSE 1 END) <= 1);');

/* ============================================================
   5) Order-centric optical job core (finance-free)
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_OpticalJobs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_OpticalJobs]
    (
        [Id] uniqueidentifier NOT NULL,
        [JobCode] nvarchar(40) NOT NULL,
        [CustomerOrderId] uniqueidentifier NOT NULL,
        [SalesInvoiceId] uniqueidentifier NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [RequiredDate] date NULL,
        [Status] tinyint NOT NULL,
        [AssignedTechnicianId] uniqueidentifier NULL,
        [StartedAtUtc] datetimeoffset NULL,
        [CompletedAtUtc] datetimeoffset NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_OpticalJobs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpticalJobs_CustomerOrders_CustomerOrderId] FOREIGN KEY ([CustomerOrderId]) REFERENCES [dbo].[tbl_CustomerOrders]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalJobs_SalesInvoices_SalesInvoiceId] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [dbo].[tbl_SalesInvoices]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalJobs_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[tbl_Customers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalJobs_Employees_AssignedTechnicianId] FOREIGN KEY ([AssignedTechnicianId]) REFERENCES [hr].[Employees]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_OpticalJobs_JobCode' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_OpticalJobs]'))
    CREATE UNIQUE INDEX [UX_OpticalJobs_JobCode] ON [dbo].[tbl_OpticalJobs]([JobCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_OpticalJobs_CustomerOrderId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_OpticalJobs]'))
    CREATE UNIQUE INDEX [UX_OpticalJobs_CustomerOrderId] ON [dbo].[tbl_OpticalJobs]([CustomerOrderId]) WHERE [IsActive] = 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OpticalJobs_Status' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_OpticalJobs]'))
    CREATE INDEX [IX_OpticalJobs_Status] ON [dbo].[tbl_OpticalJobs]([Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OpticalJobs_AssignedTechnicianId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_OpticalJobs]'))
    CREATE INDEX [IX_OpticalJobs_AssignedTechnicianId] ON [dbo].[tbl_OpticalJobs]([AssignedTechnicianId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OpticalJobs_RequiredDate' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_OpticalJobs]'))
    CREATE INDEX [IX_OpticalJobs_RequiredDate] ON [dbo].[tbl_OpticalJobs]([RequiredDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OpticalJobs_SalesInvoiceId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_OpticalJobs]'))
    CREATE INDEX [IX_OpticalJobs_SalesInvoiceId] ON [dbo].[tbl_OpticalJobs]([SalesInvoiceId]);

IF OBJECT_ID(N'[dbo].[tbl_OpticalJobLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_OpticalJobLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [OpticalJobId] uniqueidentifier NOT NULL,
        [CustomerOrderLineId] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NULL,
        [LineNumber] int NOT NULL,
        [LineType] tinyint NOT NULL,
        [Eye] tinyint NULL,
        [DescriptionSnapshot] nvarchar(500) NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL,
        [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_OpticalJobLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OpticalJobLines_LineNumber] CHECK ([LineNumber] > 0),
        CONSTRAINT [CK_OpticalJobLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_OpticalJobLines_OpticalJobs_OpticalJobId] FOREIGN KEY ([OpticalJobId]) REFERENCES [dbo].[tbl_OpticalJobs]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalJobLines_CustomerOrderLines_CustomerOrderLineId] FOREIGN KEY ([CustomerOrderLineId]) REFERENCES [dbo].[tbl_CustomerOrderLines]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OpticalJobLines_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_OpticalJobLines_Job_LineNumber' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_OpticalJobLines]'))
    CREATE UNIQUE INDEX [UX_OpticalJobLines_Job_LineNumber] ON [dbo].[tbl_OpticalJobLines]([OpticalJobId], [LineNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_OpticalJobLines_Job_CustomerOrderLine' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_OpticalJobLines]'))
    CREATE UNIQUE INDEX [UX_OpticalJobLines_Job_CustomerOrderLine] ON [dbo].[tbl_OpticalJobLines]([OpticalJobId], [CustomerOrderLineId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OpticalJobLines_CustomerOrderLineId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_OpticalJobLines]'))
    CREATE INDEX [IX_OpticalJobLines_CustomerOrderLineId] ON [dbo].[tbl_OpticalJobLines]([CustomerOrderLineId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OpticalJobLines_ProductVariantId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_OpticalJobLines]'))
    CREATE INDEX [IX_OpticalJobLines_ProductVariantId] ON [dbo].[tbl_OpticalJobLines]([ProductVariantId]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* Payment allocations depend on customer advance applications. */
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_PaymentAllocations_CustomerAdvanceApplication' AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]'))
    ALTER TABLE [dbo].[tbl_PaymentAllocations] DROP CONSTRAINT [FK_PaymentAllocations_CustomerAdvanceApplication];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_PaymentAllocations_TypedSource' AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]'))
    ALTER TABLE [dbo].[tbl_PaymentAllocations] DROP CONSTRAINT [CK_PaymentAllocations_TypedSource];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_PaymentAllocations_CustomerAdvanceApplicationId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]'))
    DROP INDEX [IX_PaymentAllocations_CustomerAdvanceApplicationId] ON [dbo].[tbl_PaymentAllocations];
IF COL_LENGTH(N'dbo.tbl_PaymentAllocations', N'CustomerAdvanceApplicationId') IS NOT NULL
    ALTER TABLE [dbo].[tbl_PaymentAllocations] DROP COLUMN [CustomerAdvanceApplicationId];
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_PaymentAllocations_TypedSource' AND [parent_object_id] = OBJECT_ID(N'[dbo].[tbl_PaymentAllocations]'))
    ALTER TABLE [dbo].[tbl_PaymentAllocations] WITH CHECK ADD CONSTRAINT [CK_PaymentAllocations_TypedSource]
        CHECK ([ReceiptVoucherLineId] IS NULL OR [PaymentVoucherLineId] IS NULL);

IF OBJECT_ID(N'[dbo].[tbl_OpticalJobLines]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_OpticalJobLines];
IF OBJECT_ID(N'[dbo].[tbl_OpticalJobs]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_OpticalJobs];
IF OBJECT_ID(N'[dbo].[tbl_CustomerAdvanceApplications]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_CustomerAdvanceApplications];
IF OBJECT_ID(N'[dbo].[tbl_CustomerAdvances]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_CustomerAdvances];
IF OBJECT_ID(N'[dbo].[tbl_CustomerOrderLineOpticalSnapshots]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_CustomerOrderLineOpticalSnapshots];
IF OBJECT_ID(N'[dbo].[tbl_LensVariantDetails]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_LensVariantDetails];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_PurchaseRequestLines_CustomerOrderLine_ProductVariant' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PurchaseRequestLines]'))
    DROP INDEX [IX_PurchaseRequestLines_CustomerOrderLine_ProductVariant] ON [dbo].[tbl_PurchaseRequestLines];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_PurchaseRequestLines_ScheduledOrderAtUtc' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_PurchaseRequestLines]'))
    DROP INDEX [IX_PurchaseRequestLines_ScheduledOrderAtUtc] ON [dbo].[tbl_PurchaseRequestLines];
IF COL_LENGTH(N'dbo.tbl_PurchaseRequestLines', N'ScheduledOrderAtUtc') IS NOT NULL
    ALTER TABLE [dbo].[tbl_PurchaseRequestLines] DROP COLUMN [ScheduledOrderAtUtc];
""");
    }
}
