using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Expenses;

namespace OAS.Application.Accounting.Expenses.Queries.GetExpenseById;

public sealed record GetExpenseByIdQuery(Guid Id)
    : IQuery<ExpenseDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Expenses.View];
}
