using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CashShifts;

namespace OAS.Application.Accounting.CashShifts.Commands.CloseCashShift;

public sealed record CloseCashShiftCommand(Guid Id, SetCashShiftClosingRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashShifts.Close];
}
