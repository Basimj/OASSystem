using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CashAccounts;

namespace OAS.Application.Accounting.CashAccounts.Commands.ReserveCashAccountCode;

public sealed record ReserveCashAccountCodeCommand
    : ICommand<CashAccountCodeReservationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashAccounts.Create];
}
