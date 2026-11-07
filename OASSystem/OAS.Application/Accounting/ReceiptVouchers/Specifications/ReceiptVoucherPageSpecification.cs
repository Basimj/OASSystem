using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.ReceiptVouchers.Specifications;

public sealed class ReceiptVoucherPageSpecification
{
    public ISpecification<ReceiptVoucher> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<ReceiptVoucher>();

        // Specification.Where replaces the previous predicate, so every active filter
        // must be combined into one expression BEFORE database pagination.
        var status = normalized.Status ?? string.Empty;
        var search = normalized.Search ?? string.Empty;
        specification.Where(x =>
            (status != "draft" || x.Status == ReceiptVoucherStatus.Draft) &&
            (status != "approved" || x.Status == ReceiptVoucherStatus.Approved) &&
            (status != "posted" || x.Status == ReceiptVoucherStatus.Posted) &&
            (status != "cancelled" || x.Status == ReceiptVoucherStatus.Cancelled) &&
            (search == string.Empty || x.VoucherNumber.Contains(search) ||
                (x.Description != null && x.Description.Contains(search))));

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
            return nameof(ReceiptVoucher.VoucherDate);

        return requested.Trim().ToLowerInvariant() switch
        {
            "vouchernumber" => nameof(ReceiptVoucher.VoucherNumber),
            "voucherdate" => nameof(ReceiptVoucher.VoucherDate),
            "totalamount" => nameof(ReceiptVoucher.BaseTotalAmount),
            "basetotalamount" => nameof(ReceiptVoucher.BaseTotalAmount),
            "status" => nameof(ReceiptVoucher.Status),
            "createdatutc" => nameof(ReceiptVoucher.CreatedAtUtc),
            _ => nameof(ReceiptVoucher.VoucherDate)
        };
    }
}
