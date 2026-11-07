using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.CashShifts.Specifications;

public sealed class CashShiftPageSpecification
{
    public ISpecification<CashShift> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<CashShift>();

        var status = normalized.Status ?? string.Empty;
        var search = normalized.Search ?? string.Empty;
        specification.Where(x =>
            (status != "open" || x.Status == CashShiftStatus.Open) &&
            (status != "closing" || x.Status == CashShiftStatus.Closing) &&
            (status != "closed" || x.Status == CashShiftStatus.Closed) &&
            (status != "approved" || x.Status == CashShiftStatus.Approved) &&
            (search == string.Empty || x.ShiftNumber.Contains(search)));

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
