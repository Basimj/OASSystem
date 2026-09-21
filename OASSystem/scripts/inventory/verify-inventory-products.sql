/*
  Inventory Products UI round-trip verification.
  ضع القيم التي استخدمتها في الاختبار ثم شغل السكربت.
*/
DECLARE @CategoryCode nvarchar(32) = N'QA-CAT-001';
DECLARE @BrandCode    nvarchar(32) = N'QA-BRAND-001';
DECLARE @UnitCode     nvarchar(32) = N'QA-UNIT-001';
DECLARE @ProductCode  nvarchar(32) = N'QA-PROD-001';
DECLARE @Sku          nvarchar(64) = N'QA-SKU-001';

SELECT * FROM dbo.tbl_ProductCategories WHERE Code = @CategoryCode;
SELECT * FROM dbo.tbl_Brands            WHERE Code = @BrandCode;
SELECT * FROM dbo.tbl_Units             WHERE Code = @UnitCode;

SELECT p.*
FROM dbo.tbl_Products p
WHERE p.ProductCode = @ProductCode;

SELECT fd.*
FROM dbo.tbl_FrameDetails fd
INNER JOIN dbo.tbl_Products p ON p.Id = fd.ProductId
WHERE p.ProductCode = @ProductCode;

SELECT ld.*
FROM dbo.tbl_LensDetails ld
INNER JOIN dbo.tbl_Products p ON p.Id = ld.ProductId
WHERE p.ProductCode = @ProductCode;

SELECT v.*
FROM dbo.tbl_ProductVariants v
WHERE v.SKU = @Sku;
