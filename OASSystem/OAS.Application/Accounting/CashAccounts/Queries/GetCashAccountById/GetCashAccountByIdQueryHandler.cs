using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CashAccounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Queries.GetCashAccountById;

public sealed class GetCashAccountByIdQueryHandler(
    IReadRepository<CashAccount, Guid> repository,
    CashAccountMapper mapper)
    : IRequestHandler<GetCashAccountByIdQuery, CashAccountDto>
{
    public async Task<CashAccountDto> Handle(
        GetCashAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(CashAccount), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
