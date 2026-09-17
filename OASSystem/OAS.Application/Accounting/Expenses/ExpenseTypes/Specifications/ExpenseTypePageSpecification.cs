using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Specifications;

public sealed class ExpenseTypePageSpecification
    : ICrudSpecificationFactory<ExpenseType>
{
    public ISpecification<ExpenseType> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<ExpenseType>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.Code.Contains(search) ||
                x.NameAr.Contains(search) ||
                (x.NameEn != null && x.NameEn.Contains(search)));
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
            return nameof(ExpenseType.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "code" => nameof(ExpenseType.Code),
            "name" => nameof(ExpenseType.NameAr),
            "namear" => nameof(ExpenseType.NameAr),
            "nameen" => nameof(ExpenseType.NameEn),
            "isactive" => nameof(ExpenseType.IsActive),
            _ => nameof(ExpenseType.Code)
        };
    }
}
