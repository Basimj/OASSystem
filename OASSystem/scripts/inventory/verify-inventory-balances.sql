/*
  OAS Inventory V1 - Inventory Balance UI verification
  غيّر @Sku و/أو @WarehouseCode ثم نفذ السكربت وقارن النتائج مع شاشة أرصدة المخزون.
*/
SET NOCOUNT ON;

DECLARE @Sku nvarchar(64) = N'QA-SKU-001';
DECLARE @WarehouseCode nvarchar(32) = NULL; -- مثال: N'QA-WH-001'

SELECT
    b.Id AS BalanceId,
    p.Id AS ProductId,
    p.ProductCode,
    p.NameAr AS ProductNameAr,
    p.NameEn AS ProductNameEn,
    v.Id AS ProductVariantId,
    v.VariantName,
    v.SKU,
    v.Barcode,
    w.Id AS WarehouseId,
    w.Code AS WarehouseCode,
    w.NameAr AS WarehouseNameAr,
    w.IsActive AS WarehouseIsActive,
    CAST(b.OnHandQuantity AS decimal(18,3)) AS OnHandQuantity,
    CAST(b.ReservedQuantity AS decimal(18,3)) AS ReservedQuantity,
    CAST(b.OnHandQuantity - b.ReservedQuantity AS decimal(18,3)) AS AvailableQuantity,
    CAST(b.OnOrderQuantity AS decimal(18,3)) AS OnOrderQuantity,
    CAST(b.AverageUnitCost AS decimal(18,2)) AS AverageUnitCost,
    CAST(b.InventoryValue AS decimal(18,2)) AS InventoryValue,
    b.LastMovementAtUtc
FROM dbo.tbl_InventoryBalances AS b
INNER JOIN dbo.tbl_ProductVariants AS v ON v.Id = b.ProductVariantId
INNER JOIN dbo.tbl_Products AS p ON p.Id = v.ProductId
INNER JOIN dbo.tbl_Warehouses AS w ON w.Id = b.WarehouseId
WHERE (@Sku IS NULL OR v.SKU = @Sku)
  AND (@WarehouseCode IS NULL OR w.Code = @WarehouseCode)
ORDER BY w.Code, p.ProductCode, v.SKU;

-- يجب أن تكون النتيجة صفر صفوف: لا يوجد أكثر من رصيد لنفس Warehouse + ProductVariant.
SELECT
    WarehouseId,
    ProductVariantId,
    COUNT(*) AS DuplicateCount
FROM dbo.tbl_InventoryBalances
GROUP BY WarehouseId, ProductVariantId
HAVING COUNT(*) > 1;

-- يجب أن تكون النتيجة صفر صفوف: تحقق دفاعي من سلامة علاقات المخزن والـVariant.
SELECT b.Id, b.WarehouseId, b.ProductVariantId
FROM dbo.tbl_InventoryBalances AS b
LEFT JOIN dbo.tbl_Warehouses AS w ON w.Id = b.WarehouseId
LEFT JOIN dbo.tbl_ProductVariants AS v ON v.Id = b.ProductVariantId
WHERE w.Id IS NULL OR v.Id IS NULL;

-- ملخص يساعد في اختبار حالة أكثر من 200 سجل.
SELECT
    COUNT(*) AS TotalBalances,
    COUNT(DISTINCT WarehouseId) AS WarehouseCount,
    COUNT(DISTINCT ProductVariantId) AS VariantCount
FROM dbo.tbl_InventoryBalances;

-- أرصدة المخازن غير النشطة: يجب أن تبقى قابلة للعرض والفلترة من واجهة الاستعلام إن وجدت.
SELECT
    w.Code AS WarehouseCode,
    w.NameAr,
    v.SKU,
    b.OnHandQuantity,
    b.ReservedQuantity,
    b.OnOrderQuantity
FROM dbo.tbl_InventoryBalances AS b
INNER JOIN dbo.tbl_Warehouses AS w ON w.Id = b.WarehouseId
INNER JOIN dbo.tbl_ProductVariants AS v ON v.Id = b.ProductVariantId
WHERE w.IsActive = 0
ORDER BY w.Code, v.SKU;
