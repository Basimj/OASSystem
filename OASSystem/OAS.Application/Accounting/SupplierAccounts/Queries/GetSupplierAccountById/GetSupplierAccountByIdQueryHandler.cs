using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.SupplierAccounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.SupplierAccounts.Queries.GetSupplierAccountById;

public sealed class GetSupplierAccountByIdQueryHandler(
    IReadRepository<SupplierAccount, Guid> repository,
    SupplierAccountMapper mapper)
    : IRequestHandler<GetSupplierAccountByIdQuery, SupplierAccountDto>
{
    public async Task<SupplierAccountDto> Handle(
        GetSupplierAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(SupplierAccount), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
