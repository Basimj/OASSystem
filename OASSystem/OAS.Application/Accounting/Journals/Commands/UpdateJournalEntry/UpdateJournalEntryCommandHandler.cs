using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Journals;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Entities;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;
using DomainRateType = OAS.Domain.Accounting.Enums.ExchangeRateType;

namespace OAS.Application.Accounting.Journals.Commands.UpdateJournalEntry;

public sealed class UpdateJournalEntryCommandHandler(
    IRepository<JournalEntry, Guid> repository,
    IRepository<JournalEntryLine, Guid> lineRepository,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Supplier, Guid> suppliers,
    IReadRepository<Employee, Guid> employees,
    IReadRepository<AccountingSettings, Guid> settingsRepository,
    IReadRepository<Currency, Guid> currencies,
    IExchangeRateResolver rateResolver,
    ICurrencyRoundingService rounding,
    IPermissionChecker permissions)
    : IRequestHandler<UpdateJournalEntryCommand, Guid>
{
    public async Task<Guid> Handle(UpdateJournalEntryCommand request, CancellationToken ct)
    {
        var journal = await repository.GetForUpdateAsync(request.JournalEntryId, ct)
            ?? throw new NotFoundException(nameof(JournalEntry), request.JournalEntryId);

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!journal.RowVersion.SequenceEqual(requestedRowVersion))
            throw new ConcurrencyException("The journal entry has been modified by another user.");

        journal.EnsureEditable();

        var settings = await settingsRepository.GetByIdAsync(AccountingSettings.SingletonId, ct)
            ?? throw new ConflictException("accounting_settings_required", "Accounting settings and base currency must be configured first.");
        var baseCurrency = await currencies.GetByIdAsync(settings.BaseCurrencyId, ct)
            ?? throw new ConflictException("base_currency_missing", "Configured base currency does not exist.");

        journal.UpdateDraft(
            (DomainJournalType)(byte)request.Request.JournalType,
            request.Request.PostingDate,
            request.Request.DocumentDate,
            request.Request.FiscalPeriodId,
            request.Request.Description,
            request.Request.SourceModule,
            request.Request.SourceDocumentType,
            request.Request.SourceDocumentId);
        journal.SetBaseCurrencySnapshot(baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces);

        var existingLines = await lineRepository.ListAsync(
            new Specification<JournalEntryLine>()
                .Where(x => x.JournalEntryId == journal.Id)
                .Tracking(), ct);
        if (existingLines.Count > 0)
            lineRepository.DeleteRange(existingLines);

        var newLines = new List<JournalEntryLine>(request.Request.Lines.Count);
        var lineNumber = 1;
        foreach (var line in request.Request.Lines)
            newLines.Add(await BuildLineAsync(journal.Id, lineNumber++, request.Request.DocumentDate, line, baseCurrency, ct));

        journal.ReplaceLines(newLines);
        if (newLines.Count > 0)
            await lineRepository.AddRangeAsync(newLines, ct);

        repository.Update(journal);
        return journal.Id;
    }

    private async Task<JournalEntryLine> BuildLineAsync(
        Guid journalId, int lineNumber, DateOnly date, CreateJournalEntryLineRequest line, Currency baseCurrency, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(line.AccountId, ct)
            ?? throw new NotFoundException(nameof(Account), line.AccountId);
        if (!account.CanReceiveManualPosting())
            throw new ConflictException("journal_account_not_manual_postable", $"Account '{account.Code}' cannot receive manual journal posting.");

        ValidateDirection(line.TransactionDebitAmount, line.TransactionCreditAmount);

        var currencyId = line.TransactionCurrencyId ?? baseCurrency.Id;
        var manualAllowed = line.ExchangeRate.HasValue &&
                            await permissions.HasPermissionAsync(AccountingPermissions.ExchangeRates.Override, ct);
        var rate = await rateResolver.ResolveAsync(
            currencyId, date, (DomainRateType)(byte)line.ExchangeRateType, line.ExchangeRate, manualAllowed, ct);

        var txDebit = rounding.Round(line.TransactionDebitAmount, rate.CurrencyDecimalPlaces);
        var txCredit = rounding.Round(line.TransactionCreditAmount, rate.CurrencyDecimalPlaces);
        var debit = txDebit > 0
            ? rounding.CalculateBaseAmount(txDebit, rate.Rate, rate.CurrencyDecimalPlaces, baseCurrency.DecimalPlaces)
            : 0m;
        var credit = txCredit > 0
            ? rounding.CalculateBaseAmount(txCredit, rate.Rate, rate.CurrencyDecimalPlaces, baseCurrency.DecimalPlaces)
            : 0m;

        var party = await ResolvePartySnapshotAsync(line, ct);
        return JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journalId, lineNumber, line.AccountId,
            debit, credit,
            rate.CurrencyId, rate.CurrencyCode, rate.CurrencyDecimalPlaces,
            txDebit, txCredit, rate.Rate, rate.RateDate, rate.RateType, rate.Source,
            line.Description, line.CustomerId, line.SupplierId, line.EmployeeId, party,
            line.CostCenterId, line.ProductVariantId, line.WarehouseId, null);
    }

    private async Task<string?> ResolvePartySnapshotAsync(CreateJournalEntryLineRequest line, CancellationToken ct)
    {
        var count = (line.CustomerId.HasValue ? 1 : 0) +
                    (line.SupplierId.HasValue ? 1 : 0) +
                    (line.EmployeeId.HasValue ? 1 : 0);
        if (count > 1)
            throw new ConflictException("journal_party_invalid", "Only one customer, supplier or employee can be selected per journal line.");

        if (line.CustomerId is Guid customerId)
        {
            var customer = await customers.GetByIdAsync(customerId, ct)
                ?? throw new NotFoundException(nameof(Customer), customerId);
            if (!customer.IsActive) throw new ConflictException("journal_customer_inactive", "The selected customer is inactive.");
            return customer.NameAr;
        }
        if (line.SupplierId is Guid supplierId)
        {
            var supplier = await suppliers.GetByIdAsync(supplierId, ct)
                ?? throw new NotFoundException(nameof(Supplier), supplierId);
            if (!supplier.IsActive) throw new ConflictException("journal_supplier_inactive", "The selected supplier is inactive.");
            return supplier.NameAr;
        }
        if (line.EmployeeId is Guid employeeId)
        {
            var employee = await employees.GetByIdAsync(employeeId, ct)
                ?? throw new NotFoundException(nameof(Employee), employeeId);
            if (!employee.IsActive) throw new ConflictException("journal_employee_inactive", "The selected employee is inactive.");
            return employee.DisplayName;
        }
        return null;
    }

    private static void ValidateDirection(decimal debit, decimal credit)
    {
        if (debit < 0 || credit < 0 || (debit > 0 && credit > 0) || (debit == 0 && credit == 0))
            throw new ConflictException("journal_line_amount_invalid", "A journal line must contain a positive debit or credit, but not both.");
    }
}
