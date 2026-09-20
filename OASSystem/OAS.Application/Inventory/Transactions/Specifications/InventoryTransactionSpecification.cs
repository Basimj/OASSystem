using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;
using DomainStatus = OAS.Domain.Enums.Inventory.InventoryTransactionStatus;
using DomainType = OAS.Domain.Enums.Inventory.InventoryTransactionType;

namespace OAS.Application.Inventory.Transactions.Specifications;

public static class InventoryTransactionSpecification
{
    public static ISpecification<InventoryTransaction> Create(
        PageRequest request,
        DomainType? transactionType = null,
        DomainStatus? status = null,
        Guid? warehouseId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var normalized = request.Normalize();
        var specification = new Specification<InventoryTransaction>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.TransactionNumber.Contains(search) ||
                (x.Reason != null && x.Reason.Contains(search)) ||
                (x.Notes != null && x.Notes.Contains(search)));
        }

        if (transactionType.HasValue)
            specification.Where(x => x.TransactionType == transactionType.Value);

        if (status.HasValue)
            specification.Where(x => x.Status == status.Value);

        if (warehouseId.HasValue)
            specification.Where(x => x.SourceWarehouseId == warehouseId.Value || x.DestinationWarehouseId == warehouseId.Value);

        if (fromDate.HasValue)
            specification.Where(x => x.TransactionDate >= fromDate.Value);

        if (toDate.HasValue)
            specification.Where(x => x.TransactionDate <= toDate.Value);

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
            return nameof(InventoryTransaction.TransactionDate);

        return requested.Trim().ToLowerInvariant() switch
        {
            "transactionnumber" => nameof(InventoryTransaction.TransactionNumber),
            "number" => nameof(InventoryTransaction.TransactionNumber),
            "transactiondate" => nameof(InventoryTransaction.TransactionDate),
            "date" => nameof(InventoryTransaction.TransactionDate),
            "status" => nameof(InventoryTransaction.Status),
            "transactiontype" => nameof(InventoryTransaction.TransactionType),
            "type" => nameof(InventoryTransaction.TransactionType),
            _ => nameof(InventoryTransaction.TransactionDate)
        };
    }
}
