using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CashAccounts.Mapping;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Commands.CreateCashAccount;

public sealed class CreateCashAccountCommandHandler(
    IRepository<CashAccount, Guid> repository,
    CashAccountMapper mapper)
    : IRequestHandler<CreateCashAccountCommand, CashAccount>
{
    public async Task<CashAccount> Handle(
        CreateCashAccountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
