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

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            if (Guid.TryParse(search, out var searchId))
            {
                specification.Where(x =>
                    x.PaymentSourceId == searchId ||
                    x.TargetDocumentId == searchId);
            }
            else if (Enum.TryParse<OAS.Domain.Accounting.Enums.PaymentSourceType>(search, true, out var sourceType))
            {
                specification.Where(x => x.PaymentSourceType == sourceType);
            }
            else if (Enum.TryParse<OAS.Domain.Accounting.Enums.AllocationTargetDocumentType>(search, true, out var targetType))
            {
                specification.Where(x => x.TargetDocumentType == targetType);
            }
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
