using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Accounts.Mapping;
using OAS.Application.Accounting.CoreHardening.Services;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Commands.CreateAccount;

public sealed class CreateAccountCommandHandler(
    IRepository<Account, Guid> repository,
    AccountMapper mapper,
    IAccountIntegrityService integrity)
    : IRequestHandler<CreateAccountCommand, Account>
{
    public async Task<Account> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Create(request.Data);
        await integrity.ValidateHierarchyAsync(
            entity.Id,
            entity.ParentAccountId,
            entity.Level,
            cancellationToken);

        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
