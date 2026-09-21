using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Accounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Commands.UpdateAccount;

public sealed class UpdateAccountCommandHandler(
    IRepository<Account, Guid> repository,
    AccountMapper mapper)
    : IRequestHandler<UpdateAccountCommand, Account>
{
    public async Task<Account> Handle(
        UpdateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Account), request.Id);
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
