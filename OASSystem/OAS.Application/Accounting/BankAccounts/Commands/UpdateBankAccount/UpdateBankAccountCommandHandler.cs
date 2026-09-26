using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.BankAccounts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Commands.UpdateBankAccount;

public sealed class UpdateBankAccountCommandHandler(
    IRepository<BankAccount, Guid> repository,
    IReadRepository<Currency, Guid> currencies,
    ILinkedAccountingAccountProvisioningService linkedAccounts,
    BankAccountMapper mapper)
    : IRequestHandler<UpdateBankAccountCommand, BankAccount>
{
    public async Task<BankAccount> Handle(UpdateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BankAccount), request.Id);
        if (!string.Equals(entity.AccountNumber, request.Data.AccountNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("accounting_bank_account_number_immutable", "The bank account number cannot be changed after creation.");
        if (!entity.RowVersion.SequenceEqual(Convert.FromBase64String(request.Data.RowVersion)))
            throw new ConcurrencyException("The bank account has been modified by another user.");

        var currency = await currencies.GetByIdAsync(request.Data.CurrencyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), request.Data.CurrencyId);
        if (!currency.IsActive) throw new ConflictException("bank_currency_inactive", "The selected currency is inactive.");

        mapper.Update(request.Data, entity);
        await linkedAccounts.SynchronizeAsync(entity.AccountId, $"{entity.BankName} - {entity.AccountName}", entity.IsActive, cancellationToken);
        repository.Update(entity);
        return entity;
    }
}
