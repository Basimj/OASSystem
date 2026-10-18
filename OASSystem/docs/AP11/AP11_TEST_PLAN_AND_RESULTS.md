# AP-11 — SQL Server acceptance and safety gate

Source: `Basimj/OASSystem` `main` at commit `f1b5c9a9fb107c7480527d7a35756fea77a0dc73`, Issue #12. This AP-11 patch adds tests / read-only SQL checks / operational docs only; **no application production code, migration, posted journal, or database is changed by the patch**.

## Status of this deliverable

| Requirement | Evidence included | Result |
|---|---|---|
| Updated AP-10 source in `main` | GitHub read-only review | Confirmed files exist, not functional validation |
| Existing OAS tests for purchase matching, payment, supplier FX, allocation concurrency | Existing test suites referenced below | Not executed in current environment |
| SQL Server schema, migrations, constraints, idempotency | AP11 disposable migration fixture + preflight query | **NOT RUN — requires SQL Server** |
| Posted journal debit/credit, supplier isolation, cross-supplier allocations, oversettlement | AP11 read-only NUnit suite + reconciliation query | **NOT RUN — requires populated SQL Server replica** |
| Real 115,000 - 60,000 - 23,000 = 32,000 posted document | Optional NUnit fixture keyed by actual invoice ID | **NOT RUN — requires UI/API scenario and invoice ID** |
| 100 USD @1,500 settled @1,600 = FX loss 10,000 | Optional NUnit fixture keyed by actual allocation ID | **NOT RUN — requires UI/API scenario** |
| FX gain at 1,400 | Manual fixture instructions below | **NOT RUN** |
| Authentication/permission contract | NUnit structural guard | **NOT RUN**; HTTP 401/403 still mandatory |
| Multi-supplier PO→GRNI→Invoice→Payment→Return→Supplier statement/GL | Manual E2E acceptance steps + SQL reconciliation | **NOT RUN — not claimable from SQL invariants alone** |
| SQL concurrency and failure-injection, follow-up worker recovery | Existing AP03/AP09 tests; manual fault-injection instructions | **NOT RUN** |
| 100k invoice & allocation query plans | Read-only performance script | **NOT RUN** |
| Migration on restored production-like database and rollback rehearsal | Runbook below | **NOT RUN** |

**Release gate: BLOCKED** until all P0 scenarios have executed with a real SQL Server replica, verified journal/stock counts, screenshot/log evidence, and an authorized reviewer sign-off. Do not treat skipped/ignored NUnit cases as passes.

## Build, run and collect evidence

From repository root in PowerShell:

```powershell
dotnet restore OASSystem/OASSystem.sln
dotnet build OASSystem/OASSystem.sln --no-restore
dotnet test OASSystem/OAS.Tests/OAS.Tests.csproj --no-build --filter "Category=AP11" --logger "trx;LogFileName=AP11-Contracts.trx"
```

Run schema migration rehearsal against an **isolated, disposable** database. Connection string gives SERVER only; fixture creates a NEW `OAS_AP11_<GUID>` database, then deletes only that database. The SQL user must have the required CREATE DATABASE permission. Never point the `OAS_PURCHASING_TEST_SQLSERVER` variable at a sensitive production SQL instance:

```powershell
$env:OAS_PURCHASING_TEST_SQLSERVER = "Server=YOUR_TEST_SERVER;Database=master;Integrated Security=True;TrustServerCertificate=True"
dotnet test OASSystem/OAS.Tests/OAS.Tests.csproj --filter "Category=AP11DisposableSqlServer" --logger "trx;LogFileName=AP11-Migration.trx"
```

Run **read-only** invariants on a RESTORED production-like clone. The server login should be DB `db_datareader` only. The test fixture runs no `UPDATE`/`DELETE`/migrations:

```powershell
$env:OAS_AP11_ACCEPTANCE_SQLSERVER = "Server=YOUR_TEST_SERVER;Database=OAS_REPLICA_AP11;Integrated Security=True;TrustServerCertificate=True"
dotnet test OASSystem/OAS.Tests/OAS.Tests.csproj --filter "Category=AP11SqlServer" --logger "trx;LogFileName=AP11-SQL-Acceptance.trx"
```

**Optional scenario IDs:**

- `$env:OAS_AP11_INVOICE_115K_ID = "<real posted purchase invoice GUID>"` checks the actual 115k invoice, 60k allocated voucher payment and 23k applied return credit.
- `$env:OAS_AP11_AHMED_SUPPLIER_ID = "<real supplier GUID>"` checks November base-currency opening 30,000 and closing 90,000 from posted GL.
- `$env:OAS_AP11_MULTISUPPLIER_VOUCHER_ID = "<real posted voucher GUID>"` verifies at least two distinct supplier lines.
- `$env:OAS_AP11_FX_ALLOCATION_ID = "<real 100 USD allocation GUID>"` checks the allocation and FX loss posted journal (100 USD @1500, settlement @1600).
- `$env:OAS_AP11_REQUIRE_FULL_CURRENCY_SNAPSHOTS = "1"` makes legacy missing TargetDocumentAmount amounts fail instead of just reporting them. **Only enable after legacy cleanup**; do not backfill foreign-currency amounts by guessing.

