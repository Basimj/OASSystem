using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Currencies.Commands.UpdateCurrency;

public sealed class UpdateCurrencyCommandHandler(
    IRepository<Currency, Guid> repository,
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<JournalEntry, Guid> journals,
    IReadRepository<JournalEntryLine, Guid> journalLines)
    : IRequestHandler<UpdateCurrencyCommand>
{
    public async Task Handle(UpdateCurrencyCommand request, CancellationToken ct)
    {
        var currency = await repository.GetForUpdateAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Currency), request.Id);
        byte[] rowVersion;
        try { rowVersion = Convert.FromBase64String(request.Request.RowVersion); }
        catch (FormatException ex) { throw new ConcurrencyException("Currency row version is invalid.", ex); }
        if (!rowVersion.SequenceEqual(currency.RowVersion))
            throw new ConcurrencyException("تم تعديل العملة بواسطة مستخدم آخر. حدّث الصفحة ثم حاول مجددًا.");

        var setup = await settings.GetByIdAsync(AccountingSettings.SingletonId, ct);
        var isBase = setup?.BaseCurrencyId == currency.Id;
        if (isBase && !request.Request.IsActive)
            throw new ConflictException("base_currency_deactivation_blocked",
                "لا يمكن تعطيل العملة الأساسية المحددة في إعدادات المحاسبة.");

        if (currency.DecimalPlaces != request.Request.DecimalPlaces)
        {
            // Precision affects financial interpretation of stored entries.
            // Keep existing currency snapshots stable after journal use.
            var usedAsBase = await journals.CountAsync(new Specification<JournalEntry>()
                .Where(x => x.BaseCurrencyId == currency.Id &&
                    (x.Status == JournalEntryStatus.Posted || x.Status == JournalEntryStatus.Reversed)), ct) > 0;
            var usedOnJournalLines = await journalLines.CountAsync(new Specification<JournalEntryLine>()
                .Where(x => x.TransactionCurrencyId == currency.Id), ct) > 0;
            if (usedAsBase || usedOnJournalLines)
                throw new ConflictException("currency_precision_change_blocked",
                    "لا يمكن تعديل دقة عملة مستخدمة في القيود المحاسبية. احتفظ بالدقة التاريخية الحالية.");
        }

        currency.Update(request.Request.NameAr, request.Request.NameEn, request.Request.Symbol,
            request.Request.DecimalPlaces, request.Request.IsActive);
        repository.Update(currency);
    }
}
