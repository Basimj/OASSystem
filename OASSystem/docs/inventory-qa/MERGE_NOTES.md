# Inventory V1 merge notes

This branch was merged onto the supplied `OASSystem-main` base without replacing Accounting-specific source files.

## Inventory content retained/updated
- Inventory API controllers and client service wiring already present in the supplied base were preserved.
- Inventory UI pages were updated to the latest reviewed versions for Products, Warehouses, Balances, Transactions, Ledger, and Stock Counts.
- Product/Warehouse/Transaction/Stock Count validators and stock-count workflow handlers include the manual-QA fixes.
- Inventory average-cost and stock-count variance rounding match the SQL schema precision.
- Inventory enum values remain identical between Domain and Contracts.
- `InventorySchemaAlignment` migration remains included.

## Shared UiLib merge
Only the capabilities required by Inventory were merged into the supplied base versions:
- `UiDataTable`: headers, row templates, row selection/click, compact/fit/sidebar typography modes.
- `UiWorkspaceFilterBar`: compact layout mode.
- `UiInputText`, `UiSelect`, `UiInputDate`, `UiCheckbox`: field-level `ErrorText` support.
- Sidebar font-size CSS variables used by Inventory table typography.

The supplied base behavior (including existing `ValueExpression` fallback logic) was preserved.

## Accounting conflict guard
No Accounting-specific source file under the following paths was replaced:
- `OAS.Client/Accounting`
- `OAS.Application/Accounting`
- `OAS.Domain/Accounting`
- `OAS.Infrastructure/Accounting`
- `OAS.API/Accounting`
- `OAS.Contracts/Accounting`
- `Shared/UiLib/Components/Accounting`

The supplied base contains `PaymentAllocation.UpdateAllocatedAmount`, and the existing split `AccountingWorkspaceHost` partial files were preserved as-is.

## QA assets
Manual QA plans are in this folder. SQL seed/verification scripts are under `scripts/inventory`.
