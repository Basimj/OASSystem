using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.SupplierAccounts.Specifications;

public sealed class SupplierAccountPageSpecification
    : ICrudSpecificationFactory<SupplierAccount>
{
    public ISpecification<SupplierAccount> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<SupplierAccount>();

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
            return nameof(SupplierAccount.CreatedAtUtc);

        return requested.Trim().ToLowerInvariant() switch
        {
            "supplierid" => nameof(SupplierAccount.SupplierId),
            "accountid" => nameof(SupplierAccount.AccountId),
            "controlaccountid" => nameof(SupplierAccount.ControlAccountId),
            "isactive" => nameof(SupplierAccount.IsActive),
            "createdatutc" => nameof(SupplierAccount.CreatedAtUtc),
            _ => nameof(SupplierAccount.CreatedAtUtc)
        };
    }
}
