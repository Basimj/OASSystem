using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Commands.SetCashAccountStatus;

public sealed class SetCashAccountStatusCommandHandler(
    IRepository<CashAccount, Guid> repository)
    : IRequestHandler<SetCashAccountStatusCommand>
{
    public async Task Handle(
        SetCashAccountStatusCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(CashAccount), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The cash account has been modified by another user.");
        }

        entity.SetActive(request.Request.IsActive);
        repository.Update(entity);
    }
}
