# SA-01 — Sales Return Accounting & Quality Hardening

## Scope
This change hardens sales returns without rebuilding the existing sales workflow.

### Historical accounting lineage
- `dbo.tbl_JournalEntryLines.PostingRole` stores stable non-localized semantic roles.
- New sales invoice postings persist:
  - `SalesCustomerReceivable`
  - `SalesRevenue`
  - `SalesTax`
  - `SalesInventory`
  - `SalesCOGS`
- Sales return posting resolves the historical `AccountId` from the original posted invoice journal and never from `Description` or the currently active Posting Profile.
- Legacy reconstruction is intentionally conservative:
  - customer AR can be reconstructed from the unique customer debit header line;
  - COGS / Inventory can be reconstructed from `SourceDocumentLineId` plus debit/credit direction;
  - tax-free revenue can be reconstructed only when one unambiguous non-customer header credit exists;
  - ambiguous taxed legacy invoices remain without a role and return posting stops with a review-required error instead of guessing.

### Accounting retry / partial-post protection
- Sales-return accounting posting now reads **all** journals for the return source before creating a journal.
- A previous `Posted`/`Reversed` source journal makes a retry idempotent and returns the existing journal id.
- A previous `Draft`, `PendingApproval`, or `Approved` source journal is treated as an incomplete/partial posting and blocks the retry with `sales_return_journal_partial_posting`.
- More than one source journal is treated as an integrity error and blocks posting.
- `UX_JournalEntries_SourceDocument(SourceModule, SourceDocumentType, SourceDocumentId)` remains the SQL Server concurrency boundary that prevents two racing requests from persisting duplicate source journals.

### Return quality and quarantine
- `SalesReturnLine` stores `ReturnCondition`, `StockDisposition`, `InspectedAtUtc`, and `InspectedBy`.
- `Warehouse.AllowsSalesAllocation` differentiates sellable warehouses from quarantine/non-sales warehouses.
- Sales availability, reservation, checkout-related warehouse lookup, and optical material issue paths reject warehouses where `AllowsSalesAllocation = 0`.
- Custom / prescription-related lenses default to `NeedsInspection + Quarantine`; direct restock requires the explicit quality override permission.
- Posted quarantined lines can be inspected through the sales return API. A resellable item is released through an atomic inventory transfer from the quarantine warehouse to a sellable warehouse; after release the return line tracks the warehouse that physically holds the item. Damaged / still-inspection items remain quarantined.
- Inventory return posting is grouped by destination warehouse, uses the original cost snapshot, and is retry-idempotent when all expected warehouse transactions already exist. Partial/inconsistent warehouse posting is rejected for review.

### Return-window policy
- V1 policy snapshot is 30 days (`SalesReturnPolicy.DefaultReturnWindowDays`).
- The reference is the latest optical-job delivery date when available. If an invoice has an optical job with no recorded delivery yet, return creation is rejected. Direct/non-optical invoices without a delivery timestamp use the invoice posting date as the documented operational fallback.
- Overrides require a reason and `sales.returns.window_override`.

## Permissions
- `sales.returns.inspect`
- `sales.returns.window_override`
- `sales.returns.quality_override`

## Migration
`20261016080000_SalesReturnAccountingAndQualityHardening`

The migration is additive and guarded:
- adds nullable lineage / quality fields first;
- defaults existing warehouses to sellable (`AllowsSalesAllocation = 1`);
- performs only structurally safe role backfill;
- does **not** infer roles from localized journal descriptions;
- leaves ambiguous historical roles `NULL` for accounting review.

Before applying to a shared environment:
1. Back up the database.
2. Snapshot `sys.tables`, `sys.columns`, `sys.indexes`, `sys.foreign_keys`, and `__EFMigrationsHistory`.
3. Run the migration on a clone and report journal lines that still have `PostingRole IS NULL` for posted SalesInvoice sources.
4. Do not mass-fill ambiguous Revenue/Tax roles from `Description`.

## Verification
- Domain tests cover return quantity limits, quality policy, quarantine and release rules, and return-window audit snapshots.
- Resolver tests verify that changing journal text/language does not change historical account resolution and that ambiguous legacy taxed invoices are rejected for review.
- Accounting posting service tests verify:
  - completed journal retry is idempotent;
  - `Draft` / `PendingApproval` / `Approved` source journals block retry as partial posting;
  - duplicate source journals are rejected.
- Inventory posting service tests verify:
  - a multi-warehouse return creates one posted inventory transaction per destination warehouse;
  - original cost snapshots are preserved;
  - a completed retry does not post inventory movements twice.
- EF configuration tests verify:
  - `UX_JournalEntries_SourceDocument` remains unique;
  - `JournalEntry`, `SalesReturn`, and `SalesReturnLine` RowVersion properties are configured for optimistic concurrency.
- SQL Server opt-in tests (`OAS_SALES_TEST_SQLSERVER`) verify:
  - the hardened schema and migration are applied;
  - the source-journal unique index exists in SQL Server;
  - the relevant RowVersion columns are real SQL Server rowversion columns;
  - normal sales warehouse lookup excludes quarantine warehouses;
  - the SA-01 migration can be rolled back to `20261013203000_AccountingCoreHardening` and safely reapplied on a fresh owned test database.

### SQL Server execution
Set `OAS_SALES_TEST_SQLSERVER` to a SQL Server connection whose login is allowed to create/drop a test database, then run the `SalesSqlServer` category. The fixture creates uniquely named databases and deletes them after the tests.

Full API/UI cross-request E2E acceptance remains part of SA-07. SA-01 now provides the domain, application, persistence, SQL-concurrency, migration-recovery, inventory, and retry-hardening behavior required by that later E2E suite.
