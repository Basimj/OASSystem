using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
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

        // Linked ledgers and historical postings use the original currency.
        // Currency conversion is not supported by the ordinary edit command.
        if (entity.CurrencyId != request.Data.CurrencyId)
            throw new ConflictException("accounting_cash_currency_immutable",
                "The currency of an existing cash account cannot be changed. Create a new account for a different currency.");

        var currency = await currencies.GetByIdAsync(request.Data.CurrencyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), request.Data.CurrencyId);
        if (!currency.IsActive) throw new ConflictException("cash_currency_inactive", "The selected currency is inactive.");

        if (request.Data.IsDefault && request.Data.IsActive &&
            await repository.CountAsync(
                new Specification<CashAccount>().Where(x => x.CurrencyId == request.Data.CurrencyId && x.IsDefault && x.IsActive && x.Id != entity.Id),
                cancellationToken) > 0)
            throw new ConflictException("cash_default_already_exists", "An active default cash account already exists for this currency.");

        mapper.Update(request.Data, entity);
        await linkedAccounts.SynchronizeAsync(entity.AccountId, entity.Name, entity.IsActive, cancellationToken);
        repository.Update(entity);
        return entity;
    }
}
