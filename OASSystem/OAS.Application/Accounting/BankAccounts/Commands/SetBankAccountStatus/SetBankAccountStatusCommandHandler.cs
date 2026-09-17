using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Commands.SetBankAccountStatus;

public sealed class SetBankAccountStatusCommandHandler(
    IRepository<BankAccount, Guid> repository)
    : IRequestHandler<SetBankAccountStatusCommand>
{
    public async Task Handle(
        SetBankAccountStatusCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(BankAccount), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The bank account has been modified by another user.");
        }

        entity.SetActive(request.Request.IsActive);
        repository.Update(entity);
    }
}
