using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.BankAccounts.Mapping;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Commands.CreateBankAccount;

public sealed class CreateBankAccountCommandHandler(
    IRepository<BankAccount, Guid> repository,
    BankAccountMapper mapper)
    : IRequestHandler<CreateBankAccountCommand, BankAccount>
{
    public async Task<BankAccount> Handle(
        CreateBankAccountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
