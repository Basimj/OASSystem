using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.CoreHardening.Services;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Journals;
using OAS.Domain.Accounting.Entities;
using DomainJournalStatus = OAS.Domain.Accounting.Enums.JournalEntryStatus;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;
using DomainRateType = OAS.Domain.Accounting.Enums.ExchangeRateType;

namespace OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;

public sealed class CreateJournalEntryCommandHandler(
    IRepository<JournalEntry, Guid> repository,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<AccountingSettings, Guid> settingsRepository,
    IReadRepository<Currency, Guid> currencies,
    IExchangeRateResolver rateResolver,
    ICurrencyRoundingService rounding,
    IPermissionChecker permissions,
    ISequenceNumberGenerator sequenceNumberGenerator,
    IJournalSubledgerIntegrityService subledgerIntegrity,
    IJournalSourceIntegrityService sourceIntegrity)
    : IRequestHandler<CreateJournalEntryCommand, Guid>
{
    public async Task<Guid> Handle(CreateJournalEntryCommand request, CancellationToken ct)
    {
        var data = request.Request;

        await sourceIntegrity.ValidateUniqueSourceAsync(
            data.SourceModule,
            data.SourceDocumentType,
            data.SourceDocumentId,
            cancellationToken: ct);

        var number = await ResolveNumberAsync(data.JournalNumber, data.PostingDate.Year, ct);
        var settings = await settingsRepository.GetByIdAsync(AccountingSettings.SingletonId, ct)
            ?? throw new ConflictException(
                "accounting_settings_required",
                "Accounting settings and base currency must be configured first.");
        var baseCurrency = await currencies.GetByIdAsync(settings.BaseCurrencyId, ct)
            ?? throw new ConflictException(
                "base_currency_missing",
                "Configured base currency does not exist.");

        var id = Guid.NewGuid();
        var journal = JournalEntry.Create(
            id,
            number,
            (DomainJournalType)(byte)data.JournalType,
            data.PostingDate,
            data.DocumentDate,
            data.FiscalPeriodId,
            data.Description,
            data.SourceModule,
            data.SourceDocumentType,
            data.SourceDocumentId,
            DomainJournalStatus.Draft);
        journal.SetBaseCurrencySnapshot(baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces);

        var lineNumber = 1;
        foreach (var line in data.Lines)
        {
            journal.AddLine(await BuildLineAsync(
                id,
                lineNumber++,
                data.DocumentDate,
                line,
                baseCurrency,
                ct));
        }

        await repository.AddAsync(journal, ct);
        return id;
    }

    private async Task<JournalEntryLine> BuildLineAsync(
        Guid journalId,
        int lineNumber,
        DateOnly date,
        CreateJournalEntryLineRequest line,
        Currency baseCurrency,
        CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(line.AccountId, ct)
            ?? throw new NotFoundException(nameof(Account), line.AccountId);
        if (!account.CanReceiveManualPosting())
        {
            throw new ConflictException(
                "journal_account_not_manual_postable",
                $"Account '{account.Code}' cannot receive manual journal posting.");
        }

        ValidateDirection(line.TransactionDebitAmount, line.TransactionCreditAmount);

        var currencyId = line.TransactionCurrencyId ?? baseCurrency.Id;
        var manualAllowed = line.ExchangeRate.HasValue &&
                            await permissions.HasPermissionAsync(AccountingPermissions.ExchangeRates.Override, ct);
        var rate = await rateResolver.ResolveAsync(
            currencyId,
            date,
            (DomainRateType)(byte)line.ExchangeRateType,
            line.ExchangeRate,
            manualAllowed,
            ct);

        var txDebit = rounding.Round(line.TransactionDebitAmount, rate.CurrencyDecimalPlaces);
        var txCredit = rounding.Round(line.TransactionCreditAmount, rate.CurrencyDecimalPlaces);
        var debit = txDebit > 0
            ? rounding.CalculateBaseAmount(txDebit, rate.Rate, rate.CurrencyDecimalPlaces, baseCurrency.DecimalPlaces)
            : 0m;
        var credit = txCredit > 0
            ? rounding.CalculateBaseAmount(txCredit, rate.Rate, rate.CurrencyDecimalPlaces, baseCurrency.DecimalPlaces)
            : 0m;

        var partyName = await subledgerIntegrity.ValidateAndResolvePartyNameAsync(
            account,
            line.CustomerId,
            line.SupplierId,
            line.EmployeeId,
            ct);

        return JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(),
            journalId,
            lineNumber,
            line.AccountId,
            debit,
            credit,
            rate.CurrencyId,
            rate.CurrencyCode,
            rate.CurrencyDecimalPlaces,
            txDebit,
            txCredit,
            rate.Rate,
            rate.RateDate,
            rate.RateType,
            rate.Source,
            line.Description,
            line.CustomerId,
            line.SupplierId,
            line.EmployeeId,
            partyName,
            line.CostCenterId,
            line.ProductVariantId,
            line.WarehouseId,
            null);
    }

    private static void ValidateDirection(decimal debit, decimal credit)
    {
        if (debit < 0 || credit < 0 || (debit > 0 && credit > 0) || (debit == 0 && credit == 0))
        {
            throw new ConflictException(
                "journal_line_amount_invalid",
                "A journal line must contain a positive debit or credit, but not both.");
        }
    }

    private async Task<string> ResolveNumberAsync(string? requested, int year, CancellationToken ct)
    {
        var number = requested?.Trim();
        if (!string.IsNullOrEmpty(number) &&
            !number.StartsWith($"JV-{year:0000}-", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                "journal_number_period_mismatch",
                "Reserved journal number does not match the posting year.");
        }

        if (string.IsNullOrEmpty(number))
        {
            for (var i = 0; i < 100; i++)
            {
                number = $"JV-{year:0000}-{await sequenceNumberGenerator.NextAsync($"JournalEntry-{year}", ct):000000}";
                if (await repository.CountAsync(
                        new Specification<JournalEntry>().Where(x => x.JournalNumber == number), ct) == 0)
                    break;
            }
        }

        if (string.IsNullOrEmpty(number) ||
            await repository.CountAsync(
                new Specification<JournalEntry>().Where(x => x.JournalNumber == number), ct) > 0)
        {
            throw new ConflictException(
                "journal_number_duplicate",
                "Journal number is already in use.");
        }

        return number;
    }
}
