using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Specifications;

public sealed class CostCenterPageSpecification
    : ICrudSpecificationFactory<CostCenter>
{
    public ISpecification<CostCenter> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<CostCenter>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.Code.Contains(search) ||
                x.NameAr.Contains(search) ||
                (x.NameEn != null && x.NameEn.Contains(search)));
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
            return nameof(CostCenter.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "name" => nameof(CostCenter.NameAr),
            "namear" => nameof(CostCenter.NameAr),
            "nameen" => nameof(CostCenter.NameEn),
            "isactive" => nameof(CostCenter.IsActive),
            "code" => nameof(CostCenter.Code),
            _ => nameof(CostCenter.Code)
        };
    }
}
