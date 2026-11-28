using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CashShifts;

namespace OAS.Application.Accounting.CashShifts.Commands.CreateCashShift;

public sealed record CreateCashShiftCommand(CreateCashShiftRequest Data)
    : ICommand<Guid>, IAuthorizedRequest
{
    internal Guid? GroupId { get; init; }

    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashShifts.Create];
}
