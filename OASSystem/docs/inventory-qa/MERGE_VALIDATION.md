# Inventory V1 merge validation

Validation performed after merging the Inventory work onto the supplied `OASSystem-main` base:

- Inventory Domain and Contracts enum members/values are identical.
- Inventory API/Client routes and the six V1 pages are present.
- `InventoryInitial` and `InventorySchemaAlignment` migrations are present.
- Shared UiLib capabilities required by the Inventory pages are present.
- All Inventory FluentValidation/error codes found in `OAS.Application/Inventory` have Arabic and English resource entries.
- No duplicate keys were found in `.resx` files.
- No Accounting-specific source file was replaced during the merge.
- The supplied Accounting base still contains `PaymentAllocation.UpdateAllocatedAmount` and the existing `AccountingWorkspaceHost` partial files.
- The Stock Count create validator uses a local `parsedWarehouseId` rather than capturing the `out warehouseId` parameter in lambdas.
- No `bin`, `obj`, or `.vs` folders are included in the merged project.

A full `dotnet build` was not executed because the current execution environment does not have the .NET SDK installed. Run the solution build locally as the final compiler verification.
