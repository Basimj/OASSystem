using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.CoreHardening.Services;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Commands.SetAccountStatus;

public sealed class SetAccountStatusCommandHandler(
    IRepository<Account, Guid> repository,
    IManagedAccountGuard managedAccounts,
    IAccountIntegrityService integrity) : IRequestHandler<SetAccountStatusCommand>
{
    public async Task Handle(SetAccountStatusCommand request, CancellationToken cancellationToken)
    {
        var account = await repository.GetForUpdateAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        try
        {
            if (!Convert.FromBase64String(request.Request.RowVersion).SequenceEqual(account.RowVersion))
                throw new ConcurrencyException("Account was modified by another user.");
        }
        catch (FormatException ex)
        {
            throw new ConcurrencyException("Account row version is invalid.", ex);
        }

        if (await managedAccounts.IsManagedAsync(account.Id, cancellationToken))
        {
            throw new ConflictException(
                "accounting_managed_account_status_forbidden",
                "This linked account status must be changed from its owning customer, supplier, employee, cash or bank screen.");
        }

        if (!request.Request.IsActive && account.IsActive)
            await integrity.ValidateDeactivationAsync(account, cancellationToken);

        account.SetActive(request.Request.IsActive);
        repository.Update(account);
    }
}
