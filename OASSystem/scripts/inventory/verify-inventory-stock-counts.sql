/*
    OAS Inventory Stock Counts — Manual QA Verification

    ضع رقم الجرد الذي تريد فحصه في @StockCountNumber.
    إذا تركته NULL سيختار السكربت أحدث جرد.

    Enums:
      StockCountStatus: Draft=1, Counting=2, Review=3, Approved=4, Posted=5, Cancelled=6
      InventoryTransactionType: AdjustmentIncrease=5, AdjustmentDecrease=6
      InventoryTransactionStatus: Draft=1, Posted=2
      InventoryMovementType: In=1, Out=2
*/

SET NOCOUNT ON;

DECLARE @StockCountNumber nvarchar(32) = NULL; -- مثال: N'SC-2026-000001'
DECLARE @StockCountId uniqueidentifier;
DECLARE @WarehouseId uniqueidentifier;
DECLARE @Status int;

IF @StockCountNumber IS NULL
BEGIN
    SELECT TOP (1)
        @StockCountId = Id,
        @StockCountNumber = CountNumber,
        @WarehouseId = WarehouseId,
        @Status = Status
    FROM dbo.tbl_StockCounts
    ORDER BY CountDate DESC, CreatedAtUtc DESC;
END
ELSE
BEGIN
    SELECT
        @StockCountId = Id,
        @WarehouseId = WarehouseId,
        @Status = Status
    FROM dbo.tbl_StockCounts
    WHERE CountNumber = @StockCountNumber;
END;

IF @StockCountId IS NULL
BEGIN
    THROW 50001, 'Stock count was not found. Update @StockCountNumber and run again.', 1;
END;

PRINT 'Selected Stock Count: ' + ISNULL(@StockCountNumber, N'');

PRINT '=== 1. STOCK COUNT HEADER ===';
SELECT
    sc.Id,
    sc.CountNumber,
    sc.WarehouseId,
    w.Code AS WarehouseCode,
    w.NameAr AS WarehouseName,
    w.IsActive AS WarehouseIsActive,
    sc.Status,
    CASE sc.Status
        WHEN 1 THEN N'Draft'
        WHEN 2 THEN N'Counting'
        WHEN 3 THEN N'Review'
        WHEN 4 THEN N'Approved'
        WHEN 5 THEN N'Posted'
        WHEN 6 THEN N'Cancelled'
        ELSE N'Unknown'
    END AS StatusName,
    sc.CountDate,
    sc.StartedAtUtc,
    sc.CompletedAtUtc,
    sc.ApprovedAtUtc,
    sc.ApprovedBy,
    sc.PostedAtUtc,
    sc.PostedBy,
    sc.Notes,
    sc.CreatedAtUtc,
    sc.CreatedBy,
    sc.LastModifiedAtUtc,
    sc.LastModifiedBy
FROM dbo.tbl_StockCounts sc
JOIN dbo.tbl_Warehouses w ON w.Id = sc.WarehouseId
WHERE sc.Id = @StockCountId;

PRINT '=== 2. STOCK COUNT LINES + CURRENT BALANCE ===';
SELECT
    scl.Id AS StockCountLineId,
    scl.ProductVariantId,
    p.ProductCode,
    p.NameAr AS ProductName,
    pv.SKU,
    pv.Barcode,
    pv.VariantName,
    scl.SystemQuantity,
    scl.CountedQuantity,
    scl.DifferenceQuantity,
    scl.AverageCostSnapshot,
    scl.VarianceValue,
    ROUND(scl.CountedQuantity - scl.SystemQuantity, 3) AS ExpectedDifference,
    ROUND((scl.CountedQuantity - scl.SystemQuantity) * scl.AverageCostSnapshot, 2) AS ExpectedVarianceValue,
    scl.CountedAtUtc,
    scl.CountedBy,
    scl.Notes,
    b.OnHandQuantity AS CurrentOnHand,
    b.AverageUnitCost AS CurrentAverageCost,
    b.InventoryValue AS CurrentInventoryValue,
    CASE
        WHEN scl.CountedAtUtc IS NULL THEN NULL
        ELSE b.OnHandQuantity - scl.CountedQuantity
    END AS CurrentBalanceMinusCounted
