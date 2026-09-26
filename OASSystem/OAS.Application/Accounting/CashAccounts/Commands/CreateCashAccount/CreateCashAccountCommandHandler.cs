using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.CashAccounts.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Commands.CreateCashAccount;

public sealed class CreateCashAccountCommandHandler(
    IRepository<CashAccount, Guid> repository,
    IReadRepository<Currency, Guid> currencies,
    ILinkedAccountingAccountProvisioningService linkedAccounts,
    ISequenceNumberGenerator sequences)
    : IRequestHandler<CreateCashAccountCommand, CashAccount>
{
    public async Task<CashAccount> Handle(
        CreateCashAccountCommand request,
        CancellationToken cancellationToken)
    {
        var data = request.Data;

        var currency = await currencies.GetByIdAsync(data.CurrencyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), data.CurrencyId);

        if (!currency.IsActive)
            throw new ConflictException(
                "cash_currency_inactive",
                "The selected currency is inactive.");

        var code = data.Code?.Trim();

        // The UI reserves a code when a new tab opens. If the request comes from
        // Excel/API without a code, or the reserved code became occupied before
        // save, generate the next available code instead of returning 409.
        if (string.IsNullOrWhiteSpace(code) ||
            await repository.CountAsync(CashAccountSpecifications.ByCode(code), cancellationToken) > 0)
        {
            code = await GenerateUniqueCodeAsync(cancellationToken);
        }

        var account = await linkedAccounts.ProvisionCashAccountAsync(
            data.Name.Trim(),
            data.IsActive,
            null,
            cancellationToken);

        var entity = CashAccount.Create(
            Guid.NewGuid(),
            code,
            data.Name,
            account.Id,
            currency.Id,
            data.IsDefault,
            data.IsActive);

        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 100; i++)
        {
            var code = CashAccountCodeFormatter.Format(
                await sequences.NextAsync("CashAccountCodeSequence", cancellationToken));

            if (await repository.CountAsync(
                    CashAccountSpecifications.ByCode(code),
                    cancellationToken) == 0)
            {
                return code;
            }
        }

        throw new ConflictException(
            "accounting_cash_account_code_exhausted",
            "Unable to generate a unique cash account code.");
    }
}
