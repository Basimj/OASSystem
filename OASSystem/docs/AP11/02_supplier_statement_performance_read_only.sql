/* AP-11 read-only performance baseline. Run only against an authorized TEST/REPLICA.
   Fill @SupplierId from an existing supplier; never generate a GUID here.
   Measure actual execution plan in SSMS and capture STATISTICS IO/TIME output. */
SET NOCOUNT ON;
DECLARE @SupplierId uniqueidentifier = NULL; -- specify actual supplier from TEST database
DECLARE @From date = '2026-01-01', @To date = '2026-12-31';
IF @SupplierId IS NULL
BEGIN
    PRINT 'AP-11: provide an existing test supplier ID; no query executed.';
    RETURN;
END;
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
-- Include stable deterministic ordering equivalent to the supplier statement API.
SELECT TOP (200) j.PostingDate,j.JournalNumber,j.Id AS JournalId,l.LineNumber,
       l.DebitAmount,l.CreditAmount,l.TransactionCurrencyCodeSnapshot
FROM dbo.tbl_Suppliers s
JOIN dbo.tbl_JournalEntryLines l ON l.AccountId=s.AccountId
JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId
WHERE s.Id=@SupplierId AND j.Status=4 AND j.PostingDate BETWEEN @From AND @To
ORDER BY j.PostingDate,j.JournalNumber,j.Id,l.LineNumber;
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
-- For a 100k-row fixture, record: rows returned, reads, CPU/elapsed ms, spills,
-- key scans and seek predicates. Never create indexes in production on guesswork.