FROM dbo.tbl_StockCountLines scl
JOIN dbo.tbl_ProductVariants pv ON pv.Id = scl.ProductVariantId
JOIN dbo.tbl_Products p ON p.Id = pv.ProductId
LEFT JOIN dbo.tbl_InventoryBalances b
    ON b.WarehouseId = @WarehouseId
   AND b.ProductVariantId = scl.ProductVariantId
WHERE scl.StockCountId = @StockCountId
ORDER BY p.ProductCode, pv.SKU;

PRINT '=== 3. AUTO-GENERATED ADJUSTMENT TRANSACTIONS ===';
SELECT
    t.Id AS TransactionId,
    t.TransactionNumber,
    t.TransactionType,
    CASE t.TransactionType
        WHEN 5 THEN N'AdjustmentIncrease'
        WHEN 6 THEN N'AdjustmentDecrease'
        ELSE N'Unexpected'
    END AS TransactionTypeName,
    t.Status,
    CASE t.Status WHEN 1 THEN N'Draft' WHEN 2 THEN N'Posted' ELSE N'Unknown' END AS TransactionStatusName,
    t.SourceWarehouseId,
    t.DestinationWarehouseId,
    t.TransactionDate,
    t.ReferenceType,
    t.ReferenceId,
    t.Reason,
    t.Notes,
    t.PostedAtUtc,
    t.PostedBy
FROM dbo.tbl_InventoryTransactions t
WHERE t.ReferenceType = N'StockCount'
  AND t.ReferenceId = @StockCountId
ORDER BY t.TransactionType, t.TransactionNumber;

PRINT '=== 4. GENERATED ADJUSTMENT LINES ===';
SELECT
    t.TransactionNumber,
    t.TransactionType,
    tl.Id AS TransactionLineId,
    tl.ProductVariantId,
    pv.SKU,
    tl.Quantity,
    tl.UnitCost,
    tl.TotalCost,
    tl.Notes
FROM dbo.tbl_InventoryTransactions t
JOIN dbo.tbl_InventoryTransactionLines tl ON tl.TransactionId = t.Id
JOIN dbo.tbl_ProductVariants pv ON pv.Id = tl.ProductVariantId
WHERE t.ReferenceType = N'StockCount'
  AND t.ReferenceId = @StockCountId
ORDER BY t.TransactionType, pv.SKU;

PRINT '=== 5. LEDGER CREATED BY STOCK COUNT ADJUSTMENTS ===';
SELECT
    g.Id AS LedgerId,
    g.SequenceNumber,
    t.TransactionNumber,
    t.TransactionType,
    g.TransactionId,
    g.TransactionLineId,
    g.WarehouseId,
    w.Code AS WarehouseCode,
    g.ProductVariantId,
    pv.SKU,
    g.MovementType,
    CASE g.MovementType WHEN 1 THEN N'In' WHEN 2 THEN N'Out' ELSE N'Unknown' END AS MovementTypeName,
    g.QuantityIn,
    g.QuantityOut,
    g.BalanceAfter,
    g.UnitCost,
    g.AverageCostAfter,
    g.InventoryValueAfter,
    g.MovementDate,
    g.CreatedAtUtc,
    g.CreatedBy
FROM dbo.tbl_InventoryTransactions t
JOIN dbo.tbl_InventoryTransactionLines tl ON tl.TransactionId = t.Id
JOIN dbo.tbl_InventoryLedger g ON g.TransactionId = t.Id AND g.TransactionLineId = tl.Id
JOIN dbo.tbl_Warehouses w ON w.Id = g.WarehouseId
JOIN dbo.tbl_ProductVariants pv ON pv.Id = g.ProductVariantId
WHERE t.ReferenceType = N'StockCount'
  AND t.ReferenceId = @StockCountId
ORDER BY g.SequenceNumber;

PRINT '=== 6. AUTOMATED PASS / FAIL CHECKS ===';

