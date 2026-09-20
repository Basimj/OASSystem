using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;
using DomainStatus = OAS.Domain.Enums.Inventory.StockCountStatus;

namespace OAS.Application.Inventory.StockCounts.Specifications;

public static class StockCountSpecification
{
    public static ISpecification<StockCount> Create(
        PageRequest request,
        Guid? warehouseId = null,
        DomainStatus? status = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null)
    {
        var normalized = request.Normalize();
        var specification = new Specification<StockCount>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.CountNumber.Contains(search) ||
                (x.Notes != null && x.Notes.Contains(search)));
        }

        if (warehouseId.HasValue)
            specification.Where(x => x.WarehouseId == warehouseId.Value);

        if (status.HasValue)
            specification.Where(x => x.Status == status.Value);

        if (fromDate.HasValue)
            specification.Where(x => x.CountDate >= fromDate.Value);

        if (toDate.HasValue)
            specification.Where(x => x.CountDate <= toDate.Value);

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
            return nameof(StockCount.CountDate);

        return requested.Trim().ToLowerInvariant() switch
        {
            "countnumber" => nameof(StockCount.CountNumber),
            "number" => nameof(StockCount.CountNumber),
            "countdate" => nameof(StockCount.CountDate),
            "date" => nameof(StockCount.CountDate),
            "status" => nameof(StockCount.Status),
            _ => nameof(StockCount.CountDate)
        };
    }
}
