using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.BankAccounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Commands.UpdateBankAccount;

public sealed class UpdateBankAccountCommandHandler(
    IRepository<BankAccount, Guid> repository,
    BankAccountMapper mapper)
    : IRequestHandler<UpdateBankAccountCommand, BankAccount>
{
    public async Task<BankAccount> Handle(
        UpdateBankAccountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(BankAccount), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The bank account has been modified by another user.");
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