DECLARE @LineCount int = (
    SELECT COUNT(*) FROM dbo.tbl_StockCountLines WHERE StockCountId = @StockCountId
);
DECLARE @UncountedCount int = (
    SELECT COUNT(*) FROM dbo.tbl_StockCountLines WHERE StockCountId = @StockCountId AND CountedAtUtc IS NULL
);
DECLARE @PositiveCount int = (
    SELECT COUNT(*) FROM dbo.tbl_StockCountLines WHERE StockCountId = @StockCountId AND DifferenceQuantity > 0
);
DECLARE @NegativeCount int = (
    SELECT COUNT(*) FROM dbo.tbl_StockCountLines WHERE StockCountId = @StockCountId AND DifferenceQuantity < 0
);
DECLARE @ZeroCount int = (
    SELECT COUNT(*) FROM dbo.tbl_StockCountLines WHERE StockCountId = @StockCountId AND DifferenceQuantity = 0
);
DECLARE @ExpectedTransactionCount int =
    CASE WHEN @PositiveCount > 0 THEN 1 ELSE 0 END +
    CASE WHEN @NegativeCount > 0 THEN 1 ELSE 0 END;
DECLARE @GeneratedTransactionCount int = (
    SELECT COUNT(*)
    FROM dbo.tbl_InventoryTransactions
    WHERE ReferenceType = N'StockCount' AND ReferenceId = @StockCountId
);
DECLARE @GeneratedLedgerCount int = (
    SELECT COUNT(*)
    FROM dbo.tbl_InventoryLedger g
    JOIN dbo.tbl_InventoryTransactions t ON t.Id = g.TransactionId
    WHERE t.ReferenceType = N'StockCount' AND t.ReferenceId = @StockCountId
);

DECLARE @Checks TABLE
(
    CheckName nvarchar(160) NOT NULL,
    ResultStatus varchar(8) NOT NULL,
    Details nvarchar(500) NULL
);

INSERT INTO @Checks VALUES
(
    N'الجرد يحتوي على بنود',
    CASE WHEN @LineCount > 0 THEN 'PASS' ELSE 'FAIL' END,
    CONCAT(N'Lines=', @LineCount)
);

INSERT INTO @Checks VALUES
(
    N'DifferenceQuantity مطابق للكمية الفعلية ناقص كمية النظام',
    CASE WHEN NOT EXISTS (
        SELECT 1 FROM dbo.tbl_StockCountLines
        WHERE StockCountId = @StockCountId
          AND CountedAtUtc IS NOT NULL
          AND DifferenceQuantity <> ROUND(CountedQuantity - SystemQuantity, 3)
    ) THEN 'PASS' ELSE 'FAIL' END,
    NULL
);

INSERT INTO @Checks VALUES
(
    N'VarianceValue مطابق للفرق × تكلفة Snapshot بعد التقريب',
    CASE WHEN NOT EXISTS (
        SELECT 1 FROM dbo.tbl_StockCountLines
        WHERE StockCountId = @StockCountId
          AND CountedAtUtc IS NOT NULL
          AND VarianceValue <> ROUND(DifferenceQuantity * AverageCostSnapshot, 2)
    ) THEN 'PASS' ELSE 'FAIL' END,
    NULL
);

INSERT INTO @Checks VALUES
(
    N'كل بند معدود يحتوي CountedBy',
    CASE WHEN NOT EXISTS (
        SELECT 1 FROM dbo.tbl_StockCountLines
        WHERE StockCountId = @StockCountId
          AND CountedAtUtc IS NOT NULL
          AND (CountedBy IS NULL OR LTRIM(RTRIM(CountedBy)) = N'')
    ) THEN 'PASS' ELSE 'FAIL' END,
    NULL
);

INSERT INTO @Checks VALUES
(
    N'Review/Approved/Posted لا تحتوي بنودًا غير معدودة',
    CASE WHEN @Status IN (3,4,5) AND @UncountedCount > 0 THEN 'FAIL' ELSE 'PASS' END,
    CONCAT(N'Uncounted=', @UncountedCount)
);

