SET NOCOUNT ON;

/* OAS Accounting Multi-Currency preflight.
   Run AFTER AccountingMultiCurrencyExpand and AFTER configuring BaseCurrencyId.
   This script is read-only. */

SELECT 'accounting_settings_missing' AS ProblemCode, NULL AS EntityId, NULL AS Reference
WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_AccountingSettings);

SELECT 'cash_currency_missing' AS ProblemCode, c.Id AS EntityId, c.Code AS Reference
FROM dbo.tbl_CashAccounts c
WHERE c.CurrencyId IS NULL;

SELECT 'bank_currency_missing' AS ProblemCode, b.Id AS EntityId, b.Code AS Reference
FROM dbo.tbl_BankAccounts b
WHERE b.CurrencyId IS NULL;

SELECT 'receipt_total_mismatch' AS ProblemCode, v.Id AS EntityId, v.VoucherNumber AS Reference
FROM dbo.tbl_ReceiptVouchers v
OUTER APPLY (SELECT SUM(l.Amount) AS LineTotal FROM dbo.tbl_ReceiptVoucherLines l WHERE l.ReceiptVoucherId=v.Id) x
WHERE ISNULL(x.LineTotal,0) <> v.TotalAmount;

SELECT 'payment_total_mismatch' AS ProblemCode, v.Id AS EntityId, v.VoucherNumber AS Reference
FROM dbo.tbl_PaymentVouchers v
OUTER APPLY (SELECT SUM(l.Amount) AS LineTotal FROM dbo.tbl_PaymentVoucherLines l WHERE l.PaymentVoucherId=v.Id) x
WHERE ISNULL(x.LineTotal,0) <> v.TotalAmount;

SELECT 'receipt_line_account_missing' AS ProblemCode, l.Id AS EntityId, v.VoucherNumber AS Reference
FROM dbo.tbl_ReceiptVoucherLines l
JOIN dbo.tbl_ReceiptVouchers v ON v.Id=l.ReceiptVoucherId
LEFT JOIN dbo.tbl_Accounts a ON a.Id=l.AccountId
WHERE a.Id IS NULL;

SELECT 'payment_line_account_missing' AS ProblemCode, l.Id AS EntityId, v.VoucherNumber AS Reference
FROM dbo.tbl_PaymentVoucherLines l
JOIN dbo.tbl_PaymentVouchers v ON v.Id=l.PaymentVoucherId
LEFT JOIN dbo.tbl_Accounts a ON a.Id=l.AccountId
WHERE a.Id IS NULL;

SELECT 'legacy_receipt_ambiguous_distribution' AS ProblemCode, l.Id AS EntityId, v.VoucherNumber AS Reference,
       l.AccountId AS OldLineAccountId, c.AccountId AS ExpectedCustomerAccountId
FROM dbo.tbl_ReceiptVoucherLines l
JOIN dbo.tbl_ReceiptVouchers v ON v.Id=l.ReceiptVoucherId
JOIN dbo.tbl_Customers c ON c.Id=v.CustomerId
WHERE v.CustomerId IS NOT NULL AND l.AccountId <> c.AccountId;

SELECT 'legacy_payment_ambiguous_distribution' AS ProblemCode, l.Id AS EntityId, v.VoucherNumber AS Reference,
       l.AccountId AS OldLineAccountId, s.AccountId AS ExpectedSupplierAccountId
FROM dbo.tbl_PaymentVoucherLines l
JOIN dbo.tbl_PaymentVouchers v ON v.Id=l.PaymentVoucherId
JOIN dbo.tbl_Suppliers s ON s.Id=v.SupplierId
WHERE v.SupplierId IS NOT NULL AND l.AccountId <> s.AccountId;

SELECT 'journal_unbalanced' AS ProblemCode, j.Id AS EntityId, j.JournalNumber AS Reference,
       SUM(l.DebitAmount) AS DebitTotal, SUM(l.CreditAmount) AS CreditTotal
FROM dbo.tbl_JournalEntries j
JOIN dbo.tbl_JournalEntryLines l ON l.JournalEntryId=j.Id
GROUP BY j.Id,j.JournalNumber
HAVING SUM(l.DebitAmount) <> SUM(l.CreditAmount);
