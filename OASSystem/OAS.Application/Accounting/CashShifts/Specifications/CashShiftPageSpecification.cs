using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashShifts.Specifications;

public sealed class CashShiftPageSpecification
{
    public ISpecification<CashShift> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<CashShift>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x => x.ShiftNumber.Contains(search));
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
            return nameof(CashShift.OpenedAtUtc);

        return requested.Trim().ToLowerInvariant() switch
        {
            "shiftnumber" => nameof(CashShift.ShiftNumber),
            "openedatutc" => nameof(CashShift.OpenedAtUtc),
            "openingbalance" => nameof(CashShift.OpeningBalance),
            "actualclosingbalance" => nameof(CashShift.ActualClosingBalance),
            "status" => nameof(CashShift.Status),
            _ => nameof(CashShift.OpenedAtUtc)
        };
    }
}
