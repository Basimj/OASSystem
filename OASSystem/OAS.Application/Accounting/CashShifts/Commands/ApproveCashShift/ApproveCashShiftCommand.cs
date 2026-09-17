using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;

namespace OAS.Application.Accounting.CashShifts.Commands.ApproveCashShift;

public sealed record ApproveCashShiftCommand(Guid Id, string RowVersion)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashShifts.Approve];
}
