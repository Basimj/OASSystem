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

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search;

            Where(x =>
                x.Name.Contains(search) ||
                x.PeriodNumber.ToString().Contains(search));
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