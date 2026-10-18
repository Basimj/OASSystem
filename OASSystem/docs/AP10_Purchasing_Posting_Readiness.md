# AP-10 — Purchasing posting readiness and explicit setup

Base: `Basimj/OASSystem`, branch `main`, commit `9a129181fe99cfd01a086e9059a653b67f93a8ab` (18 October 2026).

## Changes

- `GET api/accounting/posting-profiles/purchasing-readiness?postingDate=2026-10-18[&supplierId=GUID][&requirePurchaseTax=true][&requirePriceVariance=true][&requireForeignCurrency=true]`
  - Authorized MediatR read-only query: requires `accounting.posting_profiles.view`, `accounting.settings.view`, `accounting.suppliers.view`.
  - Checks period overlaps/lock, base currency, relevant setup accounts (Inventory must be an Asset, GRNI must be a Liability), exact count of active Purchasing Receipt/Invoice profiles, required role cardinality and posting-account validity, supplier subledger/control parent.
  - Optional tax, PPV and FX warnings become blockers **when requested**. Actual transaction posting still validates its own source lines.
  - Returns `IsReady`, `BaseCurrencyCode`, issues with machine-readable code and severity. Never mutates data.
- UI: `/accounting/purchasing-readiness`, with date, supplier lookup, tax, price variance and FX checkboxes; linked from `/accounting/posting-profiles`. Supplier lookup is server-side and bounded.
- `ProfileAccountingPostingService` **stops creating or activating Purchasing profiles from an actual posting operation**. It validates the source roles/supplier prior to journal-number generation and still enforces balanced journal, posted period, correct currency and unique source document.
- Supplier posting uses the `Supplier.AccountId` dedicated active Liability/Credit Subledger below an active Liability/Credit Control parent; profile roles cannot use supplier subledger accounts.
- Non-Purchasing Optical legacy provisioning is left as-is to avoid changing a separate module's behavior without its owner's approval.
- Does not create database tables or indexes; no EF migration or existing posted-entry edits.

## Existing/legacy databases

1. Open Accounting settings; confirm base currency, inventory, GRNI, tax/PPV/FX accounts as applicable.
2. Open Posting Profiles. Create/activate **exactly one** profile for `Purchasing`/`PurchaseReceipt` with roles `Inventory`, `GoodsReceivedNotInvoiced`, and **exactly one** for `Purchasing`/`PurchaseInvoice` with `GoodsReceivedNotInvoiced`; add `PurchaseTax` and `PurchasePriceVariance` if transaction data will use them. Do not activate duplicate profiles.
3. Check all roles reference active, posting-enabled, non-Subledger accounts. Confirm supplier's Control parent is an active Liability/Credit account and supplier subledger is active.
4. Choose an unlocked fiscal period. Run readiness check; fix all `Block` issues. WARNING issues may become blockers for taxable or foreign-currency invoices.
5. POSTING IS NEVER AUTOMATICALLY AUTHORIZED BY `IsReady=true`; existing command/API permission and concurrency checks remain authoritative.
6. Legacy inactive Purchasing profiles stay inactive until a user with posting-profile edit permissions explicitly activates them.

## Verification

```powershell
# Run from the repo root
dotnet build OASSystem/OASSystem.sln
dotnet test OASSystem/OAS.Tests/OAS.Tests.csproj --filter "FullyQualifiedName~AP10|FullyQualifiedName~ProfileAccountingPostingServiceTests"
```

Integration tests still required with SQL Server: supplier A vs B, posted receipt/invoice/return/payment, GRNI vs AP, tax/PPV, FX, duplicate profile/role, stale period, closed period, unbalanced lines and idempotent SourceDocument, role permissions, legacy disabled profiles. Existing GL snapshots must remain unchanged. The delivered code was **not** built in this runtime (dotnet SDK unavailable), so these checks must run before promoting to main.

## Limitations

The read-only readiness report intentionally does **not** make a decision about a specific amount/rate, the existence of inventory stock, or all effects of reversals. Those remain in existing posting commands. This patch does not comprehensively retrofit manual voucher integrity checks or SQL Server integration tests. Future acceptance work should cover those separately before closing #11.
