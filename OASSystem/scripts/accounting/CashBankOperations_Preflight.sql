-- Read-only. Select the OAS database in SSMS before executing.
-- Section 1 must return no rows before applying 20261126130000_CashBankOperations.
SET NOCOUNT ON;
;WITH Accounts AS (
    SELECT AccountId, Id AS CashBankId, N'Cash' AS Kind, Name AS DisplayName, CurrencyId FROM dbo.tbl_CashAccounts
    UNION ALL
    SELECT AccountId, Id, N'Bank', CONCAT(BankName, N' - ', AccountName), CurrencyId FROM dbo.tbl_BankAccounts
), Duplicates AS (
    SELECT AccountId FROM Accounts GROUP BY AccountId HAVING COUNT(*) > 1
)
SELECT a.* FROM Accounts a INNER JOIN Duplicates d ON d.AccountId = a.AccountId ORDER BY a.AccountId, a.Kind;
-- Section 2: old duplicate active shifts (must be empty).
SELECT CashAccountId, COUNT(*) AS ActiveShiftCount FROM dbo.tbl_CashShifts WHERE Status IN (1,2)
GROUP BY CashAccountId HAVING COUNT(*) > 1;
-- Section 3: cash/bank accounts without a configured currency (review).
SELECT N'Cash' AS Kind, Id, AccountId, Name AS DisplayName FROM dbo.tbl_CashAccounts WHERE CurrencyId IS NULL
UNION ALL SELECT N'Bank', Id, AccountId, CONCAT(BankName, N' - ', AccountName) FROM dbo.tbl_BankAccounts WHERE CurrencyId IS NULL;
-- Section 4: posted cash/bank lines without a reliable original currency amount.
-- Does not block the migration. Review before opening shifts or transferring these balances.
;WITH Accounts AS (
    SELECT AccountId, CurrencyId, Name AS DisplayName FROM dbo.tbl_CashAccounts
    UNION ALL SELECT AccountId, CurrencyId, CONCAT(BankName, N' - ', AccountName) FROM dbo.tbl_BankAccounts
)
SELECT a.DisplayName, a.AccountId, a.CurrencyId, j.JournalNumber, j.PostingDate, j.BaseCurrencyId,
    l.TransactionCurrencyId, l.TransactionDebitAmount, l.TransactionCreditAmount, l.DebitAmount, l.CreditAmount
FROM Accounts a JOIN dbo.tbl_JournalEntryLines l ON l.AccountId = a.AccountId
JOIN dbo.tbl_JournalEntries j ON j.Id = l.JournalEntryId
WHERE j.Status = 4 AND a.CurrencyId IS NOT NULL AND NOT (
    (l.TransactionCurrencyId IS NOT NULL AND l.TransactionCurrencyId = a.CurrencyId AND l.TransactionDebitAmount IS NOT NULL AND l.TransactionCreditAmount IS NOT NULL)
    OR (l.TransactionCurrencyId IS NULL AND j.BaseCurrencyId IS NOT NULL AND j.BaseCurrencyId = a.CurrencyId)
)
ORDER BY a.DisplayName, j.PostingDate, j.JournalNumber;
SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId DESC;
