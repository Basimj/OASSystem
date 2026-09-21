/*
  OAS Inventory V1 - Warehouse round-trip verification
  غيّر كود المخزن فقط ثم نفذ السكربت بعد الحفظ من الواجهة.
*/
SET NOCOUNT ON;

DECLARE @WarehouseCode nvarchar(32) = N'QA-WH-001';

SELECT
    w.Id,
    w.Code,
    w.NameAr,
    w.NameEn,
    w.Description,
    w.IsDefault,
    w.IsActive,
    w.CreatedAtUtc,
    w.CreatedBy,
    w.LastModifiedAtUtc,
    w.LastModifiedBy
FROM dbo.tbl_Warehouses AS w
WHERE w.Code = @WarehouseCode;

-- يجب أن تكون النتيجة صفر صفوف.
SELECT Code, COUNT(*) AS DuplicateCount
FROM dbo.tbl_Warehouses
GROUP BY Code
HAVING COUNT(*) > 1;

-- للمراجعة اليدوية: يفترض أن يوجد مخزن افتراضي واحد كحد أقصى.
SELECT Id, Code, NameAr, IsDefault, IsActive
FROM dbo.tbl_Warehouses
WHERE IsDefault = 1
ORDER BY Code;

-- يجب أن تكون النتيجة صفر صفوف.
SELECT Id, Code, NameAr, IsDefault, IsActive
FROM dbo.tbl_Warehouses
WHERE IsDefault = 1 AND IsActive = 0;
