# SA-03 — Return credits, refunds and cross-invoice applications

## Policy (critical)

- An eligible credit is derived **only from a Posted sales return**, to the extent the
  return amount exceeded its source invoice's unpaid portion when considering current
  allocations. The return's original **Cr Customer AR** stays untouched.
- **Credit is denominated in BASE currency** using the return's historical base values.
  Applying that entitlement to a foreign-currency invoice consumes the target invoice's
  historical base carrying amount (`targetDocumentAmount * invoice.ExchangeRate`).
  A refund in foreign currency consumes the payout amount translated to base at today's
  actual payment-voucher exchange rate. This prevents fake FX gain/loss on a
  base-currency-denominated credit. The independent SA-02 invoice-payment FX path remains
  responsible for realised FX on customer collections.
- Application to another invoice is **subledger-only**. It adds no second credit to AR.
- Refund creates and posts exactly one `PaymentVoucher` at the current rate:
  **Dr Customer AR / Cr Cash or Bank**. No double journal for the return.
- Reversal of an invoice application restores the credit entitlement (no GL change).
  Reversal of a Posted refund REQUIRES a matched, Posted compensating ReceiptVoucherLine
  with exactly the same party, account, currency, source and base amounts; the refund
  voucher and its posted GL stay immutable.
- Idempotency keys (globally unique), status, rowversion, and SQL source-return locks
  prevent duplicate consumption and concurrent over-refund. Target invoice locking
  coordinates with SA-02 invoice-payment allocations.
- Existing posted returns are **not re-posted**. Their available credit is calculated
  read-only. Historic refunds occurring before this feature need manual reconciliation
  before enabling settlements in production to avoid resurrecting already-refunded credit.
- Customer advances use their **own liability accounts**, unchanged by this feature.

## Installation

Run `Install-SA03.ps1 -ProjectRoot <path-to-OASSystem-solution-folder>` from
an extracted archive. The script copies only the listed new/modified files; it
backs up and safely patches the three existing DI/snapshot files and the return UI.
No GitHub push is performed.

Run: `dotnet build OASSystem.sln` (or the solution under the root) and
`dotnet test OAS.Tests/OAS.Tests.csproj --filter 'FullyQualifiedName~CustomerCredit'`.
For SQL tests set `OAS_SALES_TEST_SQLSERVER` to a SQL Server connection with CREATE DATABASE.
Apply database migration only after rehearsing on a restored copy, with a verified backup.
The feature assumes SA-02 PaymentAllocations shared migration is already deployed.

## Migration note

The model snapshot in this repo is split across base + AP-05 partials. The
SA-03 partial adds the model; installation adds the corresponding one-line call
inside the AP-05 model builder to retain previous schema changes.

## Security & UX

- Credit application uses `sales.credit.use`.
- Refund additionally needs create, approve, post payment-voucher permissions.
- CustomerId comes from the authenticated endpoint route; all source and target customer
  IDs are rechecked server-side. Outstanding and entitlement caps are checked inside
  the same SQL transaction as the inserted application and posted refund voucher.
- Refund screen uses actual permitted cash/bank/currency lookups, not free-form GUIDs.
- A posted refund must not be reversed by merely changing its ledger application.
