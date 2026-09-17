using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CustomerAccounts.Specifications;

public sealed class CustomerAccountPageSpecification
    : ICrudSpecificationFactory<CustomerAccount>
{
    public ISpecification<CustomerAccount> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<CustomerAccount>();

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
            return nameof(CustomerAccount.CreatedAtUtc);

        return requested.Trim().ToLowerInvariant() switch
        {
            "customerid" => nameof(CustomerAccount.CustomerId),
            "accountid" => nameof(CustomerAccount.AccountId),
            "controlaccountid" => nameof(CustomerAccount.ControlAccountId),
            "isactive" => nameof(CustomerAccount.IsActive),
            "createdatutc" => nameof(CustomerAccount.CreatedAtUtc),
            _ => nameof(CustomerAccount.CreatedAtUtc)
        };
    }
}
