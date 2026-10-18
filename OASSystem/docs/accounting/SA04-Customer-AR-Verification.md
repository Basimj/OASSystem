# SA-04 — Customer AR Statements, Aging, Reconciliation — Issue #18

Baseline: `Basimj/OASSystem` main commit `77bf16e0a477e16ee53085a71a91ce1d5dbfb9f3` (2026-10-18).

## Accounting rules

- Statement and reconciliation must read `JournalEntryLine.AccountId == Customer.AccountId` in **posted journal entries** and must **not** union all lines with `CustomerId`: cash/bank and tax lines must not double-count customer AR.
- Positive statement balance = debit customer receivable; negative = customer credit.
- Account statement opening = SUM(debit-credit) for posted journal lines strictly before FromDate. Closing = Opening + period debit - period credit. Pagination must not reset running balance.
- Aging: posted invoice BaseTotalAmount minus target allocated payments, posted returns on ORIGINAL invoice, and credit applications to OTHER invoices active on AsOf date. Credit applied to source invoice must not be counted twice with its own return. Never include an allocation, return or reversal that is effective after the report's AsOf date.
- Credit from refunded full-paid return is a separate credit entitlement, NOT negative invoice outstanding. Unapplied receipt and customer advance liability display separately.
- Multi-currency totals are base-currency only, original document currency shown as reference only. No editing posted journals.

## Manual scenarios on SQL Server test database

1. One customer opening Dr30,000; new posted invoices Dr200,000; posted returns Cr20,000; cash receipts Cr120,000 in AR -> statement closing Dr90,000. Do not aggregate receipt cash/bank line to customer AR.
2. Posted invoice 100,000, receipt 70,000 and return 30,000 -> aging outstanding zero; no second subtraction of the return if a credit application targets its original invoice.
3. Posted invoice 100,000, receipt 100,000 and return 30,000 -> invoice aging zero, unapplied return credit 30,000, AR balance -30,000 until refund/apply. Refund 30,000 -> credit zero. Apply 15,000 to another invoice -> remaining credit 15,000, target invoice outstanding reduced 15,000. Verify no duplicate GL.
4. AsOf before receipt/return/credit: none of the later transactions may be included. Re-run AsOf on transaction date and after.
5. Cross-customer receipts: ensure other customer AR is unchanged. Reversing journal is a posted compensating journal and must net with its original.
6. Multi-currency invoice/receipt: compare historical base amounts and realized FX posting to posted GL, not sum of USD/YER transaction amounts.
7. Pagination: page 2 first row opening/running balance must reflect page 1 + opening; export CSV must match API and UI. CSV text starting with formula operators must be escaped.
8. Access with account lacking `accounting.customers.view` permission: endpoints must return 403. Anonymous access: 401.
9. Reconcile: selected control account subtree GL less sum of customer subaccounts must equal zero, or report and preserve the difference. If common control parent cannot be resolved, the report requires explicit selection and does not claim reconciliation succeeded.
10. Validate customer / posted invoice report with 10k+ lines for pagination and query cost; profile SQL execution plans before adding shared indexes.

## Scope & constraints

No EF migration is required; the report reads existing Account, JournalEntry, JournalEntryLine, SalesInvoice, SalesReturn, CustomerCreditApplication, PaymentAllocation, ReceiptVoucher and CustomerAdvance tables. `CustomersPage.razor` and the Infrastructure DI registration are the two existing files intentionally modified; purchasing/AP services are untouched.

Important: Customer advances refunded/cancelled in legacy data have no immutable event timestamp in `CustomerAdvance`. An accurate liability balance for *prior* dates cannot be reconstructed from current `Status` alone; where this history is material, reconcile against historical posted liability GL and record the limitation. Data with unidentified historical journal roles should be reviewed, not altered silently. As-of settlement timestamp reporting uses UTC day cutoff. Production sign-off requires a Windows .NET 9 build and SQL Server integration evidence from the actual environment.
