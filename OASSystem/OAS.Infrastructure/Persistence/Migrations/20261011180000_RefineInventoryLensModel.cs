using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261011180000_RefineInventoryLensModel")]
public sealed class RefineInventoryLensModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* ============================================================
   1) Inventory master names: NameAr/NameEn -> Name
   ============================================================ */
IF OBJECT_ID(N'[dbo].[tbl_productTypes]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_productTypes', N'Name') IS NULL
        ALTER TABLE [dbo].[tbl_productTypes] ADD [Name] nvarchar(100) NULL;
    EXEC(N'UPDATE [dbo].[tbl_productTypes] SET [Name] = COALESCE(NULLIF(LTRIM(RTRIM([NameAr])), N''''), NULLIF(LTRIM(RTRIM([NameEn])), N''''), [Code]) WHERE [Name] IS NULL OR LTRIM(RTRIM([Name])) = N'''';');
    ALTER TABLE [dbo].[tbl_productTypes] ALTER COLUMN [Name] nvarchar(100) NOT NULL;
    IF COL_LENGTH(N'dbo.tbl_productTypes', N'NameAr') IS NOT NULL ALTER TABLE [dbo].[tbl_productTypes] DROP COLUMN [NameAr];
    IF COL_LENGTH(N'dbo.tbl_productTypes', N'NameEn') IS NOT NULL ALTER TABLE [dbo].[tbl_productTypes] DROP COLUMN [NameEn];
END;

IF OBJECT_ID(N'[dbo].[tbl_ProductCategories]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_ProductCategories', N'Name') IS NULL
        ALTER TABLE [dbo].[tbl_ProductCategories] ADD [Name] nvarchar(100) NULL;
    EXEC(N'UPDATE [dbo].[tbl_ProductCategories] SET [Name] = COALESCE(NULLIF(LTRIM(RTRIM([NameAr])), N''''), NULLIF(LTRIM(RTRIM([NameEn])), N''''), [Code]) WHERE [Name] IS NULL OR LTRIM(RTRIM([Name])) = N'''';');
    ALTER TABLE [dbo].[tbl_ProductCategories] ALTER COLUMN [Name] nvarchar(100) NOT NULL;

    IF COL_LENGTH(N'dbo.tbl_ProductCategories', N'ParentCategoryId') IS NOT NULL
    BEGIN
        DECLARE @ParentFk sysname;
        SELECT TOP (1) @ParentFk = fk.[name]
        FROM sys.foreign_keys fk
        INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE fk.parent_object_id = OBJECT_ID(N'[dbo].[tbl_ProductCategories]') AND c.[name] = N'ParentCategoryId';
        IF @ParentFk IS NOT NULL EXEC(N'ALTER TABLE [dbo].[tbl_ProductCategories] DROP CONSTRAINT [' + @ParentFk + N']');

        DECLARE @ParentIndex sysname;
        SELECT TOP (1) @ParentIndex = i.[name]
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID(N'[dbo].[tbl_ProductCategories]') AND c.[name] = N'ParentCategoryId' AND i.is_primary_key = 0 AND i.is_unique_constraint = 0;
        IF @ParentIndex IS NOT NULL EXEC(N'DROP INDEX [' + @ParentIndex + N'] ON [dbo].[tbl_ProductCategories]');

        ALTER TABLE [dbo].[tbl_ProductCategories] DROP COLUMN [ParentCategoryId];
    END;
    IF COL_LENGTH(N'dbo.tbl_ProductCategories', N'NameAr') IS NOT NULL ALTER TABLE [dbo].[tbl_ProductCategories] DROP COLUMN [NameAr];
    IF COL_LENGTH(N'dbo.tbl_ProductCategories', N'NameEn') IS NOT NULL ALTER TABLE [dbo].[tbl_ProductCategories] DROP COLUMN [NameEn];
END;

