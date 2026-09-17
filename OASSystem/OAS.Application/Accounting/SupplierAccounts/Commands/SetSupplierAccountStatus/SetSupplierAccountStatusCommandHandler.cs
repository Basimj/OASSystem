using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.SupplierAccounts.Commands.SetSupplierAccountStatus;

public sealed class SetSupplierAccountStatusCommandHandler(
    IRepository<SupplierAccount, Guid> repository)
    : IRequestHandler<SetSupplierAccountStatusCommand>
{
    public async Task Handle(
        SetSupplierAccountStatusCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(SupplierAccount), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The supplier account has been modified by another user.");
        }

        entity.SetActive(request.Request.IsActive);
        repository.Update(entity);
    }
}
