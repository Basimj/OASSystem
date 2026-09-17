using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Queries.GetExpenseTypes;

public sealed record GetExpenseTypesQuery(PageRequest Request)
    : GetEntityPageQuery<ExpenseType, Guid, ExpenseTypeDto>(Request),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.ExpenseTypes.View];
}
