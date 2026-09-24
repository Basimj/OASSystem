# Product / Inventory Integration

This implementation completes the first-stock workflow without moving warehouse or quantity fields into `Product`.

## Creation flow

`POST /api/inventory/products/stock-product` accepts:

- Product master data.
- An initial product variant when the product is a stock item.
- Optional frame/lens details.
- Optional opening inventory (`WarehouseId`, `Quantity`, `UnitCost`).

The application command is transactional through the existing MediatR `TransactionBehavior` / `IUnitOfWork` pipeline. When opening inventory is supplied, it creates an `Opening` inventory transaction and line, then calls the existing `IInventoryPostingService`. The product workflow never writes `InventoryBalance` or `InventoryLedger` directly.

## Business rules

- A newly created stock product must include an initial active variant.
- `SERVICE` products cannot be stock items and cannot receive opening inventory.
- Opening quantity must be greater than zero; unit cost cannot be negative.
- Opening inventory requires an active warehouse.
- Inventory transaction create/post paths validate active variant, active product, stock-product flag, and active warehouse server-side.
- A stock product cannot lose its final active variant through the variant update endpoint.
- A product with inventory balance/history cannot be converted to `SERVICE`.
- At most one warehouse can be both active and default. Migration `20260923090000_InventoryProductIntegrationRules` normalizes existing duplicates and creates the filtered unique index.

## Inventory balance screen

The balance page uses the selected active warehouse as context. It renders active variants belonging to active stock products and left-joins the selected warehouse's balance. Missing balance rows are displayed as zero values; no zero balance row is inserted into the database merely for display.

## Product list

The product list now shows:

- Stock-item flag.
- Variant count.
- Inventory status (`غير مخزني`, `بدون Variant`, `بدون Variant فعال`, `بدون حركة مخزنية`, `رصيد صفري`, `يوجد مخزون`).

## Verification commands

From the solution directory:

```bash
dotnet build OASSystem.sln
dotnet test OAS.Tests/OAS.Tests.csproj --filter "FullyQualifiedName~OAS.Tests.Inventory"
```

Apply pending database migrations through the application's existing database-update flow before exercising the UI.
