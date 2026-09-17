using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CashShifts;

namespace OAS.Application.Accounting.CashShifts.Commands.SetCashShiftStatus;

public sealed record SetCashShiftStatusCommand(Guid Id, SetCashShiftStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashShifts.Close];
}
