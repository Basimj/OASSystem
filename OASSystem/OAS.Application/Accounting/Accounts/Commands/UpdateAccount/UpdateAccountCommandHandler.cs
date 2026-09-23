using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Accounts.Mapping;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Commands.UpdateAccount;

public sealed class UpdateAccountCommandHandler(
    IRepository<Account, Guid> repository,
    AccountMapper mapper,
    IManagedAccountGuard managedAccounts)
    : IRequestHandler<UpdateAccountCommand, Account>
{
    public async Task<Account> Handle(UpdateAccountCommand request,CancellationToken cancellationToken)
    {
        var entity=await repository.GetForUpdateAsync(request.Id,cancellationToken)??throw new NotFoundException(nameof(Account),request.Id);
        try
        {
            if(!Convert.FromBase64String(request.Data.RowVersion).SequenceEqual(entity.RowVersion))
                throw new ConcurrencyException("Account was modified by another user.");
        }
        catch(FormatException ex){throw new ConcurrencyException("Account row version is invalid.",ex);}

        if(await managedAccounts.IsManagedAsync(entity.Id,cancellationToken))
            throw new ConflictException("accounting_managed_account_edit_forbidden","Customer and supplier accounts must be maintained from the customer or supplier screen.");

        if(!string.Equals(entity.Code,request.Data.Code.Trim(),StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("accounting_account_code_immutable","The account code cannot be changed after creation.");

        mapper.Update(request.Data,entity);repository.Update(entity);return entity;
    }
}
