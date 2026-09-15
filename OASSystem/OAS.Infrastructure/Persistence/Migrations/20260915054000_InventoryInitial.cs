using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20260915054000_InventoryInitial")]
public sealed class InventoryInitial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_ProductCategories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_ProductCategories]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(100) NOT NULL,
        [NameEn] nvarchar(100) NULL,
        [ParentCategoryId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,

        CONSTRAINT [PK_tbl_ProductCategories]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_ProductCategories_ParentCategory]
            FOREIGN KEY ([ParentCategoryId])
            REFERENCES [dbo].[tbl_ProductCategories]([Id])
            ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_Brands]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_Brands]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,

        CONSTRAINT [PK_tbl_Brands]
            PRIMARY KEY ([Id])
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_Units]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_Units]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(100) NOT NULL,
        [NameEn] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,

        CONSTRAINT [PK_tbl_Units]
            PRIMARY KEY ([Id])
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_Products]
    (
        [Id] uniqueidentifier NOT NULL,
        [ProductCode] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NULL,
        [CategoryId] uniqueidentifier NOT NULL,
        [BrandId] uniqueidentifier NULL,
        [ProductType] nvarchar(32) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [IsStockItem] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_Products]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_Products_tbl_ProductCategories_CategoryId]
            FOREIGN KEY ([CategoryId])
            REFERENCES [dbo].[tbl_ProductCategories]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_tbl_Products_tbl_Brands_BrandId]
            FOREIGN KEY ([BrandId])
            REFERENCES [dbo].[tbl_Brands]([Id])
            ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_ProductVariants]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_ProductVariants]
    (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [SKU] nvarchar(64) NOT NULL,
        [Barcode] nvarchar(64) NULL,
        [VariantName] nvarchar(100) NULL,
        [Color] nvarchar(50) NULL,
        [Size] nvarchar(50) NULL,
        [UnitId] uniqueidentifier NULL,
        [PurchasePrice] decimal(18,2) NOT NULL,
        [SellingPrice] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_ProductVariants]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_ProductVariants_tbl_Products_ProductId]
            FOREIGN KEY ([ProductId])
            REFERENCES [dbo].[tbl_Products]([Id])
            ON DELETE CASCADE,

        CONSTRAINT [FK_tbl_ProductVariants_tbl_Units_UnitId]
            FOREIGN KEY ([UnitId])
            REFERENCES [dbo].[tbl_Units]([Id])
            ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_FrameDetails]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_FrameDetails]
    (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Model] nvarchar(100) NOT NULL,
        [Material] nvarchar(100) NULL,
        [RimType] nvarchar(50) NULL,
        [Gender] nvarchar(50) NULL,
        [Shape] nvarchar(50) NULL,
        [TempleLength] decimal(6,2) NULL,
        [BridgeSize] decimal(6,2) NULL,
        [LensWidth] decimal(6,2) NULL,

        CONSTRAINT [PK_tbl_FrameDetails]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_FrameDetails_tbl_Products_ProductId]
            FOREIGN KEY ([ProductId])
            REFERENCES [dbo].[tbl_Products]([Id])
            ON DELETE CASCADE
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_LensDetails]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_LensDetails]
    (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [LensType] nvarchar(100) NOT NULL,
        [Material] nvarchar(100) NULL,
        [Coating] nvarchar(100) NULL,
        [RefractiveIndex] decimal(5,3) NULL,
        [SphereMin] decimal(6,2) NULL,
        [SphereMax] decimal(6,2) NULL,
        [CylinderMin] decimal(6,2) NULL,
        [CylinderMax] decimal(6,2) NULL,
        [AddMin] decimal(6,2) NULL,
        [AddMax] decimal(6,2) NULL,
        [IsPrescriptionLens] bit NOT NULL,

        CONSTRAINT [PK_tbl_LensDetails]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_LensDetails_tbl_Products_ProductId]
            FOREIGN KEY ([ProductId])
            REFERENCES [dbo].[tbl_Products]([Id])
            ON DELETE CASCADE
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_Warehouses]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_Warehouses]
    (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(100) NOT NULL,
        [NameEn] nvarchar(100) NULL,
        [Description] nvarchar(500) NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_Warehouses]
            PRIMARY KEY ([Id])
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_InventoryBalances]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_InventoryBalances]
    (
        [Id] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [OnHandQuantity] decimal(18,3) NOT NULL,
        [ReservedQuantity] decimal(18,3) NOT NULL,
        [OnOrderQuantity] decimal(18,3) NOT NULL,
        [AverageUnitCost] decimal(18,2) NOT NULL,
        [InventoryValue] decimal(18,2) NOT NULL,
        [LastMovementAtUtc] datetimeoffset NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_InventoryBalances]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_InventoryBalances_tbl_Warehouses_WarehouseId]
            FOREIGN KEY ([WarehouseId])
            REFERENCES [dbo].[tbl_Warehouses]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_tbl_InventoryBalances_tbl_ProductVariants_ProductVariantId]
            FOREIGN KEY ([ProductVariantId])
            REFERENCES [dbo].[tbl_ProductVariants]([Id])
            ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_InventoryTransactions]
    (
        [Id] uniqueidentifier NOT NULL,
        [TransactionNumber] nvarchar(32) NOT NULL,
        [TransactionType] nvarchar(32) NOT NULL,
        [SourceWarehouseId] uniqueidentifier NULL,
        [DestinationWarehouseId] uniqueidentifier NULL,
        [Status] nvarchar(32) NOT NULL,
        [TransactionDate] datetimeoffset NOT NULL,
        [ReferenceType] nvarchar(64) NULL,
        [ReferenceId] uniqueidentifier NULL,
        [Reason] nvarchar(500) NULL,
        [Notes] nvarchar(1000) NULL,
        [PostedAtUtc] datetimeoffset NULL,
        [PostedBy] nvarchar(64) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_InventoryTransactions]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_InventoryTransactions_SourceWarehouse]
            FOREIGN KEY ([SourceWarehouseId])
            REFERENCES [dbo].[tbl_Warehouses]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_tbl_InventoryTransactions_DestinationWarehouse]
            FOREIGN KEY ([DestinationWarehouseId])
            REFERENCES [dbo].[tbl_Warehouses]([Id])
            ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_InventoryTransactionLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_InventoryTransactionLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [TransactionId] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [UnitCost] decimal(18,2) NOT NULL,
        [TotalCost] decimal(18,2) NOT NULL,
        [Notes] nvarchar(500) NULL,

        CONSTRAINT [PK_tbl_InventoryTransactionLines]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_InventoryTransactionLines_tbl_InventoryTransactions_TransactionId]
            FOREIGN KEY ([TransactionId])
            REFERENCES [dbo].[tbl_InventoryTransactions]([Id])
            ON DELETE CASCADE,

        CONSTRAINT [FK_tbl_InventoryTransactionLines_tbl_ProductVariants_ProductVariantId]
            FOREIGN KEY ([ProductVariantId])
            REFERENCES [dbo].[tbl_ProductVariants]([Id])
            ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_InventoryLedger]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_InventoryLedger]
    (
        [Id] uniqueidentifier NOT NULL,
        [SequenceNumber] bigint NOT NULL,
        [TransactionId] uniqueidentifier NOT NULL,
        [TransactionLineId] uniqueidentifier NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [MovementType] nvarchar(32) NOT NULL,
        [QuantityIn] decimal(18,3) NOT NULL,
        [QuantityOut] decimal(18,3) NOT NULL,
        [BalanceAfter] decimal(18,3) NOT NULL,
        [UnitCost] decimal(18,2) NOT NULL,
        [AverageCostAfter] decimal(18,2) NOT NULL,
        [InventoryValueAfter] decimal(18,2) NOT NULL,
        [MovementDate] datetimeoffset NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,

        CONSTRAINT [PK_tbl_InventoryLedger]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_InventoryLedger_tbl_InventoryTransactions_TransactionId]
            FOREIGN KEY ([TransactionId])
            REFERENCES [dbo].[tbl_InventoryTransactions]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_tbl_InventoryLedger_tbl_InventoryTransactionLines_TransactionLineId]
            FOREIGN KEY ([TransactionLineId])
            REFERENCES [dbo].[tbl_InventoryTransactionLines]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_tbl_InventoryLedger_tbl_Warehouses_WarehouseId]
            FOREIGN KEY ([WarehouseId])
            REFERENCES [dbo].[tbl_Warehouses]([Id])
            ON DELETE NO ACTION,

        CONSTRAINT [FK_tbl_InventoryLedger_tbl_ProductVariants_ProductVariantId]
            FOREIGN KEY ([ProductVariantId])
            REFERENCES [dbo].[tbl_ProductVariants]([Id])
            ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_StockCounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_StockCounts]
    (
        [Id] uniqueidentifier NOT NULL,
        [CountNumber] nvarchar(32) NOT NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [CountDate] date NOT NULL,
        [StartedAtUtc] datetimeoffset NULL,
        [CompletedAtUtc] datetimeoffset NULL,
        [ApprovedAtUtc] datetimeoffset NULL,
        [ApprovedBy] nvarchar(64) NULL,
        [PostedAtUtc] datetimeoffset NULL,
        [PostedBy] nvarchar(64) NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(64) NULL,
        [LastModifiedAtUtc] datetimeoffset NULL,
        [LastModifiedBy] nvarchar(64) NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_StockCounts]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_StockCounts_tbl_Warehouses_WarehouseId]
            FOREIGN KEY ([WarehouseId])
            REFERENCES [dbo].[tbl_Warehouses]([Id])
            ON DELETE NO ACTION
    );
