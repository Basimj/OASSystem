using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Common;

namespace OAS.Application.Accounting.Expenses.Commands.ReserveExpenseNumber;

public sealed record ReserveExpenseNumberCommand(DateOnly ExpenseDate) : ICommand<AccountingNumberReservationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.Expenses.Create];
}