All optional scenario cases call `Assert.Ignore` when no IDs are provided; **ignored != passed**. The fixture does not create fake invoices or payment allocations on the shared replica.

## Manual, authenticated E2E scenario matrix

1. In a disposable/restored database, configure base currency, two active supplier subledger accounts under a liability control header, posting roles (`PurchaseReceipt` Inventory/GRNI, `PurchaseInvoice` GRNI/Tax/PPV), one open accounting period and one locked period.
2. Create suppliers **A** and **B**; create a customer demand and purchase requests for separate orders, receive A partially then fully; receipt B separately. Verify each accepted receipt changes inventory only once and creates one GRNI source journal.
3. Match purchase invoices against the correct posted receipt lines. Verify tax/price variances and the invoice-to-GRNI clearing, and that A cannot match B's receipt. Assert SourceDocument uniqueness; journal base Debit=Credit.
4. Post one voucher with multiple distinct `PaymentVoucherLineId`s and supplier IDs. Allocate a partial amount to A's invoice and the other amount to B's. Reload UI and ensure A/B ledgers stay separate; use `01_reconciliation_read_only.sql` to prove it.
5. A: invoice document currency 115,000; payment allocation 60,000; posted return credit applied 23,000; remaining 32,000. Set `OAS_AP11_INVOICE_115K_ID`, rerun tests. **Do not treat an un-applied return as an applied credit.**
6. Supplier Ahmed: seed actual posted opening + November document movements, expect starting liability 30,000 and closing 90,000. Compare **API statement**, **CSV/XLSX**, **preview/print**, and SQL totals as-of November 30 (same base currency). Record screenshot/response hashes. Verify paginated statement ordering.
7. FX: create posted 100 USD invoice @1,500; settle fully at 1,600 and verify FX loss 10,000, zero invoice document balance, supplier AP cleared, one FX journal source per allocation ID. Repeat with @1,400 and verify FX gain 10,000. Reverse one allocation and confirm compensation journal once only.
8. Race: two independent app requests allocate against the same invoice/payment line concurrently. Verify one fails with conflict or serialization handling, no over-allocation, no duplicated journal/source and no negative outstanding. Run existing `PaymentAllocationsConcurrencySqlServerTests` on the disposable server.
9. Lock fiscal period. Attempt receipt/invoice/payment/return postings and FX journals. All should be rejected without half-written inventory/GRNI/AP or mutation of existing posted journals.
10. Fault injection: trigger a failure after receipt commit before customer/lab follow-up; restart API, confirm persisted `tbl_PurchasingIntegrationJobs` recover without additional receipt inventory/GL postings. Collect SourceDocument and inventory transaction IDs before/after.
11. HTTP permission matrix: anonymous → 401, logged-in without supplier+journal read → 403, authorized → 200 for supplier statements, balances, export CSV/XLSX and print. Check cross-supplier ID restrictions where tenant/party scoping applies. Inspect actual auth pipeline; controller `[Authorize]` alone **does not prove** permissions.
12. Reversal: post supplier credit, partially allocate it, reverse and verify historical as-of dates and supplier running balance without mutating original posted journals.

## SQL Server migration / backfill / rollback safety checklist

1. Take a **verified restore-capable** full backup of the source database (and transaction log if needed). Perform `DBCC CHECKDB` on a restored clone.
2. Capture baseline: `00_schema_preflight_read_only.sql`, last migration IDs in `__EFMigrationsHistory`, table counts, rowversion fields, SupplierGL sums for periods and affected due-date snapshots.
3. Generate EF migration **SQL script** on the same source commit. Review with DB owner for additive columns/indexes, `WITH CHECK` FK checks, case-insensitive role duplicates, historical `DueDate` unknowns and AP-09 follow-up backfill counts. Never re-run scripts from unrelated branches or hand-edit `__EFMigrationsHistory`.
4. Run `Database.MigrateAsync()` only on disposable fresh DB; then apply the identical migration chain on a restored populated clone. For legacy schema, take before/after reports and collect SQL errors, run twice to check safe no-op migrations.
5. Validate `01_reconciliation_read_only.sql` and all P0 functional tests before enabling features. Compare GL and supplier subledgers by base currency and financial period, do not sum unrelated transaction currencies.
6. For unsafe historical changes, stop and make a **forward corrective migration** after review. Never delete posted GL entries or `Down()` a migration dropping financial tables. Rollback app via deployment rollback; rollback data via tested SQL backup restore under change-control.
7. Keep redacted logs, TRX reports, API status/401/403 samples, migration script hash, counts before/after, FX journals and performance plan screenshots in the PR. Sign-off must include an independent finance reviewer.

## Key acceptance gaps after code delivery

- No real SQL Server, application login, or production-like datasets are accessible in this environment; nothing under AP-11 is described as passed.
- Read-only SQL invariant tests detect corruption/cross-supplier mismatches but **cannot prove** full end-to-end write workflows, race conditions under live transactions, screenshot UI, PDF/print equivalence, closed fiscal periods, or 100k performance. These remain mandatory.
- Do **not** close Issue #12 or merge AP-11 on the strength of tests that were never run.
