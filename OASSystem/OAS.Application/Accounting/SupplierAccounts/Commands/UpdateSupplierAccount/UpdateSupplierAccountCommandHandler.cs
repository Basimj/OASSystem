using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.SupplierAccounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.SupplierAccounts.Commands.UpdateSupplierAccount;

public sealed class UpdateSupplierAccountCommandHandler(
    IRepository<SupplierAccount, Guid> repository,
    SupplierAccountMapper mapper)
    : IRequestHandler<UpdateSupplierAccountCommand, SupplierAccount>
{
    public async Task<SupplierAccount> Handle(
        UpdateSupplierAccountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(SupplierAccount), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The supplier account has been modified by another user.");
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
