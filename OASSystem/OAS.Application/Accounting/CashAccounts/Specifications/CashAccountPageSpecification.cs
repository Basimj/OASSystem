using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Specifications;

public sealed class CashAccountPageSpecification
    : ICrudSpecificationFactory<CashAccount>
{
    public ISpecification<CashAccount> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<CashAccount>();

        // A single predicate combines status and search (Where overwrites Criteria).
        var status = normalized.Status ?? string.Empty;
        var search = normalized.Search ?? string.Empty;
        specification.Where(x =>
            (status != "active" || x.IsActive) &&
            (status != "inactive" || !x.IsActive) &&
            (search == string.Empty || x.Code.Contains(search) ||
                x.Name.Contains(search)));

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
            return nameof(CashAccount.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "code" => nameof(CashAccount.Code),
            "name" => nameof(CashAccount.Name),
            "accountid" => nameof(CashAccount.AccountId),
            "isdefault" => nameof(CashAccount.IsDefault),
            "isactive" => nameof(CashAccount.IsActive),
            _ => nameof(CashAccount.Code)
        };
    }
}