INSERT INTO @Checks VALUES
(
    N'StartedAtUtc متوافق مع الحالة',
    CASE WHEN EXISTS (
        SELECT 1 FROM dbo.tbl_StockCounts
        WHERE Id = @StockCountId
          AND ((Status = 1 AND StartedAtUtc IS NOT NULL)
            OR (Status IN (2,3,4,5) AND StartedAtUtc IS NULL))
    ) THEN 'FAIL' ELSE 'PASS' END,
    NULL
);

INSERT INTO @Checks VALUES
(
    N'CompletedAtUtc متوافق مع Review/Approved/Posted',
    CASE WHEN @Status IN (3,4,5) AND EXISTS (
        SELECT 1 FROM dbo.tbl_StockCounts WHERE Id = @StockCountId AND CompletedAtUtc IS NULL
    ) THEN 'FAIL' ELSE 'PASS' END,
    NULL
);

INSERT INTO @Checks VALUES
(
    N'ApprovedAtUtc و ApprovedBy متوافقان مع Approved/Posted',
    CASE WHEN @Status IN (4,5) AND EXISTS (
        SELECT 1 FROM dbo.tbl_StockCounts
        WHERE Id = @StockCountId AND (ApprovedAtUtc IS NULL OR ApprovedBy IS NULL OR LTRIM(RTRIM(ApprovedBy)) = N'')
    ) THEN 'FAIL' ELSE 'PASS' END,
    NULL
);

INSERT INTO @Checks VALUES
(
    N'PostedAtUtc و PostedBy موجودان عند Posted',
    CASE WHEN @Status = 5 AND EXISTS (
        SELECT 1 FROM dbo.tbl_StockCounts
        WHERE Id = @StockCountId AND (PostedAtUtc IS NULL OR PostedBy IS NULL OR LTRIM(RTRIM(PostedBy)) = N'')
    ) THEN 'FAIL' ELSE 'PASS' END,
    NULL
);

INSERT INTO @Checks VALUES
(
    N'الجرد غير المرحل لا يملك Adjustment مولدة',
    CASE WHEN @Status <> 5 AND @GeneratedTransactionCount > 0 THEN 'FAIL' ELSE 'PASS' END,
    CONCAT(N'GeneratedTransactions=', @GeneratedTransactionCount)
);

INSERT INTO @Checks VALUES
(
    N'عدد Adjustment المولدة بعد Post صحيح',
    CASE WHEN @Status <> 5 OR @GeneratedTransactionCount = @ExpectedTransactionCount THEN 'PASS' ELSE 'FAIL' END,
    CONCAT(N'Expected=', @ExpectedTransactionCount, N', Actual=', @GeneratedTransactionCount)
);

INSERT INTO @Checks VALUES
(
    N'كل Adjustment مولدة مرحّلة ومرتبطة بالمخزن الصحيح',
    CASE WHEN NOT EXISTS (
        SELECT 1
        FROM dbo.tbl_InventoryTransactions t
        WHERE t.ReferenceType = N'StockCount'
          AND t.ReferenceId = @StockCountId
          AND (
                t.Status <> 2
             OR t.TransactionType NOT IN (5,6)
             OR (t.TransactionType = 5 AND (t.DestinationWarehouseId <> @WarehouseId OR t.SourceWarehouseId IS NOT NULL))
             OR (t.TransactionType = 6 AND (t.SourceWarehouseId <> @WarehouseId OR t.DestinationWarehouseId IS NOT NULL))
          )
    ) THEN 'PASS' ELSE 'FAIL' END,
    NULL
);

INSERT INTO @Checks VALUES
(
    N'كل فرق غير صفري له Adjustment Line واحدة مطابقة',
    CASE WHEN @Status <> 5 OR NOT EXISTS (
        SELECT 1
        FROM dbo.tbl_StockCountLines scl
        WHERE scl.StockCountId = @StockCountId
          AND scl.DifferenceQuantity <> 0
          AND (
              SELECT COUNT(*)
              FROM dbo.tbl_InventoryTransactions t
              JOIN dbo.tbl_InventoryTransactionLines tl ON tl.TransactionId = t.Id
              WHERE t.ReferenceType = N'StockCount'
                AND t.ReferenceId = @StockCountId
                AND tl.ProductVariantId = scl.ProductVariantId
                AND tl.Quantity = ABS(scl.DifferenceQuantity)
                AND t.TransactionType = CASE WHEN scl.DifferenceQuantity > 0 THEN 5 ELSE 6 END
          ) <> 1
    ) THEN 'PASS' ELSE 'FAIL' END,
    NULL
);

