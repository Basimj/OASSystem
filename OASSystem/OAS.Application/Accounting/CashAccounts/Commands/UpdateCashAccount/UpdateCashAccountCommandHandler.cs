using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CashAccounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Commands.UpdateCashAccount;

public sealed class UpdateCashAccountCommandHandler(
    IRepository<CashAccount, Guid> repository,
    CashAccountMapper mapper)
    : IRequestHandler<UpdateCashAccountCommand, CashAccount>
{
    public async Task<CashAccount> Handle(
        UpdateCashAccountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(CashAccount), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The cash account has been modified by another user.");
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
