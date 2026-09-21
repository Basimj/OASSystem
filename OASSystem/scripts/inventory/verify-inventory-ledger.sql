/*
    OAS Inventory Ledger verification script
    ---------------------------------------
    ضع رقم العملية المراد التحقق منها في @TransactionNumber.
    اتركه NULL لعرض أحدث سجلات Ledger.

    القيم الحالية للـEnums:
      InventoryTransactionStatus: Draft=1, Posted=2
      InventoryMovementType: In=1, Out=2
*/

SET NOCOUNT ON;

DECLARE @TransactionNumber nvarchar(32) = NULL; -- مثال: N'TXN-2026-000001'

DECLARE @TransactionId uniqueidentifier = NULL;

IF @TransactionNumber IS NOT NULL
BEGIN
    SELECT @TransactionId = Id
    FROM dbo.tbl_InventoryTransactions
    WHERE TransactionNumber = @TransactionNumber;

    IF @TransactionId IS NULL
    BEGIN
        THROW 50001, N'لم يتم العثور على TransactionNumber المحدد.', 1;
    END;
END;

-- 1) العرض التفصيلي المطابق تقريبًا لأعمدة واجهة Ledger
SELECT
    l.Id AS LedgerId,
    l.SequenceNumber,
    l.MovementDate,
    t.TransactionNumber,
    t.TransactionType,
    t.Status AS TransactionStatus,
    l.MovementType,
    p.ProductCode,
    p.NameAr AS ProductNameAr,
    pv.VariantName,
    pv.SKU,
    pv.Barcode,
    w.Code AS WarehouseCode,
    w.NameAr AS WarehouseNameAr,
    w.IsActive AS WarehouseIsActive,
    l.QuantityIn,
    l.QuantityOut,
    l.BalanceAfter,
    l.UnitCost,
    l.AverageCostAfter,
    l.InventoryValueAfter,
    t.ReferenceType,
    t.ReferenceId,
    l.TransactionId,
    l.TransactionLineId,
    l.ProductVariantId,
    l.WarehouseId,
    tl.Quantity AS TransactionLineQuantity,
    tl.UnitCost AS TransactionLineUnitCost,
    l.CreatedAtUtc,
    l.CreatedBy
FROM dbo.tbl_InventoryLedger l
INNER JOIN dbo.tbl_InventoryTransactions t ON t.Id = l.TransactionId
INNER JOIN dbo.tbl_InventoryTransactionLines tl ON tl.Id = l.TransactionLineId
INNER JOIN dbo.tbl_ProductVariants pv ON pv.Id = l.ProductVariantId
INNER JOIN dbo.tbl_Products p ON p.Id = pv.ProductId
INNER JOIN dbo.tbl_Warehouses w ON w.Id = l.WarehouseId
WHERE @TransactionId IS NULL OR l.TransactionId = @TransactionId
ORDER BY l.SequenceNumber DESC;

-- 2) اختبارات اتساق لكل Ledger row
SELECT
    l.SequenceNumber,
    t.TransactionNumber,
    CASE WHEN t.Status = 2 THEN 'PASS' ELSE 'FAIL' END AS [PostedTransaction],
    CASE WHEN tl.TransactionId = l.TransactionId THEN 'PASS' ELSE 'FAIL' END AS [LineBelongsToTransaction],
    CASE WHEN tl.ProductVariantId = l.ProductVariantId THEN 'PASS' ELSE 'FAIL' END AS [VariantMatchesLine],
    CASE WHEN l.MovementType IN (1, 2) THEN 'PASS' ELSE 'FAIL' END AS [MovementTypeValid],
    CASE
        WHEN l.MovementType = 1 AND l.QuantityIn > 0 AND l.QuantityOut = 0 THEN 'PASS'
        WHEN l.MovementType = 2 AND l.QuantityOut > 0 AND l.QuantityIn = 0 THEN 'PASS'
        ELSE 'FAIL'
    END AS [DirectionQuantitiesValid],
    CASE
        WHEN l.MovementType = 1 AND l.QuantityIn = tl.Quantity THEN 'PASS'
        WHEN l.MovementType = 2 AND l.QuantityOut = tl.Quantity THEN 'PASS'
        ELSE 'FAIL'
    END AS [MovementQuantityMatchesLine],
    CASE WHEN l.UnitCost >= 0 THEN 'PASS' ELSE 'FAIL' END AS [UnitCostNonNegative],
    CASE WHEN l.AverageCostAfter >= 0 THEN 'PASS' ELSE 'FAIL' END AS [AverageCostNonNegative],
    CASE WHEN l.InventoryValueAfter >= 0 THEN 'PASS' ELSE 'FAIL' END AS [InventoryValueNonNegative]