INSERT INTO @Checks VALUES
(
    N'البنود ذات الفرق صفر لا تنشئ Adjustment Line',
    CASE WHEN NOT EXISTS (
        SELECT 1
        FROM dbo.tbl_StockCountLines scl
        JOIN dbo.tbl_InventoryTransactions t
          ON t.ReferenceType = N'StockCount' AND t.ReferenceId = @StockCountId
        JOIN dbo.tbl_InventoryTransactionLines tl
          ON tl.TransactionId = t.Id AND tl.ProductVariantId = scl.ProductVariantId
        WHERE scl.StockCountId = @StockCountId
          AND scl.DifferenceQuantity = 0
    ) THEN 'PASS' ELSE 'FAIL' END,
    CONCAT(N'ZeroDifferenceLines=', @ZeroCount)
);

INSERT INTO @Checks VALUES
(
    N'كل Adjustment Line لها Ledger واحدة صحيحة الاتجاه والكمية',
    CASE WHEN @Status <> 5 OR NOT EXISTS (
        SELECT 1
        FROM dbo.tbl_InventoryTransactions t
        JOIN dbo.tbl_InventoryTransactionLines tl ON tl.TransactionId = t.Id
        WHERE t.ReferenceType = N'StockCount'
          AND t.ReferenceId = @StockCountId
          AND (
              SELECT COUNT(*)
              FROM dbo.tbl_InventoryLedger g
              WHERE g.TransactionId = t.Id
                AND g.TransactionLineId = tl.Id
                AND g.ProductVariantId = tl.ProductVariantId
                AND g.WarehouseId = @WarehouseId
                AND (
                    (t.TransactionType = 5 AND g.MovementType = 1 AND g.QuantityIn = tl.Quantity AND g.QuantityOut = 0)
                    OR
                    (t.TransactionType = 6 AND g.MovementType = 2 AND g.QuantityOut = tl.Quantity AND g.QuantityIn = 0)
                )
          ) <> 1
    ) THEN 'PASS' ELSE 'FAIL' END,
    CONCAT(N'LedgerRows=', @GeneratedLedgerCount)
);

INSERT INTO @Checks VALUES
(
    N'لا توجد Ledger orphan مرتبطة بـAdjustment الجرد',
    CASE WHEN NOT EXISTS (
        SELECT 1
        FROM dbo.tbl_InventoryLedger g
        JOIN dbo.tbl_InventoryTransactions t ON t.Id = g.TransactionId
        LEFT JOIN dbo.tbl_InventoryTransactionLines tl
          ON tl.Id = g.TransactionLineId AND tl.TransactionId = g.TransactionId
        WHERE t.ReferenceType = N'StockCount'
          AND t.ReferenceId = @StockCountId
          AND tl.Id IS NULL
    ) THEN 'PASS' ELSE 'FAIL' END,
    NULL
);

SELECT CheckName, ResultStatus, Details
FROM @Checks
ORDER BY CASE ResultStatus WHEN 'FAIL' THEN 0 ELSE 1 END, CheckName;

PRINT '=== 7. SUMMARY ===';
SELECT
    @StockCountNumber AS StockCountNumber,
    @Status AS StockCountStatus,
    @LineCount AS LineCount,
    @UncountedCount AS UncountedLineCount,
    @PositiveCount AS PositiveDifferenceLines,
    @NegativeCount AS NegativeDifferenceLines,
    @ZeroCount AS ZeroDifferenceLines,
    @GeneratedTransactionCount AS GeneratedAdjustmentTransactions,
    @GeneratedLedgerCount AS GeneratedLedgerRows,
    (SELECT COUNT(*) FROM @Checks WHERE ResultStatus = 'PASS') AS PassedChecks,
    (SELECT COUNT(*) FROM @Checks WHERE ResultStatus = 'FAIL') AS FailedChecks;
