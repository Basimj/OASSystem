using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Specifications;

public sealed class BankAccountPageSpecification
    : ICrudSpecificationFactory<BankAccount>
{
    public ISpecification<BankAccount> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<BankAccount>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.Code.Contains(search) ||
                x.BankName.Contains(search) ||
                x.AccountName.Contains(search) ||
                x.AccountNumber.Contains(search) ||
                (x.IBAN != null && x.IBAN.Contains(search)));
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
            return nameof(BankAccount.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "code" => nameof(BankAccount.Code),
            "bankname" => nameof(BankAccount.BankName),
            "accountname" => nameof(BankAccount.AccountName),
            "accountnumber" => nameof(BankAccount.AccountNumber),
            "iban" => nameof(BankAccount.IBAN),
            "accountid" => nameof(BankAccount.AccountId),
            "isactive" => nameof(BankAccount.IsActive),
            _ => nameof(BankAccount.Code)
        };
    }
}
