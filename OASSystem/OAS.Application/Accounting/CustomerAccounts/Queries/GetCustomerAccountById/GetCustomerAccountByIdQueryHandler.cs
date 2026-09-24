using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CustomerAccounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CustomerAccounts.Queries.GetCustomerAccountById;

public sealed class GetCustomerAccountByIdQueryHandler(
    IReadRepository<CustomerAccount, Guid> repository,
    CustomerAccountMapper mapper)
    : IRequestHandler<GetCustomerAccountByIdQuery, CustomerAccountDto>
{
    public async Task<CustomerAccountDto> Handle(
        GetCustomerAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(CustomerAccount), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