IF OBJECT_ID(N'[dbo].[tbl_Units]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_Units', N'Name') IS NULL ALTER TABLE [dbo].[tbl_Units] ADD [Name] nvarchar(100) NULL;
    EXEC(N'UPDATE [dbo].[tbl_Units] SET [Name] = COALESCE(NULLIF(LTRIM(RTRIM([NameAr])), N''''), NULLIF(LTRIM(RTRIM([NameEn])), N''''), [Code]) WHERE [Name] IS NULL OR LTRIM(RTRIM([Name])) = N'''';');
    ALTER TABLE [dbo].[tbl_Units] ALTER COLUMN [Name] nvarchar(100) NOT NULL;
    IF COL_LENGTH(N'dbo.tbl_Units', N'NameAr') IS NOT NULL ALTER TABLE [dbo].[tbl_Units] DROP COLUMN [NameAr];
    IF COL_LENGTH(N'dbo.tbl_Units', N'NameEn') IS NOT NULL ALTER TABLE [dbo].[tbl_Units] DROP COLUMN [NameEn];
END;

IF OBJECT_ID(N'[dbo].[tbl_Products]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_Products', N'Name') IS NULL ALTER TABLE [dbo].[tbl_Products] ADD [Name] nvarchar(200) NULL;
    EXEC(N'UPDATE [dbo].[tbl_Products] SET [Name] = COALESCE(NULLIF(LTRIM(RTRIM([NameAr])), N''''), NULLIF(LTRIM(RTRIM([NameEn])), N''''), [ProductCode]) WHERE [Name] IS NULL OR LTRIM(RTRIM([Name])) = N'''';');
    ALTER TABLE [dbo].[tbl_Products] ALTER COLUMN [Name] nvarchar(200) NOT NULL;
    IF COL_LENGTH(N'dbo.tbl_Products', N'NameAr') IS NOT NULL ALTER TABLE [dbo].[tbl_Products] DROP COLUMN [NameAr];
    IF COL_LENGTH(N'dbo.tbl_Products', N'NameEn') IS NOT NULL ALTER TABLE [dbo].[tbl_Products] DROP COLUMN [NameEn];
END;