END;
""");

        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_StockCountLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_StockCountLines]
    (
        [Id] uniqueidentifier NOT NULL,
        [StockCountId] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [SystemQuantity] decimal(18,3) NOT NULL,
        [CountedQuantity] decimal(18,3) NOT NULL,
        [DifferenceQuantity] decimal(18,3) NOT NULL,
        [AverageCostSnapshot] decimal(18,2) NOT NULL,
        [VarianceValue] decimal(18,2) NOT NULL,
        [CountedAtUtc] datetimeoffset NULL,
        [CountedBy] nvarchar(64) NULL,
        [Notes] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,

        CONSTRAINT [PK_tbl_StockCountLines]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_tbl_StockCountLines_tbl_StockCounts_StockCountId]
            FOREIGN KEY ([StockCountId])
            REFERENCES [dbo].[tbl_StockCounts]([Id])
            ON DELETE CASCADE,

        CONSTRAINT [FK_tbl_StockCountLines_tbl_ProductVariants_ProductVariantId]
            FOREIGN KEY ([ProductVariantId])
            REFERENCES [dbo].[tbl_ProductVariants]([Id])
            ON DELETE NO ACTION
    );
END;
""");

        // Indexes

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ProductCategories]')
      AND name = N'UX_tbl_ProductCategories_Code'
)
CREATE UNIQUE INDEX [UX_tbl_ProductCategories_Code]
    ON [dbo].[tbl_ProductCategories]([Code]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ProductCategories]')
      AND name = N'IX_tbl_ProductCategories_ParentCategoryId'
)
CREATE INDEX [IX_tbl_ProductCategories_ParentCategoryId]
    ON [dbo].[tbl_ProductCategories]([ParentCategoryId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ProductCategories]')
      AND name = N'IX_tbl_ProductCategories_IsActive'
)
CREATE INDEX [IX_tbl_ProductCategories_IsActive]
    ON [dbo].[tbl_ProductCategories]([IsActive]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Brands]')
      AND name = N'UX_tbl_Brands_Code'
)
CREATE UNIQUE INDEX [UX_tbl_Brands_Code]
    ON [dbo].[tbl_Brands]([Code]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Brands]')
      AND name = N'UX_tbl_Brands_Name'
)
CREATE UNIQUE INDEX [UX_tbl_Brands_Name]
    ON [dbo].[tbl_Brands]([Name]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Brands]')
      AND name = N'IX_tbl_Brands_IsActive'
)
CREATE INDEX [IX_tbl_Brands_IsActive]
    ON [dbo].[tbl_Brands]([IsActive]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Units]')
      AND name = N'UX_tbl_Units_Code'
)
CREATE UNIQUE INDEX [UX_tbl_Units_Code]
    ON [dbo].[tbl_Units]([Code]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Units]')
      AND name = N'IX_tbl_Units_IsActive'
)
CREATE INDEX [IX_tbl_Units_IsActive]
    ON [dbo].[tbl_Units]([IsActive]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
      AND name = N'UX_tbl_Products_ProductCode'
)
CREATE UNIQUE INDEX [UX_tbl_Products_ProductCode]
    ON [dbo].[tbl_Products]([ProductCode]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
      AND name = N'IX_tbl_Products_CategoryId'
)
CREATE INDEX [IX_tbl_Products_CategoryId]
    ON [dbo].[tbl_Products]([CategoryId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
      AND name = N'IX_tbl_Products_BrandId'
)
CREATE INDEX [IX_tbl_Products_BrandId]
    ON [dbo].[tbl_Products]([BrandId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Products]')
      AND name = N'IX_tbl_Products_IsActive'
)
CREATE INDEX [IX_tbl_Products_IsActive]
    ON [dbo].[tbl_Products]([IsActive]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ProductVariants]')
      AND name = N'UX_tbl_ProductVariants_SKU'
)
CREATE UNIQUE INDEX [UX_tbl_ProductVariants_SKU]
    ON [dbo].[tbl_ProductVariants]([SKU]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ProductVariants]')
      AND name = N'UX_tbl_ProductVariants_Barcode'
)
CREATE UNIQUE INDEX [UX_tbl_ProductVariants_Barcode]
    ON [dbo].[tbl_ProductVariants]([Barcode])
    WHERE [Barcode] IS NOT NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ProductVariants]')
      AND name = N'IX_tbl_ProductVariants_ProductId'
)
CREATE INDEX [IX_tbl_ProductVariants_ProductId]
    ON [dbo].[tbl_ProductVariants]([ProductId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ProductVariants]')
      AND name = N'IX_tbl_ProductVariants_IsActive'
)
CREATE INDEX [IX_tbl_ProductVariants_IsActive]
    ON [dbo].[tbl_ProductVariants]([IsActive]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_FrameDetails]')
      AND name = N'UX_tbl_FrameDetails_ProductId'
)
CREATE UNIQUE INDEX [UX_tbl_FrameDetails_ProductId]
    ON [dbo].[tbl_FrameDetails]([ProductId]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_LensDetails]')
      AND name = N'UX_tbl_LensDetails_ProductId'
)
CREATE UNIQUE INDEX [UX_tbl_LensDetails_ProductId]
    ON [dbo].[tbl_LensDetails]([ProductId]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Warehouses]')
      AND name = N'UX_tbl_Warehouses_Code'
)
CREATE UNIQUE INDEX [UX_tbl_Warehouses_Code]
    ON [dbo].[tbl_Warehouses]([Code]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Warehouses]')
      AND name = N'IX_tbl_Warehouses_IsActive'
)
CREATE INDEX [IX_tbl_Warehouses_IsActive]
    ON [dbo].[tbl_Warehouses]([IsActive]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Warehouses]')
      AND name = N'IX_tbl_Warehouses_IsDefault'
)
CREATE INDEX [IX_tbl_Warehouses_IsDefault]
    ON [dbo].[tbl_Warehouses]([IsDefault]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryBalances]')
      AND name = N'UX_tbl_InventoryBalances_Warehouse_ProductVariant'
)
CREATE UNIQUE INDEX [UX_tbl_InventoryBalances_Warehouse_ProductVariant]
    ON [dbo].[tbl_InventoryBalances]([WarehouseId], [ProductVariantId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryBalances]')
      AND name = N'IX_tbl_InventoryBalances_ProductVariantId'
)
CREATE INDEX [IX_tbl_InventoryBalances_ProductVariantId]
    ON [dbo].[tbl_InventoryBalances]([ProductVariantId]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]')
      AND name = N'UX_tbl_InventoryTransactions_TransactionNumber'
)
CREATE UNIQUE INDEX [UX_tbl_InventoryTransactions_TransactionNumber]
    ON [dbo].[tbl_InventoryTransactions]([TransactionNumber]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]')
      AND name = N'IX_tbl_InventoryTransactions_TransactionDate'
)
CREATE INDEX [IX_tbl_InventoryTransactions_TransactionDate]
    ON [dbo].[tbl_InventoryTransactions]([TransactionDate]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]')
      AND name = N'IX_tbl_InventoryTransactions_Status'
)
CREATE INDEX [IX_tbl_InventoryTransactions_Status]
    ON [dbo].[tbl_InventoryTransactions]([Status]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]')
      AND name = N'IX_tbl_InventoryTransactions_SourceWarehouseId'
)
CREATE INDEX [IX_tbl_InventoryTransactions_SourceWarehouseId]
    ON [dbo].[tbl_InventoryTransactions]([SourceWarehouseId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]')
      AND name = N'IX_tbl_InventoryTransactions_DestinationWarehouseId'
)
CREATE INDEX [IX_tbl_InventoryTransactions_DestinationWarehouseId]
    ON [dbo].[tbl_InventoryTransactions]([DestinationWarehouseId]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactionLines]')
      AND name = N'IX_tbl_InventoryTransactionLines_TransactionId'
)
CREATE INDEX [IX_tbl_InventoryTransactionLines_TransactionId]
    ON [dbo].[tbl_InventoryTransactionLines]([TransactionId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryTransactionLines]')
      AND name = N'IX_tbl_InventoryTransactionLines_ProductVariantId'
)
CREATE INDEX [IX_tbl_InventoryTransactionLines_ProductVariantId]
    ON [dbo].[tbl_InventoryTransactionLines]([ProductVariantId]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryLedger]')
      AND name = N'UX_tbl_InventoryLedger_SequenceNumber'
)
CREATE UNIQUE INDEX [UX_tbl_InventoryLedger_SequenceNumber]
    ON [dbo].[tbl_InventoryLedger]([SequenceNumber]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryLedger]')
      AND name = N'IX_tbl_InventoryLedger_Warehouse_ProductVariant_Date'
)
CREATE INDEX [IX_tbl_InventoryLedger_Warehouse_ProductVariant_Date]
    ON [dbo].[tbl_InventoryLedger]([WarehouseId], [ProductVariantId], [MovementDate]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryLedger]')
      AND name = N'IX_tbl_InventoryLedger_TransactionId'
)
CREATE INDEX [IX_tbl_InventoryLedger_TransactionId]
    ON [dbo].[tbl_InventoryLedger]([TransactionId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryLedger]')
      AND name = N'IX_tbl_InventoryLedger_TransactionLineId'
)
CREATE INDEX [IX_tbl_InventoryLedger_TransactionLineId]
    ON [dbo].[tbl_InventoryLedger]([TransactionLineId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_InventoryLedger]')
      AND name = N'IX_tbl_InventoryLedger_ProductVariantId'
)
CREATE INDEX [IX_tbl_InventoryLedger_ProductVariantId]
    ON [dbo].[tbl_InventoryLedger]([ProductVariantId]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]')
      AND name = N'UX_tbl_StockCounts_CountNumber'
)
CREATE UNIQUE INDEX [UX_tbl_StockCounts_CountNumber]
    ON [dbo].[tbl_StockCounts]([CountNumber]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]')
      AND name = N'IX_tbl_StockCounts_WarehouseId'
)
CREATE INDEX [IX_tbl_StockCounts_WarehouseId]
    ON [dbo].[tbl_StockCounts]([WarehouseId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]')
      AND name = N'IX_tbl_StockCounts_Status'
)
CREATE INDEX [IX_tbl_StockCounts_Status]
    ON [dbo].[tbl_StockCounts]([Status]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCounts]')
      AND name = N'IX_tbl_StockCounts_CountDate'
)
CREATE INDEX [IX_tbl_StockCounts_CountDate]
    ON [dbo].[tbl_StockCounts]([CountDate]);
