using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentVouchers.Specifications;

public sealed class PaymentVoucherPageSpecification
{
    public ISpecification<PaymentVoucher> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<PaymentVoucher>();

        // Specification.Where replaces the previous predicate, so every active filter
        // must be combined into one expression BEFORE database pagination.
        var status = normalized.Status ?? string.Empty;
        var search = normalized.Search ?? string.Empty;
        specification.Where(x =>
            (status != "draft" || x.Status == PaymentVoucherStatus.Draft) &&
            (status != "approved" || x.Status == PaymentVoucherStatus.Approved) &&
            (status != "posted" || x.Status == PaymentVoucherStatus.Posted) &&
            (status != "cancelled" || x.Status == PaymentVoucherStatus.Cancelled) &&
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
            return nameof(PaymentVoucher.VoucherDate);

        return requested.Trim().ToLowerInvariant() switch
        {
            "vouchernumber" => nameof(PaymentVoucher.VoucherNumber),
            "voucherdate" => nameof(PaymentVoucher.VoucherDate),
            "totalamount" => nameof(PaymentVoucher.BaseTotalAmount),
            "basetotalamount" => nameof(PaymentVoucher.BaseTotalAmount),
            "status" => nameof(PaymentVoucher.Status),
            "createdatutc" => nameof(PaymentVoucher.CreatedAtUtc),
            _ => nameof(PaymentVoucher.VoucherDate)
        };
    }
}
