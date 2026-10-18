/* AP-11: READ ONLY. Run against a restored TEST/REPLICA database only.
   No database modifications, posted journal updates, or hard-coded exchange conversions.
   Choose @From/@To below. Posted journal status = 4 in OAS.Domain.Accounting.Enums.
*/
SET NOCOUNT ON;
DECLARE @From date = '2026-11-01', @To date = '2026-11-30';

PRINT 'AP11.1 Posted journals with missing/unbalanced base lines (must return ZERO rows)';
SELECT j.Id, j.JournalNumber, j.SourceModule, j.SourceDocumentType, j.SourceDocumentId,
       COUNT(l.Id) AS LineCount, COALESCE(SUM(l.DebitAmount),0) AS DebitBase,
       COALESCE(SUM(l.CreditAmount),0) AS CreditBase
FROM dbo.tbl_JournalEntries j
LEFT JOIN dbo.tbl_JournalEntryLines l ON l.JournalEntryId = j.Id
WHERE j.Status = 4 AND j.PostingDate BETWEEN @From AND @To
GROUP BY j.Id, j.JournalNumber, j.SourceModule, j.SourceDocumentType, j.SourceDocumentId
HAVING COUNT(l.Id) < 2 OR ABS(COALESCE(SUM(l.DebitAmount),0)-COALESCE(SUM(l.CreditAmount),0)) > 0.0001;

PRINT 'AP11.2 Duplicate source-document postings (must return ZERO rows)';
SELECT SourceModule, SourceDocumentType, SourceDocumentId, COUNT(*) AS DuplicateCount
FROM dbo.tbl_JournalEntries
WHERE Status = 4 AND SourceModule IS NOT NULL
  AND SourceDocumentType IS NOT NULL AND SourceDocumentId IS NOT NULL
GROUP BY SourceModule, SourceDocumentType, SourceDocumentId HAVING COUNT(*) > 1;

PRINT 'AP11.3 Supplier subledger posted movement for period (base currency; do not aggregate source currencies)';
SELECT s.Id AS SupplierId, s.SupplierCode, s.NameAr, s.AccountId,
       SUM(COALESCE(l.CreditAmount,0)-COALESCE(l.DebitAmount,0)) AS LiabilityNetBase
FROM dbo.tbl_Suppliers s
JOIN dbo.tbl_JournalEntryLines l ON l.AccountId=s.AccountId
JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId AND j.Status=4
WHERE j.PostingDate BETWEEN @From AND @To
GROUP BY s.Id,s.SupplierCode,s.NameAr,s.AccountId ORDER BY s.SupplierCode;

PRINT 'AP11.4 Incorrect supplier reference on GL subledger (must return ZERO rows)';
SELECT j.JournalNumber, l.Id AS LineId, s.SupplierCode, l.SupplierId AS ActualSupplierId,
       s.Id AS ExpectedSupplierId, l.AccountId
FROM dbo.tbl_JournalEntryLines l
JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId AND j.Status=4
JOIN dbo.tbl_Suppliers s ON s.AccountId=l.AccountId
WHERE j.PostingDate BETWEEN @From AND @To
  AND (l.SupplierId IS NULL OR l.SupplierId<>s.Id);

PRINT 'AP11.5 Cross-supplier purchase payment allocations (must return ZERO rows)';
SELECT a.Id AS AllocationId, i.PurchaseInvoiceCode, i.SupplierId AS InvoiceSupplierId,
       v.SupplierId AS VoucherLineSupplierId
FROM dbo.tbl_PaymentAllocations a
JOIN dbo.tbl_PurchaseInvoices i ON i.Id=a.TargetDocumentId
JOIN dbo.tbl_PaymentVoucherLines v ON v.Id=a.PaymentVoucherLineId
WHERE a.TargetDocumentType=2 AND a.ReversedAtUtc IS NULL
  AND (v.SupplierId IS NULL OR v.SupplierId<>i.SupplierId);

PRINT 'AP11.6 Active purchase invoice settlements (invoice currency ONLY when known)';
WITH payments AS (
 SELECT TargetDocumentId AS InvoiceId, SUM(TargetDocumentAmount) AS PaymentInInvoiceCurrency,
        SUM(CASE WHEN TargetDocumentAmount IS NULL THEN 1 ELSE 0 END) AS UnknownConversionLines
 FROM dbo.tbl_PaymentAllocations
 WHERE TargetDocumentType=2 AND ReversedAtUtc IS NULL GROUP BY TargetDocumentId
), credits AS (
 SELECT PurchaseInvoiceId AS InvoiceId, SUM(AppliedInvoiceCurrencyAmount) AS CreditInInvoiceCurrency
 FROM dbo.tbl_SupplierCreditApplications WHERE Status=1 GROUP BY PurchaseInvoiceId
)
SELECT i.PurchaseInvoiceCode, i.SupplierId, i.CurrencyCodeSnapshot,
       i.TotalAmount, p.PaymentInInvoiceCurrency, c.CreditInInvoiceCurrency,
       i.TotalAmount - COALESCE(p.PaymentInInvoiceCurrency,0) - COALESCE(c.CreditInInvoiceCurrency,0) AS RemainingInvoiceCurrency,
       COALESCE(p.UnknownConversionLines,0) AS UnknownConversionLines
FROM dbo.tbl_PurchaseInvoices i
LEFT JOIN payments p ON p.InvoiceId=i.Id
LEFT JOIN credits c ON c.InvoiceId=i.Id
WHERE i.PostingDate BETWEEN @From AND @To
ORDER BY i.PurchaseInvoiceCode;

PRINT 'AP11.7 Exchange settlement journals and counterpart AP entries';
SELECT j.SourceDocumentId AS AllocationId, j.JournalNumber, j.PostingDate,
       COUNT(l.Id) AS LineCount, SUM(l.DebitAmount) AS TotalDebitBase,
       SUM(l.CreditAmount) AS TotalCreditBase
FROM dbo.tbl_JournalEntries j
JOIN dbo.tbl_JournalEntryLines l ON l.JournalEntryId=j.Id
WHERE j.SourceModule=N'Accounting'
  AND j.SourceDocumentType IN (N'SupplierSettlementFx',N'SupplierSettlementFxReversal')
  AND j.PostingDate BETWEEN @From AND @To
GROUP BY j.SourceDocumentId,j.JournalNumber,j.PostingDate
ORDER BY j.PostingDate,j.JournalNumber;

PRINT 'AP11.8 Subledger/control hierarchy: direct journal lines on header/control account (manual investigation)';
SELECT a.Code, a.NameAr, a.AccountType, SUM(l.DebitAmount) AS DebitBase, SUM(l.CreditAmount) AS CreditBase
FROM dbo.tbl_Accounts a
JOIN dbo.tbl_JournalEntryLines l ON l.AccountId=a.Id
JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId AND j.Status=4
WHERE a.IsControlAccount=1 AND j.PostingDate BETWEEN @From AND @To
GROUP BY a.Code,a.NameAr,a.AccountType;
-- A control header often has NO direct lines; reconcile its descendant subledger balances,
-- not a naive equality between supplier leaf totals and a non-posting header row.