""");

        migrationBuilder.Sql("""
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCountLines]')
      AND name = N'UX_tbl_StockCountLines_StockCount_ProductVariant'
)
CREATE UNIQUE INDEX [UX_tbl_StockCountLines_StockCount_ProductVariant]
    ON [dbo].[tbl_StockCountLines]([StockCountId], [ProductVariantId]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StockCountLines]')
      AND name = N'IX_tbl_StockCountLines_ProductVariantId'
)
CREATE INDEX [IX_tbl_StockCountLines_ProductVariantId]
    ON [dbo].[tbl_StockCountLines]([ProductVariantId]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[tbl_StockCountLines]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_StockCountLines];

IF OBJECT_ID(N'[dbo].[tbl_StockCounts]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_StockCounts];

IF OBJECT_ID(N'[dbo].[tbl_InventoryLedger]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_InventoryLedger];

IF OBJECT_ID(N'[dbo].[tbl_InventoryTransactionLines]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_InventoryTransactionLines];

IF OBJECT_ID(N'[dbo].[tbl_InventoryTransactions]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_InventoryTransactions];

IF OBJECT_ID(N'[dbo].[tbl_InventoryBalances]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_InventoryBalances];

IF OBJECT_ID(N'[dbo].[tbl_Warehouses]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_Warehouses];

IF OBJECT_ID(N'[dbo].[tbl_LensDetails]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_LensDetails];

IF OBJECT_ID(N'[dbo].[tbl_FrameDetails]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_FrameDetails];

IF OBJECT_ID(N'[dbo].[tbl_ProductVariants]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_ProductVariants];

IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_Products];

IF OBJECT_ID(N'[dbo].[tbl_Units]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_Units];

IF OBJECT_ID(N'[dbo].[tbl_Brands]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_Brands];

IF OBJECT_ID(N'[dbo].[tbl_ProductCategories]', N'U') IS NOT NULL
    DROP TABLE [dbo].[tbl_ProductCategories];
""");
    }
}