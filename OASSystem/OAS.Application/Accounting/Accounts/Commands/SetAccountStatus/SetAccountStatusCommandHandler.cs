using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Commands.SetAccountStatus;

public sealed class SetAccountStatusCommandHandler(
    IRepository<Account, Guid> repository)
    : IRequestHandler<SetAccountStatusCommand>
{
    public async Task Handle(
        SetAccountStatusCommand request,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetForUpdateAsync(
            request.AccountId,
            cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(
                nameof(Account),
                request.AccountId);
        }

        account.SetActive(request.Request.IsActive);

        repository.Update(account);
    }
}