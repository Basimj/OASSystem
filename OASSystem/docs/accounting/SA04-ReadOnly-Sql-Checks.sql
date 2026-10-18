-- SA-04 READ-ONLY sanity checks. Execute on SQL Server staging, never on production without review.
-- Choose your customer and date, then compare to GET /api/accounting/reports/customers/{id}/statement.
DECLARE @CustomerId uniqueidentifier = (SELECT TOP(1) Id FROM dbo.tbl_Customers ORDER BY CustomerCode);
DECLARE @From date = '2026-10-01';
DECLARE @To date = '2026-10-31';

SELECT c.CustomerCode, c.NameAr, c.AccountId,
       SUM(CASE WHEN j.PostingDate < @From THEN l.DebitAmount-l.CreditAmount ELSE 0 END) AS OpeningBase,
       SUM(CASE WHEN j.PostingDate BETWEEN @From AND @To THEN l.DebitAmount ELSE 0 END) AS PeriodDebitBase,
       SUM(CASE WHEN j.PostingDate BETWEEN @From AND @To THEN l.CreditAmount ELSE 0 END) AS PeriodCreditBase,
       SUM(l.DebitAmount-l.CreditAmount) AS ClosingBase
FROM dbo.tbl_Customers c
JOIN dbo.tbl_JournalEntryLines l ON l.AccountId=c.AccountId
JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId
WHERE c.Id=@CustomerId AND j.Status=4 AND j.PostingDate<=@To
GROUP BY c.CustomerCode,c.NameAr,c.AccountId;

-- Cash/bank lines carrying CustomerId must NOT be included unless they actually use Customer.AccountId.
SELECT l.JournalEntryId,l.AccountId,l.CustomerId,l.DebitAmount,l.CreditAmount
FROM dbo.tbl_JournalEntryLines l
JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId
JOIN dbo.tbl_Customers c ON c.Id=l.CustomerId
WHERE c.Id=@CustomerId AND l.AccountId<>c.AccountId AND j.Status=4 AND j.PostingDate<=@To;
