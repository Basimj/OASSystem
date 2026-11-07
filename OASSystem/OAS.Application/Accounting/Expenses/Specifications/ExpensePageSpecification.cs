using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Expenses.Specifications;

public sealed class ExpensePageSpecification
{
    public ISpecification<Expense> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<Expense>();

        // Compose a single Criteria: the Specification.Where implementation replaces
        // (rather than appends) previous Where calls.
        var status = normalized.Status ?? string.Empty;
        var search = normalized.Search ?? string.Empty;
        var dateFrom = normalized.DateFrom;
        var dateTo = normalized.DateTo;
        var typeId = normalized.ExpenseTypeId;
        PaymentMethod? method = normalized.PaymentMethod is >= 1 and <= 5
            ? (PaymentMethod)normalized.PaymentMethod.Value : null;
        specification.Where(x =>
            (status != "draft" || x.Status == ExpenseStatus.Draft) &&
            (status != "approved" || x.Status == ExpenseStatus.Approved) &&
            (status != "posted" || x.Status == ExpenseStatus.Posted) &&
            (status != "cancelled" || x.Status == ExpenseStatus.Cancelled) &&
            (!dateFrom.HasValue || x.ExpenseDate >= dateFrom.Value) &&
            (!dateTo.HasValue || x.ExpenseDate <= dateTo.Value) &&
            (!typeId.HasValue || x.ExpenseTypeId == typeId.Value) &&
            (!method.HasValue || x.PaymentMethod == method.Value) &&
            (search == string.Empty || x.ExpenseNumber.Contains(search) ||
                (x.Beneficiary != null && x.Beneficiary.Contains(search)) ||
                (x.Description != null && x.Description.Contains(search))));

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
