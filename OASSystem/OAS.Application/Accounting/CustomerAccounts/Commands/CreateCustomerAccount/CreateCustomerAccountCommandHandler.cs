using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CustomerAccounts.Mapping;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CustomerAccounts.Commands.CreateCustomerAccount;

public sealed class CreateCustomerAccountCommandHandler(
    IRepository<CustomerAccount, Guid> repository,
    CustomerAccountMapper mapper)
    : IRequestHandler<CreateCustomerAccountCommand, CustomerAccount>
{
    public async Task<CustomerAccount> Handle(
        CreateCustomerAccountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
