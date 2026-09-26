using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentVouchers.Specifications;

public sealed class PaymentVoucherPageSpecification
{
    public ISpecification<PaymentVoucher> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<PaymentVoucher>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.VoucherNumber.Contains(search) ||
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
