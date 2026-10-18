# AP-12 | Purchasing and Supplier Performance — implementation & acceptance guide

**Base:** public GitHub `Basimj/OASSystem` main commit `0fcc2f917610410e79e7ee6f547f3560842f7933` (AP-11).

**Deployment:** Add/replace only the files in the accompanying ZIP. No EF migration and no writes to financial or purchasing tables. GitHub source was read only. Do not merge into main before your local Build, NUnit tests, SQL integration review and UI verification succeed.

## Data provenance and accounting safeguards

- Orders: `tbl_PurchaseOrders` joined to `tbl_PurchaseOrderLines` (document currency). Excludes Draft, Rejected and Cancelled; open orders are Sent/PartiallyReceived with at least one order line whose ordered quantity is greater than posted accepted receipts. A Closed/FullyReceived order is not open. Returns do **not** reopen purchase orders.
- Receipts: only `tbl_PurchaseReceipts.Status=Posted` with posted accepted `tbl_PurchaseReceiptLines`. Base receipt cost is `TotalAcceptedCost`. Rejected/draft quantities do not enter receipt totals.
- Invoices: only `tbl_PurchaseInvoices.Status=Posted`, joining invoice lines. Currency-specific invoice amount (`FinalAmount`) and base amount (`BaseFinalAmount`) are **distinct**. Header totals are never multiplied by receipt allocations. Price variance comes only from allocations attached to posted invoice lines, shown under the invoice's currency context.
- Returns: only `tbl_PurchaseReturns.Status=Posted` and matching posted receipt/supplier; base amount from return lines `SupplierGrossBaseAmount`. A pre-invoice return affects remaining uninvoiced receipt quantities; a post-invoice return is not deducted twice from the GRNI quantity proxy.
- Price history: read-only `SupplierPriceHistory` linked to `SupplierCatalogItem`, queried by effective date overlap; warehouse filter yields no rows because supplier price history is not warehouse-specific.
- Summary: **separate counts and amounts per document lifecycle**; receipt base cost, invoice base total and return base total must not be summed into one purchase total. Neither PO nor receipt creates supplier Accounts Payable. Invoice totals represent posted supplier invoices, **not** outstanding AP; use GL/Supplier Statement for AP balance. The quantity labelled `UninvoicedAccepted` is an operational indicator, not the booked GRNI journal balance.
- Dates: order date for orders, posting date for posted receipts/invoices/returns; history effective date for prices. Late delivery compares order-line/PO promised date to the earlier of selected period end and server UTC today, with only accepted receipts posted up to that date. PO outstanding, invoiced and returned quantities are also as-of the selected period end. Supplier lead-time average is measured from order date to actual receipt date per posted receipt line.
- Units: all reported quantities retain their purchase-document unit context. Do not aggregate quantities of different products or units into an overall numeric sum. Supplier, warehouse, item, currency and text filters are applied as SQL predicates.
- Server paging: max 100 items per page; export reads the same authorized DTO by page with a max of 10,000 line items. Narrow date/item filters for larger datasets; no unbounded browser download.
- Authorization: Application `GetPurchasingPerformanceQuery` requires Purchasing view roles for orders, receipts, invoices and catalog. API is `[Authorize]`, rate limited, no-store. Export goes through the same MediatR query/authorization pipeline; no bypass endpoints.

## Manual acceptance matrix — run on SQL Server, not an in-memory fake

1. Supplier Ahmed, one PO for 100 pieces, first posted receipt 40, second posted receipt 20, invoice matched 30: order line displays Ordered 100, Accepted 60, Invoiced 30, Open 40, Uninvoiced 30. Only posted documents count; confirmed/draft do not.
2. Post a pre-invoice return of 10: Returned 10, Uninvoiced 20, Open stays 40. Post an invoice-linked return of 5: Returned 15, Uninvoiced remains 20 (return is accounted against AP separately).
3. Closed PO with received less than ordered shows Open 0; sent PO with late promised date and missing accepted receipts is Late. Missing promised date does not mark Late.
4. Two suppliers/two currencies (YER and USD), verify the PO document amount retains its own currency; posted invoices and return amounts display base separately. Compare posted invoices' BaseFinalAmount to SQL and general ledger, *not* to unposted purchase orders.
5. Supplier item with price changes effective 1 Jan, 1 Feb: both versions show by their periods; check historical price records. Filtering by warehouse returns no price rows.
6. Filter each tab by SupplierId, WarehouseId, ProductVariantId, CurrencyId and posting/order dates. Verify pagination beyond 200 records and search for old document codes. Drill down from report to the original PO, posted receipt and posted invoice.
7. Test read-only user lacking one report permission, guest user, and unauthorized download: request must fail. Authorized `csv`, `xlsx` and browser print should contain values from the same report rows; export max 10k enforced.
8. Compare output to `SupplierStatement`/`AP Reconciliation`: posted invoice AP is not derived from order or GRNI amounts. Verify reversal, PPV, closed period, manual posting and currency FX policy using AP-11 integration suite.
9. Measure SQL query times on 10k/100k rows, observe execution plans and missing indexes; add indexes only after evidence and coordination with database owners. There are **no new migrations** in this package.

## Run locally

```powershell
dotnet restore OASSystem/OASSystem.sln
dotnet build OASSystem/OASSystem.sln --no-restore
dotnet test OASSystem/OAS.Tests/OAS.Tests.csproj --no-restore --filter "Category=AP12"
```

Open **/purchases/performance** from Purchasing home, or **Supplier Center → أداء مشتريات المورد**. Export CSV/XLSX and Print current visible page. Full printable multi-page rendition is outside this first implementation; export for complete bounded data.

**Actual validation status at delivery:** file/hash review and ZIP integrity only. No .NET SDK or SQL Server available to this execution environment. No claim of passing Build/SQL/UI tests is made. Keep Issue #13 open until manual SQL/security/e2e matrix passes.
