using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Specifications;

public sealed class FiscalYearPageSpecification
    : ICrudSpecificationFactory<FiscalYear>
{
    public ISpecification<FiscalYear> CreatePageSpecification(
        PageRequest request)
    {
        var normalized = request.Normalize();

        var specification = new Specification<FiscalYear>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search;

            specification.Where(x =>
                x.Code.Contains(search) ||
                x.Name.Contains(search));
        }

        var sortBy = normalized.SortBy?.ToLowerInvariant();

        switch (sortBy)
        {
            case "name":
                specification.AddSort(
                    nameof(FiscalYear.Name),
                    normalized.SortDirection);
                break;

            case "startdate":
                specification.AddSort(
                    nameof(FiscalYear.StartDate),
                    normalized.SortDirection);
                break;

            case "enddate":
                specification.AddSort(
                    nameof(FiscalYear.EndDate),
                    normalized.SortDirection);
                break;

            case "status":
                specification.AddSort(
                    nameof(FiscalYear.Status),
                    normalized.SortDirection);
                break;

            case "code":
            default:
                specification.AddSort(
                    nameof(FiscalYear.Code),
                    normalized.SortDirection);
                break;
        }

        specification.ApplyPaging(
            (normalized.PageNumber - 1) * normalized.PageSize,
            normalized.PageSize);

        return specification;
    }
}