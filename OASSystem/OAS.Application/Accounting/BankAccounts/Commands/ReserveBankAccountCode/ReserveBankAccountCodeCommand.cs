using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.BankAccounts;

namespace OAS.Application.Accounting.BankAccounts.Commands.ReserveBankAccountCode;

public sealed record ReserveBankAccountCodeCommand
    : ICommand<BankAccountCodeReservationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.BankAccounts.Create];
}
