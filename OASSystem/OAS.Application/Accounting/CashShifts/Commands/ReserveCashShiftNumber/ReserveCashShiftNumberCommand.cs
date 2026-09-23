using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Common;

namespace OAS.Application.Accounting.CashShifts.Commands.ReserveCashShiftNumber;

public sealed record ReserveCashShiftNumberCommand : ICommand<AccountingNumberReservationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashShifts.Create];
}
