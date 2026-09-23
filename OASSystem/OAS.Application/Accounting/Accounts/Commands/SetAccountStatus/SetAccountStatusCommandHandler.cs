using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Commands.SetAccountStatus;

public sealed class SetAccountStatusCommandHandler(IRepository<Account,Guid> repository,IManagedAccountGuard managedAccounts):IRequestHandler<SetAccountStatusCommand>
{
    public async Task Handle(SetAccountStatusCommand request,CancellationToken cancellationToken)
    {
        var account=await repository.GetForUpdateAsync(request.AccountId,cancellationToken)??throw new NotFoundException(nameof(Account),request.AccountId);
        try{if(!Convert.FromBase64String(request.Request.RowVersion).SequenceEqual(account.RowVersion))throw new ConcurrencyException("Account was modified by another user.");}
        catch(FormatException ex){throw new ConcurrencyException("Account row version is invalid.",ex);}
        if(await managedAccounts.IsManagedAsync(account.Id,cancellationToken))throw new ConflictException("accounting_managed_account_status_forbidden","Customer and supplier account status must be changed from the customer or supplier screen.");
        account.SetActive(request.Request.IsActive);repository.Update(account);
    }
}
