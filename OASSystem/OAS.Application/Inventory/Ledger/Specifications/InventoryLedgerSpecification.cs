using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;
using DomainMovementType = OAS.Domain.Enums.Inventory.InventoryMovementType;

namespace OAS.Application.Inventory.Ledger.Specifications;

public static class InventoryLedgerSpecification
{
    public static ISpecification<InventoryLedger> Create(
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        Guid? transactionId = null,
        DomainMovementType? movementType = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var normalized = request.Normalize();
        var specification = new Specification<InventoryLedger>();

        if (warehouseId.HasValue)
            specification.Where(x => x.WarehouseId == warehouseId.Value);

        if (productVariantId.HasValue)
            specification.Where(x => x.ProductVariantId == productVariantId.Value);

        if (transactionId.HasValue)
            specification.Where(x => x.TransactionId == transactionId.Value);

        if (movementType.HasValue)
            specification.Where(x => x.MovementType == movementType.Value);

        if (fromDate.HasValue)
            specification.Where(x => x.MovementDate >= fromDate.Value);

        if (toDate.HasValue)
            specification.Where(x => x.MovementDate <= toDate.Value);

        var sortBy = ResolveSortProperty(normalized.SortBy);
        specification.AddSort(sortBy, normalized.SortDirection);

        specification.ApplyPaging(
            (normalized.PageNumber - 1) * normalized.PageSize,
            normalized.PageSize);

        return specification;
    }

    private static string ResolveSortProperty(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return nameof(InventoryLedger.SequenceNumber);

        return requested.Trim().ToLowerInvariant() switch
        {
            "sequence" => nameof(InventoryLedger.SequenceNumber),
            "sequencenumber" => nameof(InventoryLedger.SequenceNumber),
            "movementdate" => nameof(InventoryLedger.MovementDate),
            "createdatutc" => nameof(InventoryLedger.CreatedAtUtc),
            "unitcost" => nameof(InventoryLedger.UnitCost),
            _ => nameof(InventoryLedger.SequenceNumber)
        };
    }
}
