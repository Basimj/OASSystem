/*
    OAS Inventory V1 - Test Data Seed
    Target: SQL Server
    Scope: Inventory tables only

    IMPORTANT:
    - Run EF migrations first, including 20260923070000_AddProductTypesLookup.
    - This script expects the Inventory tables to be empty.
    - Inventory enum columns are inserted as INT, matching Domain/Contracts/EF configuration.

    Enum values:
      InventoryTransactionType:   Opening=1, Receipt=2, Issue=3, Transfer=4,
                                  AdjustmentIncrease=5, AdjustmentDecrease=6,
                                  SalesReturn=7, PurchaseReturn=8, ProductionIssue=9, Scrap=10
      InventoryTransactionStatus: Draft=1, Posted=2
      InventoryMovementType:      In=1, Out=2
      StockCountStatus:           Draft=1, Counting=2, Review=3, Approved=4, Posted=5, Cancelled=6
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    ---------------------------------------------------------------------------
    -- 0) Preflight: required schema/tables and Inventory schema alignment
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.tbl_ProductCategories', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_Brands', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_Units', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_productTypes', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_Products', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_ProductVariants', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_FrameDetails', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_LensDetails', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_Warehouses', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_InventoryBalances', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_InventoryTransactions', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_InventoryTransactionLines', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_InventoryLedger', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_StockCounts', N'U') IS NULL
       OR OBJECT_ID(N'dbo.tbl_StockCountLines', N'U') IS NULL
    BEGIN
        THROW 52000, 'Inventory tables are missing. Apply the OAS migrations before running this seed.', 1;
    END;

    IF COL_LENGTH(N'dbo.tbl_ProductVariants', N'CreatedAtUtc') IS NULL
       OR COL_LENGTH(N'dbo.tbl_ProductVariants', N'CreatedBy') IS NULL
       OR COL_LENGTH(N'dbo.tbl_ProductVariants', N'LastModifiedAtUtc') IS NULL
       OR COL_LENGTH(N'dbo.tbl_ProductVariants', N'LastModifiedBy') IS NULL
    BEGIN
        THROW 52001, 'InventorySchemaAlignment is not applied: ProductVariant audit columns are missing.', 1;
    END;

    IF COL_LENGTH(N'dbo.tbl_Products', N'ProductType') IS NOT NULL
       OR COL_LENGTH(N'dbo.tbl_Products', N'ProductTypeId') IS NULL
    BEGIN
        THROW 52004, 'Product type schema is not aligned. Apply RemoveProductTypeFromProducts and AddProductTypesLookup first.', 1;
    END;

    IF (SELECT COUNT(*) FROM dbo.tbl_productTypes WHERE Code IN (N'FRAME', N'LENS', N'SUNGLASSES', N'ACCESSORY', N'OTHER', N'SERVICE')) < 6
    BEGIN
        THROW 52005, 'Required product type lookup rows are missing from dbo.tbl_productTypes.', 1;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE (c.object_id = OBJECT_ID(N'dbo.tbl_InventoryTransactions') AND c.name IN (N'TransactionType', N'Status') AND t.name <> N'int')
           OR (c.object_id = OBJECT_ID(N'dbo.tbl_InventoryLedger') AND c.name = N'MovementType' AND t.name <> N'int')
           OR (c.object_id = OBJECT_ID(N'dbo.tbl_StockCounts') AND c.name = N'Status' AND t.name <> N'int')
    )
    BEGIN
        THROW 52002, 'Inventory enum columns are not INT. Apply InventorySchemaAlignment before running this seed.', 1;
    END;

    -- Safe re-run behavior: do not duplicate or overwrite real Inventory data.
    IF EXISTS (SELECT 1 FROM dbo.tbl_Products WHERE ProductCode = N'PRD-FRM-RB001')
    BEGIN
        PRINT N'OAS Inventory demo data already exists. No changes were made.';
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.tbl_ProductCategories)
       OR EXISTS (SELECT 1 FROM dbo.tbl_Brands)
       OR EXISTS (SELECT 1 FROM dbo.tbl_Units)
       OR EXISTS (SELECT 1 FROM dbo.tbl_Products)
       OR EXISTS (SELECT 1 FROM dbo.tbl_ProductVariants)
       OR EXISTS (SELECT 1 FROM dbo.tbl_FrameDetails)
       OR EXISTS (SELECT 1 FROM dbo.tbl_LensDetails)
       OR EXISTS (SELECT 1 FROM dbo.tbl_Warehouses)
       OR EXISTS (SELECT 1 FROM dbo.tbl_InventoryBalances)
       OR EXISTS (SELECT 1 FROM dbo.tbl_InventoryTransactions)
       OR EXISTS (SELECT 1 FROM dbo.tbl_InventoryTransactionLines)
       OR EXISTS (SELECT 1 FROM dbo.tbl_InventoryLedger)
       OR EXISTS (SELECT 1 FROM dbo.tbl_StockCounts)
       OR EXISTS (SELECT 1 FROM dbo.tbl_StockCountLines)
    BEGIN
        THROW 52003, 'Inventory tables are not empty. This seed intentionally refuses to overwrite existing Inventory data.', 1;
    END;

    DECLARE @Now datetimeoffset(7) = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');
    DECLARE @SeedUser nvarchar(64) = N'inventory-seed';

    DECLARE @D14 datetimeoffset(7) = DATEADD(day, -14, @Now);
    DECLARE @D12 datetimeoffset(7) = DATEADD(day, -12, @Now);
    DECLARE @D10 datetimeoffset(7) = DATEADD(day, -10, @Now);
    DECLARE @D8  datetimeoffset(7) = DATEADD(day, -8,  @Now);
    DECLARE @D6  datetimeoffset(7) = DATEADD(day, -6,  @Now);
    DECLARE @D4  datetimeoffset(7) = DATEADD(day, -4,  @Now);
    DECLARE @D2  datetimeoffset(7) = DATEADD(day, -2,  @Now);

    ---------------------------------------------------------------------------
    -- Stable IDs: Categories
    ---------------------------------------------------------------------------
    DECLARE @CatEyewear     uniqueidentifier = '10000000-0000-0000-0000-000000000001';
    DECLARE @CatFrames      uniqueidentifier = '10000000-0000-0000-0000-000000000002';
    DECLARE @CatLenses      uniqueidentifier = '10000000-0000-0000-0000-000000000003';
    DECLARE @CatSunglasses  uniqueidentifier = '10000000-0000-0000-0000-000000000004';
    DECLARE @CatAccessories uniqueidentifier = '10000000-0000-0000-0000-000000000005';
    DECLARE @CatServices    uniqueidentifier = '10000000-0000-0000-0000-000000000006';

    INSERT dbo.tbl_ProductCategories
        (Id, Code, NameAr, NameEn, ParentCategoryId, IsActive, CreatedAtUtc, CreatedBy, LastModifiedAtUtc, LastModifiedBy)
    VALUES
        (@CatEyewear,     N'EYEWEAR',     N'النظارات والعدسات', N'Eyewear',     NULL,        1, @D14, @SeedUser, NULL, NULL),
        (@CatFrames,      N'FRAMES',      N'الإطارات',          N'Frames',      @CatEyewear, 1, @D14, @SeedUser, NULL, NULL),
        (@CatLenses,      N'LENSES',      N'العدسات',           N'Lenses',      @CatEyewear, 1, @D14, @SeedUser, NULL, NULL),
        (@CatSunglasses,  N'SUNGLASSES',  N'النظارات الشمسية', N'Sunglasses',  @CatEyewear, 1, @D14, @SeedUser, NULL, NULL),
        (@CatAccessories, N'ACCESSORIES', N'الإكسسوارات',       N'Accessories', NULL,        1, @D14, @SeedUser, NULL, NULL),
        (@CatServices,    N'SERVICES',    N'الخدمات',           N'Services',    NULL,        1, @D14, @SeedUser, NULL, NULL);

    ---------------------------------------------------------------------------
    -- Brands
    ---------------------------------------------------------------------------
    DECLARE @BrandRayBan  uniqueidentifier = '20000000-0000-0000-0000-000000000001';
    DECLARE @BrandOakley  uniqueidentifier = '20000000-0000-0000-0000-000000000002';
    DECLARE @BrandEssilor uniqueidentifier = '20000000-0000-0000-0000-000000000003';
    DECLARE @BrandHoya    uniqueidentifier = '20000000-0000-0000-0000-000000000004';
    DECLARE @BrandGeneric uniqueidentifier = '20000000-0000-0000-0000-000000000005';

    INSERT dbo.tbl_Brands
        (Id, Code, Name, IsActive, CreatedAtUtc, CreatedBy, LastModifiedAtUtc, LastModifiedBy)
    VALUES
        (@BrandRayBan,  N'RAYBAN',  N'Ray-Ban', 1, @D14, @SeedUser, NULL, NULL),
        (@BrandOakley,  N'OAKLEY',  N'Oakley',  1, @D14, @SeedUser, NULL, NULL),
        (@BrandEssilor, N'ESSILOR', N'Essilor', 1, @D14, @SeedUser, NULL, NULL),
        (@BrandHoya,    N'HOYA',    N'Hoya',    1, @D14, @SeedUser, NULL, NULL),
        (@BrandGeneric, N'GENERIC', N'Generic', 1, @D14, @SeedUser, NULL, NULL);

    ---------------------------------------------------------------------------
    -- Units
    ---------------------------------------------------------------------------
    DECLARE @UnitPiece uniqueidentifier = '30000000-0000-0000-0000-000000000001';
    DECLARE @UnitPair  uniqueidentifier = '30000000-0000-0000-0000-000000000002';
    DECLARE @UnitBox   uniqueidentifier = '30000000-0000-0000-0000-000000000003';

    INSERT dbo.tbl_Units
        (Id, Code, NameAr, NameEn, IsActive, CreatedAtUtc, CreatedBy, LastModifiedAtUtc, LastModifiedBy)
    VALUES
        (@UnitPiece, N'PCS',  N'قطعة', N'Piece', 1, @D14, @SeedUser, NULL, NULL),
        (@UnitPair,  N'PAIR', N'زوج',  N'Pair',  1, @D14, @SeedUser, NULL, NULL),
        (@UnitBox,   N'BOX',  N'علبة', N'Box',   1, @D14, @SeedUser, NULL, NULL);

    ---------------------------------------------------------------------------
    -- Product types (seeded by AddProductTypesLookup migration)
    ---------------------------------------------------------------------------
    DECLARE @TypeFrame      uniqueidentifier = (SELECT Id FROM dbo.tbl_productTypes WHERE Code = N'FRAME');
    DECLARE @TypeLens       uniqueidentifier = (SELECT Id FROM dbo.tbl_productTypes WHERE Code = N'LENS');
    DECLARE @TypeSunglasses uniqueidentifier = (SELECT Id FROM dbo.tbl_productTypes WHERE Code = N'SUNGLASSES');
    DECLARE @TypeAccessory  uniqueidentifier = (SELECT Id FROM dbo.tbl_productTypes WHERE Code = N'ACCESSORY');
    DECLARE @TypeOther      uniqueidentifier = (SELECT Id FROM dbo.tbl_productTypes WHERE Code = N'OTHER');
    DECLARE @TypeService    uniqueidentifier = (SELECT Id FROM dbo.tbl_productTypes WHERE Code = N'SERVICE');

    ---------------------------------------------------------------------------
    -- Products across the seeded categories
    ---------------------------------------------------------------------------
    DECLARE @ProdFrame      uniqueidentifier = '40000000-0000-0000-0000-000000000001';
    DECLARE @ProdLens       uniqueidentifier = '40000000-0000-0000-0000-000000000002';
    DECLARE @ProdSunglasses uniqueidentifier = '40000000-0000-0000-0000-000000000003';
    DECLARE @ProdAccessory  uniqueidentifier = '40000000-0000-0000-0000-000000000004';
    DECLARE @ProdOther      uniqueidentifier = '40000000-0000-0000-0000-000000000005';
    DECLARE @ProdService    uniqueidentifier = '40000000-0000-0000-0000-000000000006';

    INSERT dbo.tbl_Products
        (Id, ProductCode, NameAr, NameEn, CategoryId, BrandId, ProductTypeId, Description,
         IsStockItem, IsActive, CreatedAtUtc, CreatedBy, LastModifiedAtUtc, LastModifiedBy)
    VALUES
        (@ProdFrame,      N'PRD-FRM-RB001', N'إطار راي بان كلاسيك',       N'Ray-Ban Classic Frame', @CatFrames,       @BrandRayBan,  @TypeFrame,      N'إطار طبي للاختبار متعدد المتغيرات.', 1, 1, @D14, @SeedUser, NULL, NULL),
        (@ProdLens,       N'PRD-LNS-ES001', N'عدسة إسيلور أحادية الرؤية', N'Essilor Single Vision', @CatLenses,       @BrandEssilor, @TypeLens,       N'عدسة طبية بدرجات انكسار متعددة.',      1, 1, @D14, @SeedUser, NULL, NULL),
        (@ProdSunglasses, N'PRD-SUN-OK001', N'نظارة أوكلي شمسية',         N'Oakley Sunglasses',     @CatSunglasses,  @BrandOakley,  @TypeSunglasses,N'نظارة شمسية مع تفاصيل إطار.',           1, 1, @D14, @SeedUser, NULL, NULL),
        (@ProdAccessory,  N'PRD-ACC-CL001', N'بخاخ تنظيف العدسات',        N'Lens Cleaning Spray',   @CatAccessories, @BrandGeneric, @TypeAccessory, N'عبوة تنظيف 60 مل.',                      1, 1, @D14, @SeedUser, NULL, NULL),
        (@ProdOther,      N'PRD-OTH-CS001', N'حافظة نظارات',              N'Glasses Case',          @CatAccessories, @BrandGeneric, @TypeOther,     N'حافظة صلبة للنظارات.',                  1, 1, @D14, @SeedUser, NULL, NULL),
        (@ProdService,    N'PRD-SRV-EX001', N'فحص نظر',                   N'Eye Examination',       @CatServices,    NULL,          @TypeService,   N'خدمة غير مخزنية للاختبار.',            0, 1, @D14, @SeedUser, NULL, NULL);

    ---------------------------------------------------------------------------
    -- Product Variants / SKU / Barcode / Prices
    ---------------------------------------------------------------------------
    DECLARE @VFrameBlack      uniqueidentifier = '50000000-0000-0000-0000-000000000001';
    DECLARE @VFrameTortoise   uniqueidentifier = '50000000-0000-0000-0000-000000000002';
    DECLARE @VSunglassesBlack uniqueidentifier = '50000000-0000-0000-0000-000000000003';
    DECLARE @VLens156         uniqueidentifier = '50000000-0000-0000-0000-000000000004';
    DECLARE @VLens167         uniqueidentifier = '50000000-0000-0000-0000-000000000005';
    DECLARE @VCleaner60       uniqueidentifier = '50000000-0000-0000-0000-000000000006';
    DECLARE @VCaseStandard    uniqueidentifier = '50000000-0000-0000-0000-000000000007';
    DECLARE @VEyeExam         uniqueidentifier = '50000000-0000-0000-0000-000000000008';

    INSERT dbo.tbl_ProductVariants
        (Id, ProductId, SKU, Barcode, VariantName, Color, Size, UnitId, PurchasePrice, SellingPrice,
         IsActive, CreatedAtUtc, CreatedBy, LastModifiedAtUtc, LastModifiedBy)
    VALUES
        (@VFrameBlack,      @ProdFrame,      N'RB-CL-52-BLK', N'6281000000011', N'أسود 52',       N'أسود',  N'52',       @UnitPiece, 45.00,  90.00, 1, @D14, @SeedUser, NULL, NULL),
        (@VFrameTortoise,   @ProdFrame,      N'RB-CL-54-TRT', N'6281000000012', N'سلحفائي 54',    N'سلحفائي',N'54',       @UnitPiece, 48.00,  95.00, 1, @D14, @SeedUser, NULL, NULL),
        (@VSunglassesBlack, @ProdSunglasses, N'OK-SUN-001',   N'6281000000013', N'أسود قياسي',    N'أسود',  N'Standard', @UnitPiece, 70.00, 140.00, 1, @D14, @SeedUser, NULL, NULL),
        (@VLens156,         @ProdLens,       N'ES-SV-156',    N'6281000000014', N'Index 1.56',     NULL,     N'1.56',      @UnitPair,  15.00,  35.00, 1, @D14, @SeedUser, NULL, NULL),
        (@VLens167,         @ProdLens,       N'ES-SV-167',    N'6281000000015', N'Index 1.67',     NULL,     N'1.67',      @UnitPair,  25.00,  55.00, 1, @D14, @SeedUser, NULL, NULL),
        (@VCleaner60,       @ProdAccessory,  N'CLN-SPR-60',   N'6281000000016', N'60 ml',          NULL,     N'60 ml',     @UnitPiece,  3.00,   8.00, 1, @D14, @SeedUser, NULL, NULL),
        (@VCaseStandard,    @ProdOther,      N'CASE-STD-001', N'6281000000017', N'قياسي',          N'أسود',  N'Standard', @UnitPiece,  2.00,   6.00, 1, @D14, @SeedUser, NULL, NULL),
        (@VEyeExam,         @ProdService,    N'SRV-EYE-EXAM', NULL,             N'فحص قياسي',      NULL,     NULL,        @UnitPiece,  0.00,  25.00, 1, @D14, @SeedUser, NULL, NULL);

    ---------------------------------------------------------------------------
    -- Specialized product details
    ---------------------------------------------------------------------------
    INSERT dbo.tbl_FrameDetails
        (Id, ProductId, Model, Material, RimType, Gender, Shape, TempleLength, BridgeSize, LensWidth)
    VALUES
        ('60000000-0000-0000-0000-000000000001', @ProdFrame,      N'RB-Classic-01', N'Acetate', N'Full Rim', N'Unisex', N'Rectangle', 140.00, 18.00, 52.00),
        ('60000000-0000-0000-0000-000000000002', @ProdSunglasses, N'OK-Sun-01',     N'O-Matter',N'Full Rim', N'Unisex', N'Sport',     138.00, 17.00, 55.00);

    INSERT dbo.tbl_LensDetails
        (Id, ProductId, LensType, Material, Coating, RefractiveIndex, SphereMin, SphereMax,
         CylinderMin, CylinderMax, AddMin, AddMax, IsPrescriptionLens)
    VALUES
        ('61000000-0000-0000-0000-000000000001', @ProdLens, N'Single Vision', N'CR-39 / Resin', N'Anti-Reflective', 1.560,
         -10.00, 6.00, -4.00, 4.00, 0.00, 3.50, 1);

    ---------------------------------------------------------------------------
    -- Warehouses
    ---------------------------------------------------------------------------
    DECLARE @WhMain   uniqueidentifier = '70000000-0000-0000-0000-000000000001';
    DECLARE @WhBranch uniqueidentifier = '70000000-0000-0000-0000-000000000002';
    DECLARE @WhLab    uniqueidentifier = '70000000-0000-0000-0000-000000000003';

    INSERT dbo.tbl_Warehouses
        (Id, Code, NameAr, NameEn, Description, IsDefault, IsActive,
         CreatedAtUtc, CreatedBy, LastModifiedAtUtc, LastModifiedBy)
    VALUES
        (@WhMain,   N'WH-MAIN',   N'المخزن الرئيسي', N'Main Warehouse',   N'المخزن الافتراضي للفرع الرئيسي.', 1, 1, @D14, @SeedUser, NULL, NULL),
        (@WhBranch, N'WH-BRANCH', N'مخزن الفرع',     N'Branch Warehouse', N'مخزن اختبار للتحويل بين المخازن.', 0, 1, @D14, @SeedUser, NULL, NULL),
        (@WhLab,    N'WH-LAB',    N'مخزن المختبر',   N'Lab Warehouse',    N'مخزن غير فعال لاختبار حالة التفعيل.', 0, 0, @D14, @SeedUser, NULL, NULL);

    ---------------------------------------------------------------------------
    -- Inventory transactions (all 10 transaction types)
    -- Posted: 1,2,3,4,5,7,10. Draft/test-posting: 6,8,9.
    ---------------------------------------------------------------------------
    DECLARE @T01 uniqueidentifier = '90000000-0000-0000-0000-000000000001'; -- Opening
    DECLARE @T02 uniqueidentifier = '90000000-0000-0000-0000-000000000002'; -- Receipt
    DECLARE @T03 uniqueidentifier = '90000000-0000-0000-0000-000000000003'; -- Issue
    DECLARE @T04 uniqueidentifier = '90000000-0000-0000-0000-000000000004'; -- Transfer
    DECLARE @T05 uniqueidentifier = '90000000-0000-0000-0000-000000000005'; -- Adj Increase
    DECLARE @T06 uniqueidentifier = '90000000-0000-0000-0000-000000000006'; -- Adj Decrease (Draft)
    DECLARE @T07 uniqueidentifier = '90000000-0000-0000-0000-000000000007'; -- Sales Return
    DECLARE @T08 uniqueidentifier = '90000000-0000-0000-0000-000000000008'; -- Purchase Return (Draft)
    DECLARE @T09 uniqueidentifier = '90000000-0000-0000-0000-000000000009'; -- Production Issue (Draft)
    DECLARE @T10 uniqueidentifier = '90000000-0000-0000-0000-000000000010'; -- Scrap

    INSERT dbo.tbl_InventoryTransactions
        (Id, TransactionNumber, TransactionType, SourceWarehouseId, DestinationWarehouseId, Status,
         TransactionDate, ReferenceType, ReferenceId, Reason, Notes, PostedAtUtc, PostedBy,
         CreatedAtUtc, CreatedBy, LastModifiedAtUtc, LastModifiedBy)
    VALUES
        (@T01, N'TXN-DEMO-OPEN-001', 1, NULL,    @WhMain,   2, @D14, N'Seed', NULL, N'رصيد افتتاحي',               N'حركة افتتاحية لاختبار الواجهة.',         DATEADD(minute, 5, @D14), @SeedUser, @D14, @SeedUser, NULL, NULL),
        (@T02, N'TXN-DEMO-REC-001',  2, NULL,    @WhMain,   2, @D12, N'Seed', NULL, N'استلام مشتريات تجريبي',      N'إدخال مخزون.',                            DATEADD(minute, 5, @D12), @SeedUser, @D12, @SeedUser, NULL, NULL),
        (@T03, N'TXN-DEMO-ISS-001',  3, @WhMain, NULL,      2, @D10, N'Seed', NULL, N'صرف داخلي تجريبي',           N'إخراج مخزون.',                            DATEADD(minute, 5, @D10), @SeedUser, @D10, @SeedUser, NULL, NULL),
        (@T04, N'TXN-DEMO-TRF-001',  4, @WhMain, @WhBranch, 2, @D8, N'Seed', NULL, N'تغذية مخزن الفرع',            N'تحويل بين المخازن.',                      DATEADD(minute, 5, @D8),  @SeedUser, @D8,  @SeedUser, NULL, NULL),
        (@T05, N'TXN-DEMO-AIN-001',  5, NULL,    @WhMain,   2, @D6, N'Seed', NULL, N'زيادة جرد تجريبية',           N'Adjustment Increase.',                    DATEADD(minute, 5, @D6),  @SeedUser, @D6,  @SeedUser, NULL, NULL),
        (@T06, N'TXN-DEMO-AOUT-001', 6, @WhMain, NULL,      1, @Now, N'Seed', NULL, N'نقص تجريبي بانتظار الترحيل', N'مسودة جاهزة لاختبار زر Post.',             NULL, NULL, @Now, @SeedUser, NULL, NULL),
        (@T07, N'TXN-DEMO-SRET-001', 7, NULL,    @WhBranch, 2, @D4, N'Seed', NULL, N'مرتجع مبيعات تجريبي',         N'Sales Return.',                            DATEADD(minute, 5, @D4),  @SeedUser, @D4,  @SeedUser, NULL, NULL),
        (@T08, N'TXN-DEMO-PRET-001', 8, @WhMain, NULL,      1, @Now, N'Seed', NULL, N'مرتجع شراء تجريبي',          N'مسودة لاختبار Purchase Return.',           NULL, NULL, @Now, @SeedUser, NULL, NULL),
        (@T09, N'TXN-DEMO-PROD-001', 9, @WhMain, NULL,      1, @Now, N'Seed', NULL, N'صرف إنتاج تجريبي',           N'مسودة لاختبار Production Issue.',         NULL, NULL, @Now, @SeedUser, NULL, NULL),
        (@T10, N'TXN-DEMO-SCRAP-001',10,@WhMain, NULL,      2, @D2, N'Seed', NULL, N'تالف تجريبي',                 N'خروج تالف / Scrap.',                      DATEADD(minute, 5, @D2),  @SeedUser, @D2,  @SeedUser, NULL, NULL);

    ---------------------------------------------------------------------------
    -- Transaction lines
    ---------------------------------------------------------------------------
    DECLARE @L01 uniqueidentifier = 'A0000000-0000-0000-0000-000000000001';
    DECLARE @L02 uniqueidentifier = 'A0000000-0000-0000-0000-000000000002';
    DECLARE @L03 uniqueidentifier = 'A0000000-0000-0000-0000-000000000003';
    DECLARE @L04 uniqueidentifier = 'A0000000-0000-0000-0000-000000000004';
    DECLARE @L05 uniqueidentifier = 'A0000000-0000-0000-0000-000000000005';
    DECLARE @L06 uniqueidentifier = 'A0000000-0000-0000-0000-000000000006';
    DECLARE @L07 uniqueidentifier = 'A0000000-0000-0000-0000-000000000007';
    DECLARE @L08 uniqueidentifier = 'A0000000-0000-0000-0000-000000000008';
    DECLARE @L09 uniqueidentifier = 'A0000000-0000-0000-0000-000000000009';
    DECLARE @L10 uniqueidentifier = 'A0000000-0000-0000-0000-000000000010';
    DECLARE @L11 uniqueidentifier = 'A0000000-0000-0000-0000-000000000011';
    DECLARE @L12 uniqueidentifier = 'A0000000-0000-0000-0000-000000000012';
    DECLARE @L13 uniqueidentifier = 'A0000000-0000-0000-0000-000000000013';
    DECLARE @L14 uniqueidentifier = 'A0000000-0000-0000-0000-000000000014';
    DECLARE @L15 uniqueidentifier = 'A0000000-0000-0000-0000-000000000015';
    DECLARE @L16 uniqueidentifier = 'A0000000-0000-0000-0000-000000000016';
    DECLARE @L17 uniqueidentifier = 'A0000000-0000-0000-0000-000000000017';
    DECLARE @L18 uniqueidentifier = 'A0000000-0000-0000-0000-000000000018';

    INSERT dbo.tbl_InventoryTransactionLines
        (Id, TransactionId, ProductVariantId, Quantity, UnitCost, TotalCost, Notes)
    VALUES
        (@L01, @T01, @VFrameBlack,      37.000, 45.00, 1665.00, N'Opening - Frame Black'),
        (@L02, @T01, @VFrameTortoise,   12.000, 48.00,  576.00, N'Opening - Frame Tortoise'),
        (@L03, @T01, @VLens156,         65.000, 15.00,  975.00, N'Opening - Lens 1.56'),
        (@L04, @T01, @VLens167,         18.000, 25.00,  450.00, N'Opening - Lens 1.67'),
        (@L05, @T01, @VCaseStandard,    22.000,  2.00,   44.00, N'Opening - Cases'),
        (@L06, @T02, @VSunglassesBlack, 10.000, 70.00,  700.00, N'Receipt - Sunglasses'),
        (@L07, @T03, @VFrameBlack,       5.000, 45.00,  225.00, N'Issue - Frame Black'),
        (@L08, @T03, @VLens156,         10.000, 15.00,  150.00, N'Issue - Lens 1.56'),
        (@L09, @T04, @VFrameBlack,       7.000, 45.00,  315.00, N'Transfer - Frame Black'),
        (@L10, @T04, @VSunglassesBlack,  2.000, 70.00,  140.00, N'Transfer - Sunglasses'),
        (@L11, @T04, @VLens156,         15.000, 15.00,  225.00, N'Transfer - Lens 1.56'),
        (@L12, @T05, @VCleaner60,       30.000,  3.00,   90.00, N'Adjustment Increase - Cleaner'),
        (@L13, @T06, @VFrameTortoise,    2.000, 48.00,   96.00, N'Draft Adjustment Decrease'),
        (@L14, @T07, @VSunglassesBlack,  3.000, 70.00,  210.00, N'Sales Return - Sunglasses'),
        (@L15, @T07, @VCleaner60,       10.000,  3.00,   30.00, N'Sales Return - Cleaner'),
        (@L16, @T08, @VLens156,          4.000, 15.00,   60.00, N'Draft Purchase Return'),
        (@L17, @T09, @VLens156,          5.000, 15.00,   75.00, N'Draft Production Issue'),
        (@L18, @T10, @VCaseStandard,     2.000,  2.00,    4.00, N'Scrap - Damaged cases');

    ---------------------------------------------------------------------------
    -- Inventory Ledger: consistent with posted transactions above
    ---------------------------------------------------------------------------
    INSERT dbo.tbl_InventoryLedger
        (Id, SequenceNumber, TransactionId, TransactionLineId, WarehouseId, ProductVariantId,
         MovementType, QuantityIn, QuantityOut, BalanceAfter, UnitCost, AverageCostAfter,
         InventoryValueAfter, MovementDate, CreatedAtUtc, CreatedBy)
    VALUES
        ('B0000000-0000-0000-0000-000000000001',  1, @T01, @L01, @WhMain,   @VFrameBlack,      1, 37.000, 0.000, 37.000, 45.00, 45.00, 1665.00, @D14, @D14, @SeedUser),
        ('B0000000-0000-0000-0000-000000000002',  2, @T01, @L02, @WhMain,   @VFrameTortoise,   1, 12.000, 0.000, 12.000, 48.00, 48.00,  576.00, @D14, @D14, @SeedUser),
        ('B0000000-0000-0000-0000-000000000003',  3, @T01, @L03, @WhMain,   @VLens156,         1, 65.000, 0.000, 65.000, 15.00, 15.00,  975.00, @D14, @D14, @SeedUser),
        ('B0000000-0000-0000-0000-000000000004',  4, @T01, @L04, @WhMain,   @VLens167,         1, 18.000, 0.000, 18.000, 25.00, 25.00,  450.00, @D14, @D14, @SeedUser),
        ('B0000000-0000-0000-0000-000000000005',  5, @T01, @L05, @WhMain,   @VCaseStandard,    1, 22.000, 0.000, 22.000,  2.00,  2.00,   44.00, @D14, @D14, @SeedUser),
        ('B0000000-0000-0000-0000-000000000006',  6, @T02, @L06, @WhMain,   @VSunglassesBlack, 1, 10.000, 0.000, 10.000, 70.00, 70.00,  700.00, @D12, @D12, @SeedUser),
        ('B0000000-0000-0000-0000-000000000007',  7, @T03, @L07, @WhMain,   @VFrameBlack,      2,  0.000, 5.000, 32.000, 45.00, 45.00, 1440.00, @D10, @D10, @SeedUser),
        ('B0000000-0000-0000-0000-000000000008',  8, @T03, @L08, @WhMain,   @VLens156,         2,  0.000,10.000, 55.000, 15.00, 15.00,  825.00, @D10, @D10, @SeedUser),
        ('B0000000-0000-0000-0000-000000000009',  9, @T04, @L09, @WhMain,   @VFrameBlack,      2,  0.000, 7.000, 25.000, 45.00, 45.00, 1125.00, @D8,  @D8,  @SeedUser),
        ('B0000000-0000-0000-0000-000000000010', 10, @T04, @L09, @WhBranch, @VFrameBlack,      1,  7.000, 0.000,  7.000, 45.00, 45.00,  315.00, @D8,  @D8,  @SeedUser),
        ('B0000000-0000-0000-0000-000000000011', 11, @T04, @L10, @WhMain,   @VSunglassesBlack, 2,  0.000, 2.000,  8.000, 70.00, 70.00,  560.00, @D8,  @D8,  @SeedUser),
        ('B0000000-0000-0000-0000-000000000012', 12, @T04, @L10, @WhBranch, @VSunglassesBlack, 1,  2.000, 0.000,  2.000, 70.00, 70.00,  140.00, @D8,  @D8,  @SeedUser),
        ('B0000000-0000-0000-0000-000000000013', 13, @T04, @L11, @WhMain,   @VLens156,         2,  0.000,15.000, 40.000, 15.00, 15.00,  600.00, @D8,  @D8,  @SeedUser),
        ('B0000000-0000-0000-0000-000000000014', 14, @T04, @L11, @WhBranch, @VLens156,         1, 15.000, 0.000, 15.000, 15.00, 15.00,  225.00, @D8,  @D8,  @SeedUser),
        ('B0000000-0000-0000-0000-000000000015', 15, @T05, @L12, @WhMain,   @VCleaner60,       1, 30.000, 0.000, 30.000,  3.00,  3.00,   90.00, @D6,  @D6,  @SeedUser),
        ('B0000000-0000-0000-0000-000000000016', 16, @T07, @L14, @WhBranch, @VSunglassesBlack, 1,  3.000, 0.000,  5.000, 70.00, 70.00,  350.00, @D4,  @D4,  @SeedUser),
        ('B0000000-0000-0000-0000-000000000017', 17, @T07, @L15, @WhBranch, @VCleaner60,       1, 10.000, 0.000, 10.000,  3.00,  3.00,   30.00, @D4,  @D4,  @SeedUser),
        ('B0000000-0000-0000-0000-000000000018', 18, @T10, @L18, @WhMain,   @VCaseStandard,    2,  0.000, 2.000, 20.000,  2.00,  2.00,   40.00, @D2,  @D2,  @SeedUser);

    ---------------------------------------------------------------------------
    -- Current Inventory Balances (derived from posted demo movements)
    -- Reserved/OnOrder are intentionally populated to test Available and filters.
    ---------------------------------------------------------------------------
    INSERT dbo.tbl_InventoryBalances
        (Id, WarehouseId, ProductVariantId, OnHandQuantity, ReservedQuantity, OnOrderQuantity,
         AverageUnitCost, InventoryValue, LastMovementAtUtc,
         CreatedAtUtc, CreatedBy, LastModifiedAtUtc, LastModifiedBy)
    VALUES
        ('80000000-0000-0000-0000-000000000001', @WhMain,   @VFrameBlack,      25.000, 3.000, 10.000, 45.00, 1125.00, @D8,  @D14, @SeedUser, @D8, @SeedUser),
        ('80000000-0000-0000-0000-000000000002', @WhMain,   @VFrameTortoise,   12.000, 1.000,  5.000, 48.00,  576.00, @D14, @D14, @SeedUser, NULL, NULL),
        ('80000000-0000-0000-0000-000000000003', @WhMain,   @VSunglassesBlack,  8.000, 1.000,  4.000, 70.00,  560.00, @D8,  @D12, @SeedUser, @D8, @SeedUser),
        ('80000000-0000-0000-0000-000000000004', @WhMain,   @VLens156,         40.000, 5.000, 20.000, 15.00,  600.00, @D8,  @D14, @SeedUser, @D8, @SeedUser),
        ('80000000-0000-0000-0000-000000000005', @WhMain,   @VLens167,         18.000, 2.000, 10.000, 25.00,  450.00, @D14, @D14, @SeedUser, NULL, NULL),
        ('80000000-0000-0000-0000-000000000006', @WhMain,   @VCleaner60,       30.000, 0.000, 15.000,  3.00,   90.00, @D6,  @D6,  @SeedUser, @D6, @SeedUser),
        ('80000000-0000-0000-0000-000000000007', @WhMain,   @VCaseStandard,    20.000, 2.000,  0.000,  2.00,   40.00, @D2,  @D14, @SeedUser, @D2, @SeedUser),
        ('80000000-0000-0000-0000-000000000008', @WhBranch, @VFrameBlack,       7.000, 0.000,  0.000, 45.00,  315.00, @D8,  @D8,  @SeedUser, NULL, NULL),
        ('80000000-0000-0000-0000-000000000009', @WhBranch, @VSunglassesBlack,  5.000, 1.000,  2.000, 70.00,  350.00, @D4,  @D8,  @SeedUser, @D4, @SeedUser),
        ('80000000-0000-0000-0000-000000000010', @WhBranch, @VLens156,         15.000, 2.000,  5.000, 15.00,  225.00, @D8,  @D8,  @SeedUser, NULL, NULL),
        ('80000000-0000-0000-0000-000000000011', @WhBranch, @VCleaner60,       10.000, 0.000,  0.000,  3.00,   30.00, @D4,  @D4,  @SeedUser, NULL, NULL);

    ---------------------------------------------------------------------------
    -- Stock Counts: one example for every workflow status
    ---------------------------------------------------------------------------
    DECLARE @SC_Draft     uniqueidentifier = 'C0000000-0000-0000-0000-000000000001';
    DECLARE @SC_Counting  uniqueidentifier = 'C0000000-0000-0000-0000-000000000002';
    DECLARE @SC_Review    uniqueidentifier = 'C0000000-0000-0000-0000-000000000003';
    DECLARE @SC_Approved  uniqueidentifier = 'C0000000-0000-0000-0000-000000000004';
    DECLARE @SC_Posted    uniqueidentifier = 'C0000000-0000-0000-0000-000000000005';
    DECLARE @SC_Cancelled uniqueidentifier = 'C0000000-0000-0000-0000-000000000006';

    INSERT dbo.tbl_StockCounts
        (Id, CountNumber, WarehouseId, Status, CountDate, StartedAtUtc, CompletedAtUtc,
         ApprovedAtUtc, ApprovedBy, PostedAtUtc, PostedBy, Notes,
         CreatedAtUtc, CreatedBy, LastModifiedAtUtc, LastModifiedBy)
    VALUES
        (@SC_Draft,     N'SC-DEMO-DRAFT-001',    @WhMain,   1, CAST(@Now AS date), NULL, NULL, NULL, NULL, NULL, NULL, N'مسودة: اختبر Start Counting.', @Now, @SeedUser, NULL, NULL),
        (@SC_Counting,  N'SC-DEMO-COUNT-001',    @WhMain,   2, CAST(DATEADD(day,-1,@Now) AS date), @D2, NULL, NULL, NULL, NULL, NULL, N'قيد العد: اختبر Record Count ثم Complete.', @D2, @SeedUser, @D2, @SeedUser),
        (@SC_Review,    N'SC-DEMO-REVIEW-001',   @WhBranch, 3, CAST(DATEADD(day,-2,@Now) AS date), @D4, @D2, NULL, NULL, NULL, NULL, N'مراجعة: توجد فروقات موجبة وسالبة.', @D4, @SeedUser, @D2, @SeedUser),
        (@SC_Approved,  N'SC-DEMO-APPROVED-001', @WhMain,   4, CAST(DATEADD(day,-3,@Now) AS date), @D6, @D4, @D2, @SeedUser, NULL, NULL, N'معتمد بفروقات صفرية؛ مناسب لاختبار Post بأمان.', @D6, @SeedUser, @D2, @SeedUser),
        (@SC_Posted,    N'SC-DEMO-POSTED-001',   @WhBranch, 5, CAST(DATEADD(day,-4,@Now) AS date), @D8, @D6, @D4, @SeedUser, @D2, @SeedUser, N'جرد مرحل للاختبار البصري.', @D8, @SeedUser, @D2, @SeedUser),
        (@SC_Cancelled, N'SC-DEMO-CANCEL-001',   @WhMain,   6, CAST(DATEADD(day,-5,@Now) AS date), @D10,NULL, NULL, NULL, NULL, NULL, N'جرد ملغي لاختبار الحالة.', @D10,@SeedUser, @D8, @SeedUser);

    -- Draft lines: same initial shape produced by CreateStockCount (Counted/Difference = 0 until RecordCount).
    INSERT dbo.tbl_StockCountLines
        (Id, StockCountId, ProductVariantId, SystemQuantity, CountedQuantity, DifferenceQuantity,
         AverageCostSnapshot, VarianceValue, CountedAtUtc, CountedBy, Notes)
    VALUES
        ('D0000000-0000-0000-0000-000000000001', @SC_Draft, @VFrameBlack,    25.000, 0.000,  0.000, 45.00,   0.00, NULL, NULL, N'جاهز للبدء'),
        ('D0000000-0000-0000-0000-000000000002', @SC_Draft, @VLens156,      40.000, 0.000,  0.000, 15.00,   0.00, NULL, NULL, N'جاهز للبدء'),

        ('D0000000-0000-0000-0000-000000000003', @SC_Counting, @VFrameTortoise, 12.000,11.000, -1.000, 48.00, -48.00, @D2, @SeedUser, N'نقص قطعة'),
        ('D0000000-0000-0000-0000-000000000004', @SC_Counting, @VSunglassesBlack,8.000, 0.000,  0.000, 70.00,   0.00, NULL, NULL, N'لم يتم عده بعد'),

        ('D0000000-0000-0000-0000-000000000005', @SC_Review, @VFrameBlack,       7.000, 8.000,  1.000, 45.00,  45.00, @D2, @SeedUser, N'زيادة قطعة'),
        ('D0000000-0000-0000-0000-000000000006', @SC_Review, @VSunglassesBlack,  5.000, 4.000, -1.000, 70.00, -70.00, @D2, @SeedUser, N'نقص قطعة'),

        -- Keep Approved differences at zero so the current V1 Post flow can be tested without creating adjustment ledger rows.
        ('D0000000-0000-0000-0000-000000000007', @SC_Approved, @VCleaner60,    30.000,30.000, 0.000, 3.00, 0.00, @D4, @SeedUser, N'مطابق'),
        ('D0000000-0000-0000-0000-000000000008', @SC_Approved, @VCaseStandard, 20.000,20.000, 0.000, 2.00, 0.00, @D4, @SeedUser, N'مطابق'),

        ('D0000000-0000-0000-0000-000000000009', @SC_Posted, @VLens156,     15.000,15.000, 0.000,15.00, 0.00, @D6, @SeedUser, N'جرد مرحل'),
        ('D0000000-0000-0000-0000-000000000010', @SC_Cancelled, @VLens167, 18.000,18.000, 0.000,25.00, 0.00, @D8, @SeedUser, N'ألغي بعد بدء الاختبار');

    ---------------------------------------------------------------------------
    -- Keep the application's dynamic sequence generator ahead of seeded ledger.
    -- SequenceNumberGenerator uses accounting.Seq_InventoryLedger.
    ---------------------------------------------------------------------------
    IF SCHEMA_ID(N'accounting') IS NULL
        EXEC(N'CREATE SCHEMA [accounting] AUTHORIZATION [dbo];');

    IF EXISTS (
        SELECT 1 FROM sys.sequences
        WHERE name = N'Seq_InventoryLedger'
          AND schema_id = SCHEMA_ID(N'accounting')
    )
        ALTER SEQUENCE [accounting].[Seq_InventoryLedger] RESTART WITH 19;
    ELSE
        CREATE SEQUENCE [accounting].[Seq_InventoryLedger]
            AS BIGINT START WITH 19 INCREMENT BY 1 NO CYCLE;

    -- Use higher ranges for auto-generated transaction/count numbers to keep demo numbers visually distinct.
    IF EXISTS (
        SELECT 1 FROM sys.sequences
        WHERE name = N'Seq_InventoryTransaction'
          AND schema_id = SCHEMA_ID(N'accounting')
    )
        ALTER SEQUENCE [accounting].[Seq_InventoryTransaction] RESTART WITH 101;
    ELSE
        CREATE SEQUENCE [accounting].[Seq_InventoryTransaction]
            AS BIGINT START WITH 101 INCREMENT BY 1 NO CYCLE;

    IF EXISTS (
        SELECT 1 FROM sys.sequences
        WHERE name = N'Seq_StockCount'
          AND schema_id = SCHEMA_ID(N'accounting')
    )
        ALTER SEQUENCE [accounting].[Seq_StockCount] RESTART WITH 101;
    ELSE
        CREATE SEQUENCE [accounting].[Seq_StockCount]
            AS BIGINT START WITH 101 INCREMENT BY 1 NO CYCLE;

    COMMIT TRANSACTION;

    ---------------------------------------------------------------------------
    -- Verification summary
    ---------------------------------------------------------------------------
    SELECT N'tbl_ProductCategories'          AS [TableName], COUNT_BIG(*) AS [RowCount] FROM dbo.tbl_ProductCategories
    UNION ALL SELECT N'tbl_Brands',                          COUNT_BIG(*) FROM dbo.tbl_Brands
    UNION ALL SELECT N'tbl_Units',                           COUNT_BIG(*) FROM dbo.tbl_Units
    UNION ALL SELECT N'tbl_productTypes',                    COUNT_BIG(*) FROM dbo.tbl_productTypes
    UNION ALL SELECT N'tbl_Products',                        COUNT_BIG(*) FROM dbo.tbl_Products
    UNION ALL SELECT N'tbl_ProductVariants',                 COUNT_BIG(*) FROM dbo.tbl_ProductVariants
    UNION ALL SELECT N'tbl_FrameDetails',                    COUNT_BIG(*) FROM dbo.tbl_FrameDetails
    UNION ALL SELECT N'tbl_LensDetails',                     COUNT_BIG(*) FROM dbo.tbl_LensDetails
    UNION ALL SELECT N'tbl_Warehouses',                      COUNT_BIG(*) FROM dbo.tbl_Warehouses
    UNION ALL SELECT N'tbl_InventoryBalances',               COUNT_BIG(*) FROM dbo.tbl_InventoryBalances
    UNION ALL SELECT N'tbl_InventoryTransactions',           COUNT_BIG(*) FROM dbo.tbl_InventoryTransactions
    UNION ALL SELECT N'tbl_InventoryTransactionLines',       COUNT_BIG(*) FROM dbo.tbl_InventoryTransactionLines
    UNION ALL SELECT N'tbl_InventoryLedger',                 COUNT_BIG(*) FROM dbo.tbl_InventoryLedger
    UNION ALL SELECT N'tbl_StockCounts',                     COUNT_BIG(*) FROM dbo.tbl_StockCounts
    UNION ALL SELECT N'tbl_StockCountLines',                 COUNT_BIG(*) FROM dbo.tbl_StockCountLines;

    SELECT
        N'Seed completed successfully' AS [Result],
        (SELECT COUNT(*) FROM dbo.tbl_Products) AS Products,
        (SELECT COUNT(*) FROM dbo.tbl_ProductVariants) AS Variants,
        (SELECT COUNT(*) FROM dbo.tbl_Warehouses) AS Warehouses,
        (SELECT COUNT(*) FROM dbo.tbl_InventoryBalances) AS Balances,
        (SELECT COUNT(*) FROM dbo.tbl_InventoryTransactions) AS [Transactions],
        (SELECT COUNT(*) FROM dbo.tbl_InventoryLedger) AS LedgerEntries,
        (SELECT COUNT(*) FROM dbo.tbl_StockCounts) AS StockCounts;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
