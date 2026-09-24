using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.SupplierAccounts.Mapping;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.SupplierAccounts.Commands.CreateSupplierAccount;

public sealed class CreateSupplierAccountCommandHandler(
    IRepository<SupplierAccount, Guid> repository,
    SupplierAccountMapper mapper)
    : IRequestHandler<CreateSupplierAccountCommand, SupplierAccount>
{
    public async Task<SupplierAccount> Handle(
        CreateSupplierAccountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
