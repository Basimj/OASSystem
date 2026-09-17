using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.Specifications;

public sealed class ExpensePageSpecification
{
    public ISpecification<Expense> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<Expense>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.ExpenseNumber.Contains(search) ||
                (x.Beneficiary != null && x.Beneficiary.Contains(search)) ||
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
            return nameof(Expense.ExpenseDate);

        return requested.Trim().ToLowerInvariant() switch
        {
            "expensenumber" => nameof(Expense.ExpenseNumber),
            "expensedate" => nameof(Expense.ExpenseDate),
            "amount" => nameof(Expense.Amount),
            "beneficiary" => nameof(Expense.Beneficiary),
            "status" => nameof(Expense.Status),
            "createdatutc" => nameof(Expense.CreatedAtUtc),
            _ => nameof(Expense.ExpenseDate)
        };
    }
}
