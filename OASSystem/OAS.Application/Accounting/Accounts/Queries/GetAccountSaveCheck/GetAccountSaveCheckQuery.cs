using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Accounts;

namespace OAS.Application.Accounting.Accounts.Queries.GetAccountSaveCheck;

/// <summary>
/// Inspect the proposed account update on Save without invoking a write.
/// This is not an authorization to bypass server-side validation.
/// </summary>
public sealed record GetAccountSaveCheckQuery(Guid Id, UpdateAccountRequest Data)
    : IRequest<AccountSaveCheckDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Accounts.Edit];
}