IF OBJECT_ID(N'[dbo].[tbl_Warehouses]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tbl_Warehouses', N'Name') IS NULL ALTER TABLE [dbo].[tbl_Warehouses] ADD [Name] nvarchar(100) NULL;
    EXEC(N'UPDATE [dbo].[tbl_Warehouses] SET [Name] = COALESCE(NULLIF(LTRIM(RTRIM([NameAr])), N''''), NULLIF(LTRIM(RTRIM([NameEn])), N''''), [Code]) WHERE [Name] IS NULL OR LTRIM(RTRIM([Name])) = N'''';');
    ALTER TABLE [dbo].[tbl_Warehouses] ALTER COLUMN [Name] nvarchar(100) NOT NULL;
    IF COL_LENGTH(N'dbo.tbl_Warehouses', N'NameAr') IS NOT NULL ALTER TABLE [dbo].[tbl_Warehouses] DROP COLUMN [NameAr];
    IF COL_LENGTH(N'dbo.tbl_Warehouses', N'NameEn') IS NOT NULL ALTER TABLE [dbo].[tbl_Warehouses] DROP COLUMN [NameEn];
END;

/* ============================================================
   2) Merge product-level LensDetails + exact LensVariantDetails.
      New LensDetails is one row per ProductVariant/SKU.
   ============================================================ */
CREATE TABLE #LensDetailsMerged
(
    [Id] uniqueidentifier NOT NULL,
    [ProductVariantId] uniqueidentifier NOT NULL,
    [LensType] nvarchar(100) NOT NULL,
    [Material] nvarchar(100) NULL,
    [Coating] nvarchar(100) NULL,
    [RefractiveIndex] decimal(5,3) NULL,
    [SPH] decimal(6,2) NULL,
    [CYL] decimal(6,2) NULL,
    [ADD] decimal(6,2) NULL,
    [BaseCurve] decimal(6,2) NULL,
    [Diameter] decimal(6,2) NULL,
    [IsPrescriptionLens] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedBy] nvarchar(64) NULL,
    [LastModifiedAtUtc] datetimeoffset NULL,
    [LastModifiedBy] nvarchar(64) NULL,
    PRIMARY KEY ([ProductVariantId])
);

IF OBJECT_ID(N'[dbo].[tbl_LensDetails]', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'[dbo].[tbl_LensVariantDetails]', N'U') IS NOT NULL
    BEGIN
        INSERT #LensDetailsMerged
        (
            [Id], [ProductVariantId], [LensType], [Material], [Coating], [RefractiveIndex],
            [SPH], [CYL], [ADD], [BaseCurve], [Diameter], [IsPrescriptionLens], [IsActive],
            [CreatedAtUtc], [CreatedBy], [LastModifiedAtUtc], [LastModifiedBy]
        )
        SELECT
            COALESCE(lvd.[Id], NEWID()),
            v.[Id],
            ld.[LensType], ld.[Material], ld.[Coating], ld.[RefractiveIndex],
            lvd.[SPH], lvd.[CYL], lvd.[ADD], lvd.[BaseCurve], lvd.[Diameter],
            ld.[IsPrescriptionLens], COALESCE(lvd.[IsActive], v.[IsActive]),
            COALESCE(lvd.[CreatedAt], SYSDATETIMEOFFSET()), lvd.[CreatedBy], lvd.[UpdatedAt], lvd.[UpdatedBy]
        FROM [dbo].[tbl_LensDetails] ld
        INNER JOIN [dbo].[tbl_ProductVariants] v ON v.[ProductId] = ld.[ProductId]
        LEFT JOIN [dbo].[tbl_LensVariantDetails] lvd ON lvd.[ProductVariantId] = v.[Id];
    END
    ELSE
    BEGIN
        INSERT #LensDetailsMerged
        (
            [Id], [ProductVariantId], [LensType], [Material], [Coating], [RefractiveIndex],
            [SPH], [CYL], [ADD], [BaseCurve], [Diameter], [IsPrescriptionLens], [IsActive],
            [CreatedAtUtc], [CreatedBy], [LastModifiedAtUtc], [LastModifiedBy]
        )
        SELECT
            NEWID(), v.[Id], ld.[LensType], ld.[Material], ld.[Coating], ld.[RefractiveIndex],
            NULL, NULL, NULL, NULL, NULL, ld.[IsPrescriptionLens], v.[IsActive],
            SYSDATETIMEOFFSET(), NULL, NULL, NULL
        FROM [dbo].[tbl_LensDetails] ld
        INNER JOIN [dbo].[tbl_ProductVariants] v ON v.[ProductId] = ld.[ProductId];
    END
END;

IF OBJECT_ID(N'[dbo].[tbl_LensVariantDetails]', N'U') IS NOT NULL
BEGIN
    INSERT #LensDetailsMerged
    (
        [Id], [ProductVariantId], [LensType], [Material], [Coating], [RefractiveIndex],
        [SPH], [CYL], [ADD], [BaseCurve], [Diameter], [IsPrescriptionLens], [IsActive],
        [CreatedAtUtc], [CreatedBy], [LastModifiedAtUtc], [LastModifiedBy]
    )
    SELECT
        lvd.[Id], lvd.[ProductVariantId], N'عدسة', NULL, NULL, NULL,
        lvd.[SPH], lvd.[CYL], lvd.[ADD], lvd.[BaseCurve], lvd.[Diameter],
        CAST(1 AS bit), lvd.[IsActive], lvd.[CreatedAt], lvd.[CreatedBy], lvd.[UpdatedAt], lvd.[UpdatedBy]
    FROM [dbo].[tbl_LensVariantDetails] lvd
    WHERE NOT EXISTS (SELECT 1 FROM #LensDetailsMerged m WHERE m.[ProductVariantId] = lvd.[ProductVariantId]);
END;

IF OBJECT_ID(N'[dbo].[tbl_LensVariantDetails]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_LensVariantDetails];
IF OBJECT_ID(N'[dbo].[tbl_LensDetails]', N'U') IS NOT NULL DROP TABLE [dbo].[tbl_LensDetails];

CREATE TABLE [dbo].[tbl_LensDetails]
(
    [Id] uniqueidentifier NOT NULL,
    [ProductVariantId] uniqueidentifier NOT NULL,
    [LensType] nvarchar(100) NOT NULL,
    [Material] nvarchar(100) NULL,
    [Coating] nvarchar(100) NULL,
    [RefractiveIndex] decimal(5,3) NULL,
    [SPH] decimal(6,2) NULL,
    [CYL] decimal(6,2) NULL,
    [ADD] decimal(6,2) NULL,
    [BaseCurve] decimal(6,2) NULL,
    [Diameter] decimal(6,2) NULL,
    [IsPrescriptionLens] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedBy] nvarchar(64) NULL,
    [LastModifiedAtUtc] datetimeoffset NULL,
    [LastModifiedBy] nvarchar(64) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_tbl_LensDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_LensDetails_ADD] CHECK ([ADD] IS NULL OR [ADD] >= 0),
    CONSTRAINT [CK_LensDetails_BaseCurve] CHECK ([BaseCurve] IS NULL OR [BaseCurve] > 0),
    CONSTRAINT [CK_LensDetails_Diameter] CHECK ([Diameter] IS NULL OR [Diameter] > 0),
    CONSTRAINT [FK_LensDetails_ProductVariants_ProductVariantId]
        FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id]) ON DELETE CASCADE
);
CREATE UNIQUE INDEX [UX_LensDetails_ProductVariantId] ON [dbo].[tbl_LensDetails]([ProductVariantId]);
CREATE INDEX [IX_LensDetails_SPH_CYL_ADD] ON [dbo].[tbl_LensDetails]([SPH], [CYL], [ADD]);

INSERT [dbo].[tbl_LensDetails]
(
    [Id], [ProductVariantId], [LensType], [Material], [Coating], [RefractiveIndex],
    [SPH], [CYL], [ADD], [BaseCurve], [Diameter], [IsPrescriptionLens], [IsActive],
    [CreatedAtUtc], [CreatedBy], [LastModifiedAtUtc], [LastModifiedBy]
)
SELECT
    [Id], [ProductVariantId], [LensType], [Material], [Coating], [RefractiveIndex],
    [SPH], [CYL], [ADD], [BaseCurve], [Diameter], [IsPrescriptionLens], [IsActive],
    [CreatedAtUtc], [CreatedBy], [LastModifiedAtUtc], [LastModifiedBy]
FROM #LensDetailsMerged;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/* Down keeps direct measurements by restoring them as Min=Max. */
IF OBJECT_ID(N'[dbo].[tbl_LensVariantDetails]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tbl_LensVariantDetails]
    (
        [Id] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [SPH] decimal(6,2) NULL, [CYL] decimal(6,2) NULL, [ADD] decimal(6,2) NULL,
        [BaseCurve] decimal(6,2) NULL, [Diameter] decimal(6,2) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL, [CreatedBy] nvarchar(64) NULL,
        [UpdatedAt] datetimeoffset NULL, [UpdatedBy] nvarchar(64) NULL,
        [CreatedFromDevice] nvarchar(256) NULL, [UpdatedFromDevice] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_LensVariantDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LensVariantDetails_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[tbl_ProductVariants]([Id])
    );
    CREATE UNIQUE INDEX [UX_LensVariantDetails_ProductVariantId] ON [dbo].[tbl_LensVariantDetails]([ProductVariantId]);
END;

INSERT [dbo].[tbl_LensVariantDetails]
([Id],[ProductVariantId],[SPH],[CYL],[ADD],[BaseCurve],[Diameter],[IsActive],[CreatedAt],[CreatedBy],[UpdatedAt],[UpdatedBy])
SELECT [Id],[ProductVariantId],[SPH],[CYL],[ADD],[BaseCurve],[Diameter],[IsActive],[CreatedAtUtc],[CreatedBy],[LastModifiedAtUtc],[LastModifiedBy]
FROM [dbo].[tbl_LensDetails] d
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[tbl_LensVariantDetails] x WHERE x.[ProductVariantId]=d.[ProductVariantId]);

SELECT * INTO #NewLens FROM [dbo].[tbl_LensDetails];
DROP TABLE [dbo].[tbl_LensDetails];
CREATE TABLE [dbo].[tbl_LensDetails]
(
    [Id] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [LensType] nvarchar(100) NOT NULL,
    [Material] nvarchar(100) NULL,
    [Coating] nvarchar(100) NULL,
    [RefractiveIndex] decimal(5,3) NULL,
    [SphereMin] decimal(6,2) NULL, [SphereMax] decimal(6,2) NULL,
    [CylinderMin] decimal(6,2) NULL, [CylinderMax] decimal(6,2) NULL,
    [AddMin] decimal(6,2) NULL, [AddMax] decimal(6,2) NULL,
    [IsPrescriptionLens] bit NOT NULL,
    CONSTRAINT [PK_tbl_LensDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_tbl_LensDetails_tbl_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[tbl_Products]([Id]) ON DELETE CASCADE
);
INSERT [dbo].[tbl_LensDetails]
([Id],[ProductId],[LensType],[Material],[Coating],[RefractiveIndex],[SphereMin],[SphereMax],[CylinderMin],[CylinderMax],[AddMin],[AddMax],[IsPrescriptionLens])
SELECT NEWID(), v.[ProductId], MAX(d.[LensType]), MAX(d.[Material]), MAX(d.[Coating]), MAX(d.[RefractiveIndex]),
       MIN(d.[SPH]), MAX(d.[SPH]), MIN(d.[CYL]), MAX(d.[CYL]), MIN(d.[ADD]), MAX(d.[ADD]), MAX(CONVERT(int,d.[IsPrescriptionLens]))
FROM #NewLens d INNER JOIN [dbo].[tbl_ProductVariants] v ON v.[Id]=d.[ProductVariantId]
GROUP BY v.[ProductId];
CREATE UNIQUE INDEX [UX_tbl_LensDetails_ProductId] ON [dbo].[tbl_LensDetails]([ProductId]);

/* Recreate legacy inventory name columns from Name. */
IF COL_LENGTH(N'dbo.tbl_productTypes', N'NameAr') IS NULL ALTER TABLE [dbo].[tbl_productTypes] ADD [NameAr] nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.tbl_productTypes', N'NameEn') IS NULL ALTER TABLE [dbo].[tbl_productTypes] ADD [NameEn] nvarchar(100) NULL;
UPDATE [dbo].[tbl_productTypes] SET [NameAr]=[Name]; ALTER TABLE [dbo].[tbl_productTypes] ALTER COLUMN [NameAr] nvarchar(100) NOT NULL;
IF COL_LENGTH(N'dbo.tbl_productTypes', N'Name') IS NOT NULL ALTER TABLE [dbo].[tbl_productTypes] DROP COLUMN [Name];

IF COL_LENGTH(N'dbo.tbl_ProductCategories', N'NameAr') IS NULL ALTER TABLE [dbo].[tbl_ProductCategories] ADD [NameAr] nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.tbl_ProductCategories', N'NameEn') IS NULL ALTER TABLE [dbo].[tbl_ProductCategories] ADD [NameEn] nvarchar(100) NULL;
UPDATE [dbo].[tbl_ProductCategories] SET [NameAr]=[Name]; ALTER TABLE [dbo].[tbl_ProductCategories] ALTER COLUMN [NameAr] nvarchar(100) NOT NULL;
IF COL_LENGTH(N'dbo.tbl_ProductCategories', N'ParentCategoryId') IS NULL ALTER TABLE [dbo].[tbl_ProductCategories] ADD [ParentCategoryId] uniqueidentifier NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_ProductCategories_ParentCategoryId' AND [object_id] = OBJECT_ID(N'[dbo].[tbl_ProductCategories]'))
    CREATE INDEX [IX_ProductCategories_ParentCategoryId] ON [dbo].[tbl_ProductCategories]([ParentCategoryId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_ProductCategories_ParentCategory')
    ALTER TABLE [dbo].[tbl_ProductCategories] ADD CONSTRAINT [FK_ProductCategories_ParentCategory]
        FOREIGN KEY ([ParentCategoryId]) REFERENCES [dbo].[tbl_ProductCategories]([Id]);
IF COL_LENGTH(N'dbo.tbl_ProductCategories', N'Name') IS NOT NULL ALTER TABLE [dbo].[tbl_ProductCategories] DROP COLUMN [Name];

IF COL_LENGTH(N'dbo.tbl_Units', N'NameAr') IS NULL ALTER TABLE [dbo].[tbl_Units] ADD [NameAr] nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.tbl_Units', N'NameEn') IS NULL ALTER TABLE [dbo].[tbl_Units] ADD [NameEn] nvarchar(100) NULL;
UPDATE [dbo].[tbl_Units] SET [NameAr]=[Name]; ALTER TABLE [dbo].[tbl_Units] ALTER COLUMN [NameAr] nvarchar(100) NOT NULL;
IF COL_LENGTH(N'dbo.tbl_Units', N'Name') IS NOT NULL ALTER TABLE [dbo].[tbl_Units] DROP COLUMN [Name];

IF COL_LENGTH(N'dbo.tbl_Products', N'NameAr') IS NULL ALTER TABLE [dbo].[tbl_Products] ADD [NameAr] nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.tbl_Products', N'NameEn') IS NULL ALTER TABLE [dbo].[tbl_Products] ADD [NameEn] nvarchar(200) NULL;
UPDATE [dbo].[tbl_Products] SET [NameAr]=[Name]; ALTER TABLE [dbo].[tbl_Products] ALTER COLUMN [NameAr] nvarchar(200) NOT NULL;
IF COL_LENGTH(N'dbo.tbl_Products', N'Name') IS NOT NULL ALTER TABLE [dbo].[tbl_Products] DROP COLUMN [Name];

IF COL_LENGTH(N'dbo.tbl_Warehouses', N'NameAr') IS NULL ALTER TABLE [dbo].[tbl_Warehouses] ADD [NameAr] nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.tbl_Warehouses', N'NameEn') IS NULL ALTER TABLE [dbo].[tbl_Warehouses] ADD [NameEn] nvarchar(100) NULL;
UPDATE [dbo].[tbl_Warehouses] SET [NameAr]=[Name]; ALTER TABLE [dbo].[tbl_Warehouses] ALTER COLUMN [NameAr] nvarchar(100) NOT NULL;
IF COL_LENGTH(N'dbo.tbl_Warehouses', N'Name') IS NOT NULL ALTER TABLE [dbo].[tbl_Warehouses] DROP COLUMN [Name];
""");
    }
}
