using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.BankAccounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Queries.GetBankAccountById;

public sealed class GetBankAccountByIdQueryHandler(
    IReadRepository<BankAccount, Guid> repository,
    BankAccountMapper mapper)
    : IRequestHandler<GetBankAccountByIdQuery, BankAccountDto>
{
    public async Task<BankAccountDto> Handle(
        GetBankAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(BankAccount), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
