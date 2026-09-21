using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentAllocations.Specifications;

public sealed class PaymentAllocationPageSpecification
{
    public ISpecification<PaymentAllocation> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<PaymentAllocation>();

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
            return nameof(PaymentAllocation.AllocatedAtUtc);

        return requested.Trim().ToLowerInvariant() switch
        {
            "allocatedamount" => nameof(PaymentAllocation.AllocatedAmount),
            "allocatedatutc" => nameof(PaymentAllocation.AllocatedAtUtc),
            "paymentsourcetype" => nameof(PaymentAllocation.PaymentSourceType),
            "targetdocumenttype" => nameof(PaymentAllocation.TargetDocumentType),
            _ => nameof(PaymentAllocation.AllocatedAtUtc)
        };
    }
}
