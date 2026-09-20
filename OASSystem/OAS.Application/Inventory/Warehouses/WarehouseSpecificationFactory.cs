using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Warehouses;

public sealed class WarehouseSpecificationFactory : ICrudSpecificationFactory<Warehouse>
{
    public ISpecification<Warehouse> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<Warehouse>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.Code.Contains(search) ||
                x.NameAr.Contains(search) ||
                (x.NameEn != null && x.NameEn.Contains(search)) ||
                (x.Description != null && x.Description.Contains(search)));
        }

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
            return nameof(Warehouse.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "code" => nameof(Warehouse.Code),
            "name" => nameof(Warehouse.NameAr),
            "namear" => nameof(Warehouse.NameAr),
            "nameen" => nameof(Warehouse.NameEn),
            "isdefault" => nameof(Warehouse.IsDefault),
            "isactive" => nameof(Warehouse.IsActive),
            _ => nameof(Warehouse.Code)
        };
    }
}
