using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.BankAccounts.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Commands.CreateBankAccount;

public sealed class CreateBankAccountCommandHandler(
    IRepository<BankAccount, Guid> repository,
    IReadRepository<Currency, Guid> currencies,
    ILinkedAccountingAccountProvisioningService linkedAccounts,
    ISequenceNumberGenerator sequences)
    : IRequestHandler<CreateBankAccountCommand, BankAccount>
{
    public async Task<BankAccount> Handle(
        CreateBankAccountCommand request,
        CancellationToken cancellationToken)
    {
        var data = request.Data;

        var currency = await currencies.GetByIdAsync(data.CurrencyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), data.CurrencyId);

        if (!currency.IsActive)
            throw new ConflictException(
                "bank_currency_inactive",
                "The selected currency is inactive.");

        var accountNumber = data.AccountNumber.Trim();

        if (await repository.CountAsync(
                new Specification<BankAccount>()
                    .Where(x => x.AccountNumber == accountNumber),
                cancellationToken) > 0)
        {
            throw new ConflictException(
                "bank_account_number_duplicate",
                "Bank account number is already in use.");
        }

        var code = data.Code?.Trim();

        if (string.IsNullOrWhiteSpace(code) ||
            await repository.CountAsync(BankAccountSpecifications.ByCode(code), cancellationToken) > 0)
        {
            code = await GenerateUniqueCodeAsync(cancellationToken);
        }

        var linkedName = $"{data.BankName.Trim()} - {data.AccountName.Trim()}";

        var account = await linkedAccounts.ProvisionBankAccountAsync(
            linkedName,
            data.IsActive,
            null,
            cancellationToken);

        var entity = BankAccount.Create(
            Guid.NewGuid(),
            code,
            data.BankName,
            data.AccountName,
            accountNumber,
            data.IBAN,
            account.Id,
            currency.Id,
            data.IsActive);

        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 100; i++)
        {
            var code = BankAccountCodeFormatter.Format(
                await sequences.NextAsync("BankAccountCodeSequence", cancellationToken));

            if (await repository.CountAsync(
                    BankAccountSpecifications.ByCode(code),
                    cancellationToken) == 0)
            {
                return code;
            }
        }

        throw new ConflictException(
            "accounting_bank_account_code_exhausted",
            "Unable to generate a unique bank account code.");
    }
}
