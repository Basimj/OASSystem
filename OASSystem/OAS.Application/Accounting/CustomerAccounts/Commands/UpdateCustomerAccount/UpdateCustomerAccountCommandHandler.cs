using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CustomerAccounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CustomerAccounts.Commands.UpdateCustomerAccount;

public sealed class UpdateCustomerAccountCommandHandler(
    IRepository<CustomerAccount, Guid> repository,
    CustomerAccountMapper mapper)
    : IRequestHandler<UpdateCustomerAccountCommand, CustomerAccount>
{
    public async Task<CustomerAccount> Handle(
        UpdateCustomerAccountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(CustomerAccount), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The customer account has been modified by another user.");
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