FROM dbo.tbl_InventoryLedger l
INNER JOIN dbo.tbl_InventoryTransactions t ON t.Id = l.TransactionId
INNER JOIN dbo.tbl_InventoryTransactionLines tl ON tl.Id = l.TransactionLineId
WHERE @TransactionId IS NULL OR l.TransactionId = @TransactionId
ORDER BY l.SequenceNumber DESC;

-- 3) فحص Transfer: يجب أن يكون لكل Line سجل Out من المصدر وسجل In إلى الوجهة
SELECT
    t.TransactionNumber,
    tl.Id AS TransactionLineId,
    tl.ProductVariantId,
    tl.Quantity,
    SUM(CASE WHEN l.MovementType = 2 AND l.WarehouseId = t.SourceWarehouseId THEN 1 ELSE 0 END) AS SourceOutRows,
    SUM(CASE WHEN l.MovementType = 1 AND l.WarehouseId = t.DestinationWarehouseId THEN 1 ELSE 0 END) AS DestinationInRows,
    CASE
        WHEN SUM(CASE WHEN l.MovementType = 2 AND l.WarehouseId = t.SourceWarehouseId THEN 1 ELSE 0 END) = 1
         AND SUM(CASE WHEN l.MovementType = 1 AND l.WarehouseId = t.DestinationWarehouseId THEN 1 ELSE 0 END) = 1
        THEN 'PASS'
        ELSE 'FAIL'
    END AS [TransferPairValid]
FROM dbo.tbl_InventoryTransactions t
INNER JOIN dbo.tbl_InventoryTransactionLines tl ON tl.TransactionId = t.Id
LEFT JOIN dbo.tbl_InventoryLedger l ON l.TransactionLineId = tl.Id
WHERE t.TransactionType = 4
  AND t.Status = 2
  AND (@TransactionId IS NULL OR t.Id = @TransactionId)
GROUP BY t.TransactionNumber, tl.Id, tl.ProductVariantId, tl.Quantity
ORDER BY t.TransactionNumber, tl.Id;

-- 4) إحصاءات تساعد على اختبار > 200 سجل
SELECT
    COUNT_BIG(*) AS TotalLedgerRows,
    COUNT(DISTINCT TransactionId) AS TransactionsWithLedger,
    MIN(SequenceNumber) AS MinSequenceNumber,
    MAX(SequenceNumber) AS MaxSequenceNumber,
    MIN(MovementDate) AS OldestMovementDate,
    MAX(MovementDate) AS NewestMovementDate
FROM dbo.tbl_InventoryLedger;

-- 5) فحص أي تناقضات عامة في Ledger حتى خارج العملية المختارة
SELECT
    'Ledger linked to non-posted transaction' AS CheckName,
    COUNT_BIG(*) AS ProblemCount
FROM dbo.tbl_InventoryLedger l
INNER JOIN dbo.tbl_InventoryTransactions t ON t.Id = l.TransactionId
WHERE t.Status <> 2
UNION ALL
SELECT
    'Ledger line transaction mismatch',
    COUNT_BIG(*)
FROM dbo.tbl_InventoryLedger l
INNER JOIN dbo.tbl_InventoryTransactionLines tl ON tl.Id = l.TransactionLineId
WHERE tl.TransactionId <> l.TransactionId
UNION ALL
SELECT
    'Ledger variant differs from transaction line',
    COUNT_BIG(*)
FROM dbo.tbl_InventoryLedger l
INNER JOIN dbo.tbl_InventoryTransactionLines tl ON tl.Id = l.TransactionLineId
WHERE tl.ProductVariantId <> l.ProductVariantId
UNION ALL
SELECT
    'Invalid In/Out quantity combination',
    COUNT_BIG(*)
FROM dbo.tbl_InventoryLedger l
WHERE NOT (
    (l.MovementType = 1 AND l.QuantityIn > 0 AND l.QuantityOut = 0)
    OR
    (l.MovementType = 2 AND l.QuantityOut > 0 AND l.QuantityIn = 0)
);
