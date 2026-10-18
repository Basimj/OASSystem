# SA-03 — Phase 1: Customer return-credit preview

This is a **read-only first increment**, NOT the full #17 implementation.

## Added
- `GetCustomerReturnCreditPreviewQuery` and a secured API route `GET /api/sales/customer-credits/{customerId}/preview`.
- Domain-independent calculator of return-induced excess credit: posted return reduces unpaid invoice first; only the remainder is a customer credit candidate.
- Separate invoice-currency and historical base amounts; reverse allocations ignored.
- Unit tests: paid return, partial payment, multiple returns, historical FX, invalid overpayment/over-return.

## Critical limitations
- `EstimatedUnappliedCredit` is **preliminary**. It does not yet subtract actual refunds or reapplications (not implemented). Never authorize a payout from this number.
- No migration, no refundable balance ledger, no GL/payment voucher creation, no write API, no UI action, no SQL concurrency proof yet.
- Prior posted returns are read; this increment does not modify their journals.
- This endpoint assumes a single system base currency for aggregation and existing posted sales-invoice data.

## Remaining before closing #17
1. Add `CustomerCreditApplication` (source return, refund/apply target, status, idempotency, rowversion), EF config/migration/model snapshot; reconcile legacy returns.
2. Apply to invoice under serializable locks without another GL posting; eliminate double deduction of returns in `SalesInvoiceBalanceService`.
3. Refund through `PaymentVoucherLine` with Dr AR / Cr cash/bank, authorization, correct posting period, cash-shift validation and FX handling.
4. Implement reversal workflows and source/target/refund locking; integrate settled amounts into this preview so it becomes an authoritative available balance.
5. Arabic UI, API tests, SQL Server E2E tests and accountant-reviewed migration/backfill.
