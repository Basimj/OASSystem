using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Accounting.Expenses.Queries.GetExpenses;

public sealed record GetExpensesQuery(PageRequest Request)
    : IQuery<PagedResult<ExpenseDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Expenses.View];
}
