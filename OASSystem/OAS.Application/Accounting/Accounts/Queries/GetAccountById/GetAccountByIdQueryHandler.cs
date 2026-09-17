using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Accounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Accounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Queries.GetAccountById;

public sealed class GetAccountByIdQueryHandler(
    IReadRepository<Account, Guid> repository,
    AccountMapper mapper)
    : IRequestHandler<GetAccountByIdQuery, AccountDto>
{
    public async Task<AccountDto> Handle(
        GetAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Account), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
