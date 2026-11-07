using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalPeriods.Specifications;

public sealed class FiscalPeriodPageSpecification
    : Specification<FiscalPeriod>
{
    private static readonly HashSet<string> AllowedSortFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "PeriodNumber",
            "Name",
            "StartDate",
            "EndDate",
            "Status"
        };

    public FiscalPeriodPageSpecification(
        PageRequest request)
    {
        request = request.Normalize();

        // Specification.Where replaces the preceding predicate, so compose the
        // period/year constraints with text search in ONE expression.
        var search = request.Search;
        var yearId = request.FiscalYearId;
        if (yearId.HasValue && !string.IsNullOrWhiteSpace(search))
        {
            Where(x => x.FiscalYearId == yearId.Value &&
                (x.Name.Contains(search) || x.PeriodNumber.ToString().Contains(search)));
        }
        else if (yearId.HasValue)
        {
            Where(x => x.FiscalYearId == yearId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            Where(x => x.Name.Contains(search) || x.PeriodNumber.ToString().Contains(search));
        }

        var sortBy =
            AllowedSortFields.Contains(request.SortBy ?? string.Empty)
                ? request.SortBy!
                : "PeriodNumber";

        AddSort(
            sortBy,
            request.SortDirection);

        var skip =
            (request.PageNumber - 1) *
            request.PageSize;

        ApplyPaging(
            skip,
            request.PageSize);
    }
}