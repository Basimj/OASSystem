using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Authorization;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Contracts.Inventory.Transactions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.Transactions.Openings;

public sealed record SaveInventoryOpening(Guid? Id, InventoryOpeningRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [InventoryPermissions.Openings.Edit];
}
public sealed record DeleteInventoryOpening(Guid Id) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [InventoryPermissions.Openings.Edit];
}
public sealed record PostInventoryOpening(Guid Id) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [InventoryPermissions.Openings.Post];
}
public sealed record ReverseInventoryOpening(Guid Id, ReverseInventoryOpeningRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [InventoryPermissions.Openings.Post];
}
public sealed record GetInventoryOpeningInfo : IQuery<InventoryOpeningInfo>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [InventoryPermissions.Openings.View];
}

// Dedicated entry point: ordinary inbound inventory transactions remain prohibited.
public sealed class InventoryOpeningHandler(
    IInventoryTransactionRepository transactions,
    IRepository<InventoryTransactionLine, Guid> lines,
    IReadRepository<Warehouse, Guid> warehouses,
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<Product, Guid> products,
    IInventoryBalanceRepository balances,
    IReadRepository<InventoryLedger, Guid> ledger,
    IReadRepository<PostingProfile, Guid> profiles,
    IReadRepository<PostingProfileLine, Guid> profileLines,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<Currency, Guid> currencies,
    IRepository<JournalEntry, Guid> journals,
    IReadRepository<JournalEntryLine, Guid> journalDetails,
    IReadRepository<FiscalPeriod, Guid> periods,
    IInventoryPostingService stockPosting,
    IProfileAccountingPostingService accounting,
    ISequenceNumberGenerator sequence,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<SaveInventoryOpening, Guid>, IRequestHandler<DeleteInventoryOpening, Guid>,
      IRequestHandler<PostInventoryOpening, Guid>, IRequestHandler<ReverseInventoryOpening, Guid>, IRequestHandler<GetInventoryOpeningInfo, InventoryOpeningInfo>
{
    public const string Module = "Inventory";
    public const string Document = "InventoryOpening";
    private const string Reference = "InventoryOpening";
    private static ConflictException Error(string code, string message) => new(code, message);
    private Task<IReadOnlyList<PostingProfile>> ActiveProfiles(CancellationToken ct) => profiles.ListAsync(
        new Specification<PostingProfile>().Where(x => x.IsActive && x.Module == Module && x.DocumentType == Document), ct);

    public async Task<InventoryOpeningInfo> Handle(GetInventoryOpeningInfo request, CancellationToken ct)
    {
        var config = await settings.GetByIdAsync(AccountingSettings.SingletonId, ct);
        var currency = config is null ? null : await currencies.GetByIdAsync(config.BaseCurrencyId, ct);
        var reversed = await transactions.ListAsync(new Specification<InventoryTransaction>().Where(x => x.ReferenceType == "InventoryOpeningReversal" && x.Status == InventoryTransactionStatus.Posted && x.ReferenceId.HasValue), ct);
        var reversalJournals = await journals.ListAsync(new Specification<JournalEntry>().Where(x => x.SourceModule == Module && x.SourceDocumentType == "InventoryOpeningReversal" && x.Status == JournalEntryStatus.Posted && x.SourceDocumentId.HasValue), ct);
        var reversalIds = reversalJournals.Select(x => x.SourceDocumentId!.Value).ToHashSet();
        return new((await ActiveProfiles(ct)).Count == 1, currency?.NameAr ?? "غير محددة", currency?.Code ?? "", reversed.Where(x=>reversalIds.Contains(x.Id)).Select(x => x.ReferenceId!.Value).Distinct().ToArray());
    }

    private async Task<Currency> EnsureSetupAsync(DateOnly date, CancellationToken ct)
    {
        var active = await ActiveProfiles(ct);
        if (active.Count != 1) throw Error("opening_disabled", "إدخال الأرصدة الافتتاحية مغلق أو لم يتم تهيئة ملف ترحيل رصيد افتتاحي فعال واحد.");
        var config = await settings.GetByIdAsync(AccountingSettings.SingletonId, ct)
            ?? throw Error("accounting_settings_required", "يجب إعداد المحاسبة أولاً.");
        var currency = await currencies.GetByIdAsync(config.BaseCurrencyId, ct)
            ?? throw Error("base_currency_missing", "العملة الأساسية غير محددة.");
        if (!currency.IsActive) throw Error("base_currency_inactive", "العملة الأساسية غير فعالة.");
        var roles = await profileLines.ListAsync(new Specification<PostingProfileLine>().Where(x => x.PostingProfileId == active[0].Id), ct);
        async Task<Account> Role(string role)
        {
            var matches = roles.Where(x => x.AccountRole == role).ToArray();
            if (matches.Length != 1) throw Error("opening_profile_role", $"يجب تحديد حساب واحد للدور {role} في ملف ترحيل الرصيد الافتتاحي.");
            var account = await accounts.GetByIdAsync(matches[0].AccountId, ct);
            if (account is null || !account.CanReceivePosting() || account.AccountType == AccountType.Subledger)
                throw Error("opening_account_invalid", "حساب الرصيد الافتتاحي غير قابل للترحيل.");
            return account;
        }
        var inventory = await Role("Inventory");
        var offset = await Role("OpeningOffset");
        if (inventory.Id == offset.Id || inventory.AccountClass != AccountClass.Asset || offset.AccountClass != AccountClass.Equity || config.InventoryAccountId != inventory.Id)
            throw Error("opening_accounts_invalid", "يجب استخدام حساب المخزون المحدد في إعدادات المحاسبة وحساب مقابل مستقل ضمن حقوق الملكية.");
        if (date == default || date > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            throw Error("opening_date_invalid", "تاريخ الرصيد الافتتاحي غير صالح أو في المستقبل.");
        await accounting.ValidatePostingPeriodAsync(date, ct);
        return currency;
    }

    private async Task ValidateLinesAsync(Guid warehouse, IReadOnlyList<CreateInventoryTransactionLineRequest> entries, Currency currency, CancellationToken ct)
    {
        if (warehouse == Guid.Empty || (await warehouses.GetByIdAsync(warehouse, ct)) is not { IsActive: true })
            throw Error("opening_warehouse_invalid", "اختر مخزنًا فعالًا.");
        if (entries is null || entries.Count == 0 || entries.Count > 5000 || entries.Select(x => x.ProductVariantId).Distinct().Count() != entries.Count)
            throw Error("opening_lines_invalid", "أدخل من 1 إلى 5000 صنف دون تكرار.");
        decimal total = 0;
        foreach (var line in entries.OrderBy(x => x.ProductVariantId))
        {
            if (line.ProductVariantId == Guid.Empty || line.Quantity <= 0 || line.Quantity >= 1000000000000000m || Math.Round(line.Quantity,3) != line.Quantity ||
                line.UnitCost <= 0 || line.UnitCost >= 10000000000000000m || Math.Round(line.UnitCost,2) != line.UnitCost || line.Notes?.Length > 500 ||
                line.Quantity > 9999999999999999.99m / line.UnitCost)
                throw Error("opening_line_invalid", "الكمية والتكلفة يجب أن تكونا موجبتين وضمن دقة المخزون (3 و2 منازل عشرية). ");
            var value = Math.Round(line.Quantity * line.UnitCost, 2);
            if (value <= 0 || Math.Round(value, currency.DecimalPlaces) != value || (total += value) > 999999999999999.99m)
                throw Error("opening_value_invalid", "قيمة البند أو المستند لا تتوافق مع دقة العملة الأساسية أو حدود المبالغ.");
            var variant = await variants.GetByIdAsync(line.ProductVariantId, ct);
            if (variant is null || !variant.IsActive || (await products.GetByIdAsync(variant.ProductId, ct)) is not { IsActive: true, IsStockItem: true })
                throw Error("opening_variant_invalid", "يجب اختيار صنف مخزني فعال تابع لمنتج فعال.");
            var balance = await balances.GetByWarehouseAndVariantAsync(warehouse, line.ProductVariantId, ct);
            if (balance is not null && (balance.OnHandQuantity != 0 || balance.ReservedQuantity != 0 || balance.InventoryValue != 0 || balance.LastMovementAtUtc.HasValue))
                throw Error("opening_previous_stock", $"الصنف {variant.SKU} له رصيد أو حركة سابقة في المخزن.");
            if (await ledger.CountAsync(new Specification<InventoryLedger>().Where(x => x.WarehouseId == warehouse && x.ProductVariantId == line.ProductVariantId), ct) > 0)
                throw Error("opening_previous_movement", $"الصنف {variant.SKU} له حركات سابقة في المخزن، ولو كان رصيده صفرًا.");
        }
    }

    private async Task<InventoryTransaction> DraftAsync(Guid id, CancellationToken ct)
    {
        var t = await transactions.GetForUpdateAsync(id, ct) ?? throw new NotFoundException(nameof(InventoryTransaction), id);
        if (t.TransactionType != InventoryTransactionType.Opening || t.ReferenceType != Reference || t.Status != InventoryTransactionStatus.Draft)
            throw Error("opening_not_draft", "المستند ليس مسودة رصيد افتتاحي قابلة للتعديل.");
        return t;
    }

    public async Task<Guid> Handle(SaveInventoryOpening command, CancellationToken ct)
    {
        var input = command.Request;
        if (input.Notes?.Length > 1000) throw Error("opening_notes_invalid", "الملاحظات لا تتجاوز 1000 حرف.");
        var currency = await EnsureSetupAsync(input.OpeningDate, ct);
        await ValidateLinesAsync(input.WarehouseId, input.Lines, currency, ct);
        var date = new DateTimeOffset(input.OpeningDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        InventoryTransaction t;
        if (command.Id is Guid id)
        {
            t = await DraftAsync(id, ct);
            t.UpdateOpeningDraft(date, input.WarehouseId, input.Notes);
            transactions.Update(t);
            lines.DeleteRange(await transactions.GetLinesAsync(id, ct));
        }
        else
        {
            var n = await sequence.NextAsync("InventoryTransaction", ct);
            t = new InventoryTransaction($"OPN-{input.OpeningDate.Year:0000}-{n:000000}", InventoryTransactionType.Opening, date,
                destinationWarehouseId: input.WarehouseId, referenceType: Reference, notes: input.Notes);
            await transactions.AddAsync(t, ct);
        }
        foreach (var item in input.Lines)
        {
            var line = new InventoryTransactionLine(t.Id, item.ProductVariantId, item.Quantity, item.UnitCost, item.Notes);
            line.RoundOpeningValue();
            await lines.AddAsync(line, ct);
        }
        return t.Id;
    }

    public async Task<Guid> Handle(DeleteInventoryOpening command, CancellationToken ct)
    {
        var t = await DraftAsync(command.Id, ct);
        lines.DeleteRange(await transactions.GetLinesAsync(t.Id, ct));
        transactions.Delete(t);
        return t.Id;
    }

    public async Task<Guid> Handle(PostInventoryOpening command, CancellationToken ct)
    {
        var t = await DraftAsync(command.Id, ct);
        var date = DateOnly.FromDateTime(t.TransactionDate.UtcDateTime);
        var currency = await EnsureSetupAsync(date, ct);
        var warehouse = t.DestinationWarehouseId ?? Guid.Empty;
        var entries = await transactions.GetLinesAsync(t.Id, ct);
        await ValidateLinesAsync(warehouse, entries.Select(x => new CreateInventoryTransactionLineRequest(x.ProductVariantId,x.Quantity,x.UnitCost,x.Notes)).ToArray(), currency, ct);
        if (!Guid.TryParse(currentUser.UserId, out var user)) throw Error("opening_user_invalid", "تعذر تحديد المستخدم المسؤول عن الترحيل.");
        if (await journals.CountAsync(new Specification<JournalEntry>().Where(x => x.SourceModule == Module && x.SourceDocumentType == Document && x.SourceDocumentId == t.Id), ct) > 0)
            throw Error("opening_journal_exists", "يوجد قيد مرتبط بالمستند بالفعل. راجع المحاسبة قبل إعادة المحاولة.");
        var now = clock.GetUtcNow().UtcDateTime;
        var journalLines = new List<ProfileAccountingPostingLine>();
        foreach (var line in entries.OrderBy(x => x.ProductVariantId))
        {
            var value = Math.Round(line.Quantity * line.UnitCost, 2);
            var (balance, _) = await stockPosting.PostMovementAsync(warehouse, line.ProductVariantId, InventoryMovementType.In,
                line.Quantity, line.UnitCost, t.Id, line.Id, t.TransactionDate, user.ToString(), ct);
            if (balance.OnHandQuantity != line.Quantity || balance.InventoryValue != value)
                throw Error("opening_stock_changed", "تغير رصيد الصنف أثناء الترحيل. أعد المراجعة.");
            journalLines.Add(new(Document, "Inventory", false, value, 0, false, "رصيد افتتاحي", ProductVariantId: line.ProductVariantId, WarehouseId: warehouse, SourceLineId: line.Id));
        }
        journalLines.Add(new(Document, "OpeningOffset", false, 0, journalLines.Sum(x => x.DebitBase), false, "مقابل الرصيد الافتتاحي"));
        var journalId = await accounting.PostAsync(new(Module, Document, t.Id, date, date, $"رصيد افتتاحي {t.TransactionNumber}", null, 1m, journalLines), user, now, ct);
        t.LinkOpeningJournal(journalId);
        t.Post(now, user.ToString());
        transactions.Update(t);
        return t.Id;
    }

    // Reversal is an opposite posted document, not deletion or rewriting historical movements.
    // Never reverse an opening that has subsequent stock movements or reservations.
    public async Task<Guid> Handle(ReverseInventoryOpening command, CancellationToken ct)
    {
        var t = await transactions.GetForUpdateAsync(command.Id, ct) ?? throw new NotFoundException(nameof(InventoryTransaction), command.Id);
        if (t.TransactionType != InventoryTransactionType.Opening || t.ReferenceType != Reference || t.Status != InventoryTransactionStatus.Posted)
            throw Error("opening_reverse_invalid", "يمكن عكس رصيد افتتاحي مرحل من هذه الواجهة فقط.");
        if (await transactions.CountAsync(new Specification<InventoryTransaction>().Where(x => x.ReferenceType == "InventoryOpeningReversal" && x.ReferenceId == t.Id), ct) > 0)
            throw Error("opening_already_reversed", "تم عكس الرصيد الافتتاحي مسبقًا.");
        var date = command.Request.PostingDate;
        var reason = command.Request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 400 || date < DateOnly.FromDateTime(t.TransactionDate.UtcDateTime) || date > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            throw Error("opening_reverse_fields", "حدد سبب العكس وتاريخًا صالحًا لا يسبق تاريخ الرصيد.");
        var matchingPeriods = await periods.ListAsync(new Specification<FiscalPeriod>().Where(x => x.StartDate <= date && x.EndDate >= date), ct);
        if (matchingPeriods.Count != 1 || !matchingPeriods[0].CanPostAccounting())
            throw Error("opening_reverse_period", "تاريخ العكس يجب أن يقع في فترة مالية مفتوحة.");
        var warehouse = t.DestinationWarehouseId ?? Guid.Empty;
        if ((await warehouses.GetByIdAsync(warehouse, ct)) is not {IsActive:true}) throw Error("opening_warehouse_invalid", "مخزن الرصيد غير فعال.");
        var entries = await transactions.GetLinesAsync(t.Id, ct);
        if (entries.Count == 0) throw Error("opening_lines_invalid", "لا توجد بنود للعكس.");
        foreach (var entry in entries.OrderBy(x => x.ProductVariantId))
        {
            var moves = await ledger.ListAsync(new Specification<InventoryLedger>().Where(x => x.WarehouseId == warehouse && x.ProductVariantId == entry.ProductVariantId), ct);
            var balance = await balances.GetByWarehouseAndVariantAsync(warehouse, entry.ProductVariantId, ct);
            if (moves.Count != 1 || moves[0].TransactionId != t.Id || moves[0].QuantityIn != entry.Quantity || balance is null || balance.OnHandQuantity != entry.Quantity || balance.ReservedQuantity != 0 || balance.AverageUnitCost != entry.UnitCost || balance.InventoryValue != Math.Round(entry.Quantity*entry.UnitCost,2))
                throw Error("opening_reverse_stock_changed", "لا يمكن عكس الرصيد بعد حدوث حركة أخرى أو حجز أو تغير في رصيد أحد الأصناف.");
        }
        var originals = await journals.ListAsync(new Specification<JournalEntry>().Where(x => x.SourceModule == Module && x.SourceDocumentType == Document && x.SourceDocumentId == t.Id), ct);
        if (originals.Count != 1 || t.ReferenceId != originals[0].Id) throw Error("opening_reverse_journal_missing", "تعذر التحقق من القيد المحاسبي الأصلي.");
        var original = await journals.GetForUpdateAsync(originals[0].Id, ct) ?? throw Error("opening_reverse_journal_missing", "القيد الأصلي غير موجود.");
        if (original.Status != JournalEntryStatus.Posted || original.ReversedJournalId.HasValue || !original.BaseCurrencyId.HasValue || !original.BaseCurrencyDecimalPlacesSnapshot.HasValue || string.IsNullOrWhiteSpace(original.BaseCurrencyCodeSnapshot))
            throw Error("opening_reverse_journal_invalid", "القيد الأصلي غير قابل للعكس.");
        var config = await settings.GetByIdAsync(AccountingSettings.SingletonId,ct);
        if (config?.BaseCurrencyId != original.BaseCurrencyId) throw Error("opening_reverse_currency", "تغيرت العملة الأساسية منذ الترحيل؛ لا يمكن العكس تلقائيًا.");
        var originalLines = await journalDetails.ListAsync(new Specification<JournalEntryLine>().Where(x => x.JournalEntryId == original.Id), ct);
        OAS.Domain.Accounting.Rules.JournalPostingRules.EnsureBalanced(originalLines);
        OAS.Domain.Accounting.Rules.JournalPostingRules.EnsureCurrencySnapshots(original,originalLines);
        var expected = entries.Sum(x => Math.Round(x.Quantity*x.UnitCost,2));
        if (originalLines.Sum(x=>x.DebitAmount) != expected || originalLines.Sum(x=>x.CreditAmount) != expected)
            throw Error("opening_reverse_journal_value", "قيمة القيد الأصلي لا تطابق قيمة الرصيد الافتتاحي.");
        foreach (var accountId in originalLines.Select(x=>x.AccountId).Distinct())
            if ((await accounts.GetByIdAsync(accountId,ct))?.CanReceivePosting() != true) throw Error("opening_reverse_account", "أحد الحسابات الأصلية لم يعد قابلاً للترحيل.");
        if (!Guid.TryParse(currentUser.UserId,out var user)) throw Error("opening_user_invalid", "تعذر تحديد المستخدم.");
        var now=clock.GetUtcNow().UtcDateTime;
        var n=await sequence.NextAsync("InventoryTransaction",ct);
        var reversal = new InventoryTransaction($"OPR-{date.Year:0000}-{n:000000}",InventoryTransactionType.AdjustmentDecrease,
            new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue),TimeSpan.Zero),sourceWarehouseId:warehouse,referenceType:"InventoryOpeningReversal",referenceId:t.Id,reason:reason);
        await transactions.AddAsync(reversal,ct);
        foreach(var entry in entries.OrderBy(x=>x.ProductVariantId))
        {
            var line=new InventoryTransactionLine(reversal.Id,entry.ProductVariantId,entry.Quantity,entry.UnitCost,entry.Notes);
            line.RoundOpeningValue();
            await lines.AddAsync(line,ct);
            var (balance,movement)=await stockPosting.PostMovementAsync(warehouse,line.ProductVariantId,InventoryMovementType.Out,line.Quantity,line.UnitCost,reversal.Id,line.Id,reversal.TransactionDate,user.ToString(),ct);
            if(balance.OnHandQuantity!=0 || balance.InventoryValue!=0 || movement.UnitCost!=line.UnitCost) throw Error("opening_reverse_stock_changed","تغيرت قيمة المخزون أثناء العكس.");
        }
        var journalSeq=await sequence.NextAsync($"JournalEntry-{date.Year}",ct);
        var journal=JournalEntry.Create(Guid.NewGuid(),$"JV-{date.Year:0000}-{journalSeq:000000}",JournalType.Reversal,date,date,matchingPeriods[0].Id,
            $"عكس الرصيد {t.TransactionNumber}: {reason}",Module,"InventoryOpeningReversal",reversal.Id,JournalEntryStatus.Draft);
        journal.SetBaseCurrencySnapshot(original.BaseCurrencyId.Value, original.BaseCurrencyCodeSnapshot!, original.BaseCurrencyDecimalPlacesSnapshot.Value);
        foreach(var line in originalLines.OrderBy(x=>x.LineNumber)) journal.AddLine(line.CreateOpposite(Guid.NewGuid(),journal.Id));
        journal.SetPendingApproval();journal.Approve(user,now);journal.Post(user,now);
        original.MarkReversed(journal.Id);journals.Update(original);await journals.AddAsync(journal,ct);
        reversal.Post(now,user.ToString()); // New entity is already tracked as Added.
        return reversal.Id;
    }
}
