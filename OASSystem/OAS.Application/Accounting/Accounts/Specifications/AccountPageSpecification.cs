using System.Linq.Expressions;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Specifications;

public sealed class AccountPageSpecification
    : ICrudSpecificationFactory<Account>
{
    public ISpecification<Account> CreatePageSpecification(
        PageRequest request)
    {
        var normalized = request.Normalize();

        var specification = new Specification<Account>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();

            specification.Where(BuildSearchPredicate(search));
        }

        var sortBy = ResolveSortProperty(normalized.SortBy);

        specification.AddSort(
            sortBy,
            normalized.SortDirection);

        specification.ApplyPaging(
            (normalized.PageNumber - 1) * normalized.PageSize,
            normalized.PageSize);

        return specification;
    }

    private static Expression<Func<Account, bool>> BuildSearchPredicate(
        string search)
    {
        return account =>
            account.Code.Contains(search) ||
            account.NameAr.Contains(search) ||
            (account.NameEn != null &&
             account.NameEn.Contains(search));
    }

    private static string ResolveSortProperty(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return nameof(Account.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "code" => nameof(Account.Code),
            "name" => nameof(Account.NameAr),
            "namear" => nameof(Account.NameAr),
            "nameen" => nameof(Account.NameEn),
            "level" => nameof(Account.Level),
            "accountclass" => nameof(Account.AccountClass),
            "accounttype" => nameof(Account.AccountType),
            "active" => nameof(Account.IsActive),
            "isactive" => nameof(Account.IsActive),
            _ => nameof(Account.Code)
        };
    }
}