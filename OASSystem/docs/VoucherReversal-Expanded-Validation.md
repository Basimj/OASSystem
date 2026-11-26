# Expanded voucher reversal — verification plan

Base: VoucherReversalToolbar_FiscalFilters_Full. No schema changes. The original remains posted and linked to its opposite. For recognized modern receipt/payment lines, the server checks the historical journal, account/party mappings, balance, fiscal period, shifts, bank matches and known dependent records. The preview returns eligibility, line count and active allocation count, not a guarantee: the final transaction repeats checks, and existing allocation commands validate invoices and reverse their FX journals.

## SQL Server acceptance tests (not executed here)

1. Independent receipt/payment on ordinary posting accounts (including eligible accounts outside revenue/expense); check opposite and no duplicate.
2. Customer receipt with no allocation and supplier payment with no allocation; verify party statement and available balance.
3. Customer receipt allocated partially to a posted sales invoice; reverse voucher; allocation retained with ReversedAtUtc; outstanding amount increases; no new allocation accepted from reversed voucher.
4. Supplier payment allocated to two posted purchase invoices; verify both allocations reversed and outstanding amounts restored. Repeat for quick pay with PurchaseInvoice reference and immediate sales with SalesInvoice reference.
5. Foreign currency with existing realized FX journal: original FX journal linked to a posted reversal; check amounts/rates and trial balance. If no FX journal existed, no new FX is fabricated.
6. Multi-line voucher with one invalid/blocked allocation: entire reversal rolls back (including prior nested saves). Check original journal link and allocation states unchanged.
7. Closed/required shift: preview blocked when no required shift open; after opening, verify opposite linked to current shift, old shift unchanged.
8. Insufficient cash, inactive or remapped account, closed fiscal period, bank match, advance/loan/payroll/credit application: block with appropriate reason.
9. Original and reversal in different periods for unallocated voucher: report dates and net amounts; confirm account statement source references.
10. Reversed voucher cannot be reused for manual allocation, customer advance or opening application.

Limitations: active invoice allocation reversals use current UTC date only because allocation reversal timestamps are used for as-of aging and there is no separate effective date. Dedicated advance, loan, payroll, opening balance and credit settlement workflows remain blocked. Noncash settlement lines and legacy incomplete snapshots remain blocked. Preview does not run allocation reversal commands or guarantee final success. No general concurrency or existing-data cleanup. Existing FX behavior is reused; historical missing or incorrect FX is not repaired. Compile and run tests on target .NET SDK/SQL Server before deployment.
