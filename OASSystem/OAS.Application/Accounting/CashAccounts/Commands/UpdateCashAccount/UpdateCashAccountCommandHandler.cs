using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.CashAccounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Commands.UpdateCashAccount;

public sealed class UpdateCashAccountCommandHandler(
    IRepository<CashAccount, Guid> repository,
    IReadRepository<Currency, Guid> currencies,
    ILinkedAccountingAccountProvisioningService linkedAccounts,
    CashAccountMapper mapper)
    : IRequestHandler<UpdateCashAccountCommand, CashAccount>
{
    public async Task<CashAccount> Handle(UpdateCashAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CashAccount), request.Id);
        if (!entity.RowVersion.SequenceEqual(Convert.FromBase64String(request.Data.RowVersion)))
            throw new ConcurrencyException("The cash account has been modified by another user.");

        var currency = await currencies.GetByIdAsync(request.Data.CurrencyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), request.Data.CurrencyId);
        if (!currency.IsActive) throw new ConflictException("cash_currency_inactive", "The selected currency is inactive.");

        mapper.Update(request.Data, entity);
        await linkedAccounts.SynchronizeAsync(entity.AccountId, entity.Name, entity.IsActive, cancellationToken);
        repository.Update(entity);
        return entity;
    }
}
