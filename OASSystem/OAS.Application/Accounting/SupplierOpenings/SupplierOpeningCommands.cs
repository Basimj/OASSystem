using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.SupplierOpenings;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.SupplierOpenings;

public sealed record CreateSupplierOpeningBatchCommand(CreateSupplierOpeningBatchRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.Journals.Create, AccountingPermissions.Suppliers.View];
}

public sealed record PostSupplierOpeningBatchCommand(Guid BatchId, PostSupplierOpeningBatchRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.Journals.Post];
}

public sealed record LinkSupplierOpeningJournalCommand(Guid BatchId, LinkSupplierOpeningJournalRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.Journals.Post];
}

public sealed record ApplySupplierOpeningCommand(Guid OpeningItemId, ApplySupplierOpeningRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.PaymentAllocations.Create];
}

public sealed record ReverseSupplierOpeningApplicationCommand(Guid ApplicationId, ReverseSupplierOpeningApplicationRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.PaymentAllocations.Edit];
}

public sealed class CreateSupplierOpeningBatchCommandHandler(
    ISupplierOpeningRepository openings,
    IReadRepository<Supplier, Guid> suppliers,
    IReadRepository<Currency, Guid> currencies,
    ISequenceNumberGenerator sequences)
    : IRequestHandler<CreateSupplierOpeningBatchCommand, Guid>
{
    public async Task<Guid> Handle(CreateSupplierOpeningBatchCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (request.Items.Count == 0)
            throw new ConflictException("supplier_opening_items_required", "يجب إدخال رصيد افتتاحي واحد على الأقل.");

        var baseCurrency = await currencies.GetByIdAsync(request.BaseCurrencyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), request.BaseCurrencyId);
        if (!baseCurrency.IsActive)
            throw new ConflictException("supplier_opening_base_currency_inactive", "العملة الأساسية للدفعة الافتتاحية غير فعالة.");

        var sequence = await sequences.NextAsync($"SupplierOpeningBatch-{request.AsOfDate.Year}", cancellationToken);
        var batch = SupplierOpeningBatch.Create(
            Guid.NewGuid(),
            $"AP-OPEN-{request.AsOfDate.Year:0000}-{sequence:000000}",
            request.AsOfDate,
            request.BaseCurrencyId,
            request.Notes);

        foreach (var source in request.Items)
        {
            var supplier = await suppliers.GetByIdAsync(source.SupplierId, cancellationToken)
                ?? throw new NotFoundException(nameof(Supplier), source.SupplierId);
            if (!supplier.IsActive)
                throw new ConflictException("supplier_opening_supplier_inactive", $"المورد '{supplier.SupplierCode}' غير فعال.");
            var currency = await currencies.GetByIdAsync(source.CurrencyId, cancellationToken)
                ?? throw new NotFoundException(nameof(Currency), source.CurrencyId);
            if (!currency.IsActive)
                throw new ConflictException("supplier_opening_currency_inactive", $"العملة '{currency.Code}' غير فعالة.");
            if (source.OriginalOpenAmount <= 0m || source.ExchangeRateSnapshot <= 0m)
                throw new ConflictException("supplier_opening_amount_invalid", "مبلغ الرصيد الافتتاحي وسعر الصرف يجب أن يكونا أكبر من صفر.");
            if (source.CurrencyId == request.BaseCurrencyId && Math.Round(source.ExchangeRateSnapshot, 8) != 1m)
                throw new ConflictException("supplier_opening_base_rate_invalid", "سعر العملة الأساسية يجب أن يساوي 1.");

            var baseAmount = Math.Round(source.OriginalOpenAmount * source.ExchangeRateSnapshot, 4, MidpointRounding.AwayFromZero);
            batch.AddItem(SupplierOpeningItem.Create(
                Guid.NewGuid(), batch.Id, supplier.Id, source.LegacyReference,
                source.DocumentDate, source.DueDate,
                source.DueDate.HasValue ? SupplierOpeningDueDateProvenance.Explicit : SupplierOpeningDueDateProvenance.Unknown,
                currency.Id, currency.Code, currency.DecimalPlaces,
                source.OriginalOpenAmount, source.ExchangeRateSnapshot, baseAmount));
        }

        await openings.AddBatchAsync(batch, cancellationToken);
        return batch.Id;
    }
}

public sealed class PostSupplierOpeningBatchCommandHandler(
    ISupplierOpeningRepository openings,
    IProfileAccountingPostingService posting,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<PostSupplierOpeningBatchCommand, Guid>
{
    private const string Module = "Accounting";
    private const string DocumentType = "SupplierOpeningBatch";
    private const string BalancingRole = "OpeningBalancing";

    public async Task<Guid> Handle(PostSupplierOpeningBatchCommand command, CancellationToken cancellationToken)
    {
        var batch = await openings.GetBatchAsync(command.BatchId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(SupplierOpeningBatch), command.BatchId);
        if (batch.Status is SupplierOpeningBatchStatus.Posted or SupplierOpeningBatchStatus.LinkedExistingJournal)
            return batch.JournalEntryId ?? throw new ConflictException("supplier_opening_journal_missing", "الدفعة الافتتاحية لا تحتوي مرجع قيد.");
        SupplierOpeningRowVersion.EnsureMatches(batch.RowVersion, command.Request.RowVersion, "Supplier opening batch");
        if (batch.Items.Count == 0)
            throw new ConflictException("supplier_opening_items_required", "يجب إدخال رصيد افتتاحي واحد على الأقل.");

        await posting.ValidatePostingPeriodAsync(batch.AsOfDate, cancellationToken);
        foreach (var supplierId in batch.Items.Select(x => x.SupplierId).Distinct())
            await posting.ValidateSupplierSubledgerAsync(supplierId, cancellationToken);

        var total = Math.Round(batch.Items.Sum(x => x.BaseOpenAmount), 4, MidpointRounding.AwayFromZero);
        var lines = batch.Items
            .OrderBy(x => x.SupplierId)
            .ThenBy(x => x.Id)
            .Select(x => new ProfileAccountingPostingLine(
                DocumentType, null, true, 0m, x.BaseOpenAmount, false,
                $"Supplier opening payable {x.LegacyReference ?? x.Id.ToString("N")}",
                SupplierId: x.SupplierId,
                SourceLineId: x.Id))
            .ToList();
        lines.Add(new ProfileAccountingPostingLine(
            DocumentType, BalancingRole, false, total, 0m, false,
            $"Supplier opening balancing {batch.BatchCode}"));

        var userId = ResolveUserGuid(currentUser.UserId);
        var at = timeProvider.GetUtcNow().UtcDateTime;
        var journalId = await posting.PostAsync(new ProfileAccountingPostingRequest(
            Module, DocumentType, batch.Id, batch.AsOfDate, batch.AsOfDate,
            $"Supplier opening payables {batch.BatchCode}", null, 1m, lines), userId, at, cancellationToken);
        batch.MarkPosted(journalId, currentUser.UserId ?? userId.ToString(), at);
        openings.UpdateBatch(batch);
        return journalId;
    }

    internal static Guid ResolveUserGuid(string? userId)
    {
        if (Guid.TryParse(userId, out var parsed) && parsed != Guid.Empty) return parsed;
        throw new ConflictException("supplier_opening_user_invalid", "تعذر تحديد المستخدم المسؤول عن ترحيل الرصيد الافتتاحي.");
    }
}

public sealed class LinkSupplierOpeningJournalCommandHandler(
    ISupplierOpeningRepository openings,
    IReadRepository<Supplier, Guid> suppliers,
    ISupplierOpeningJournalVerificationPort verification,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<LinkSupplierOpeningJournalCommand, Guid>
{
    public async Task<Guid> Handle(LinkSupplierOpeningJournalCommand command, CancellationToken cancellationToken)
    {
        var batch = await openings.GetBatchAsync(command.BatchId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(SupplierOpeningBatch), command.BatchId);
        if (batch.Status is SupplierOpeningBatchStatus.Posted or SupplierOpeningBatchStatus.LinkedExistingJournal)
            return batch.JournalEntryId ?? command.Request.JournalEntryId;
        SupplierOpeningRowVersion.EnsureMatches(batch.RowVersion, command.Request.RowVersion, "Supplier opening batch");

        var expectations = new List<SupplierOpeningJournalExpectation>();
        foreach (var group in batch.Items.GroupBy(x => x.SupplierId))
        {
            var supplier = await suppliers.GetByIdAsync(group.Key, cancellationToken)
                ?? throw new NotFoundException(nameof(Supplier), group.Key);
            expectations.Add(new SupplierOpeningJournalExpectation(
                supplier.Id, supplier.AccountId,
                Math.Round(group.Sum(x => x.BaseOpenAmount), 4, MidpointRounding.AwayFromZero)));
        }
        await verification.VerifyExistingJournalAsync(command.Request.JournalEntryId, batch.AsOfDate, expectations, cancellationToken);
        var at = timeProvider.GetUtcNow().UtcDateTime;
        batch.LinkExistingJournal(command.Request.JournalEntryId, currentUser.UserId ?? "system", at);
        openings.UpdateBatch(batch);
        return command.Request.JournalEntryId;
    }
}

public sealed class ApplySupplierOpeningCommandHandler(
    ISupplierOpeningRepository openings,
    ISupplierOpeningConcurrencyPort concurrency,
    IReadRepository<PaymentVoucherLine, Guid> paymentLines,
    IReadRepository<PaymentVoucher, Guid> paymentVouchers,
    IReadRepository<PaymentAllocation, Guid> paymentAllocations,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<ApplySupplierOpeningCommand, Guid>
{
    public async Task<Guid> Handle(ApplySupplierOpeningCommand command, CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(command.Request.IdempotencyKey) ? null : command.Request.IdempotencyKey.Trim();
        if (key is not null)
        {
            var existing = await openings.GetApplicationByIdempotencyKeyAsync(key, cancellationToken);
            if (existing is not null)
            {
                if (existing.Status == SupplierOpeningApplicationStatus.Active) return existing.Id;
                throw new ConflictException("supplier_opening_idempotency_reversed", "مفتاح إعادة المحاولة يعود إلى تخصيص افتتاحي تم عكسه.");
            }
        }
        if (command.Request.AmountBase <= 0m)
            throw new ConflictException("supplier_opening_apply_amount_invalid", "قيمة التخصيص يجب أن تكون أكبر من صفر.");

        await concurrency.LockPaymentLineAndOpeningItemAsync(command.Request.PaymentVoucherLineId, command.OpeningItemId, cancellationToken);
        var item = await openings.GetItemAsync(command.OpeningItemId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(SupplierOpeningItem), command.OpeningItemId);
        var line = await paymentLines.GetByIdAsync(command.Request.PaymentVoucherLineId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentVoucherLine), command.Request.PaymentVoucherLineId);
        if (line.SupplierId != item.SupplierId)
            throw new ConflictException("supplier_opening_supplier_mismatch", "لا يمكن تخصيص دفعة مورد على رصيد افتتاحي لمورد آخر.");
        var voucher = await paymentVouchers.GetByIdAsync(line.PaymentVoucherId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentVoucher), line.PaymentVoucherId);
        if (voucher.Status != PaymentVoucherStatus.Posted)
            throw new ConflictException("supplier_opening_payment_not_posted", "يجب ترحيل سند الصرف قبل تخصيصه على الرصيد الافتتاحي.");

        var itemApps = await openings.GetApplicationsForItemAsync(item.Id, cancellationToken);
        var itemApplied = itemApps.Where(x => x.Status == SupplierOpeningApplicationStatus.Active).Sum(x => x.AppliedTargetBaseAmount);
        var targetAvailable = Math.Max(0m, item.BaseOpenAmount - itemApplied);

        var sourceApps = await openings.GetApplicationsForPaymentLineAsync(line.Id, cancellationToken);
        var sourceOpeningApplied = sourceApps.Where(x => x.Status == SupplierOpeningApplicationStatus.Active).Sum(x => x.AppliedSourceBaseAmount);
        var otherAllocations = await paymentAllocations.ListAsync(new Specification<PaymentAllocation>()
            .Where(x => x.PaymentVoucherLineId == line.Id && !x.IsReversed), cancellationToken);
        var otherApplied = otherAllocations.Sum(x => x.BaseAllocatedAmount ?? x.Amount * x.ExchangeRateSnapshot);
        var sourceBase = line.CounterpartyBaseAmount ?? line.BaseAmount ?? Math.Round(line.Amount * (line.ExchangeRate ?? 1m), 4);
        var sourceAvailable = Math.Max(0m, sourceBase - sourceOpeningApplied - otherApplied);
        var amount = Math.Round(command.Request.AmountBase, 4, MidpointRounding.AwayFromZero);
        if (amount > targetAvailable || amount > sourceAvailable)
            throw new ConflictException("supplier_opening_overallocation", "قيمة التخصيص تتجاوز الرصيد الافتتاحي أو المبلغ غير المخصص من سند الصرف.");

        var applied = SupplierOpeningApplication.Create(
            Guid.NewGuid(), item.Id, line.Id, item.SupplierId,
            amount, amount, timeProvider.GetUtcNow().UtcDateTime,
            currentUser.UserId ?? "system", key);
        await openings.AddApplicationAsync(applied, cancellationToken);
        return applied.Id;
    }
}

public sealed class ReverseSupplierOpeningApplicationCommandHandler(
    ISupplierOpeningRepository openings,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<ReverseSupplierOpeningApplicationCommand>
{
    public async Task Handle(ReverseSupplierOpeningApplicationCommand command, CancellationToken cancellationToken)
    {
        var application = await openings.GetApplicationAsync(command.ApplicationId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(SupplierOpeningApplication), command.ApplicationId);
        if (application.Status == SupplierOpeningApplicationStatus.Reversed) return;
        SupplierOpeningRowVersion.EnsureMatches(application.RowVersion, command.Request.RowVersion, "Supplier opening application");
        application.Reverse(timeProvider.GetUtcNow().UtcDateTime, currentUser.UserId ?? "system", command.Request.Reason);
        openings.UpdateApplication(application);
    }
}

internal static class SupplierOpeningRowVersion
{
    public static void EnsureMatches(byte[] current, string supplied, string resourceName)
    {
        byte[] incoming;
        try { incoming = Convert.FromBase64String(supplied); }
        catch (FormatException ex) { throw new ConcurrencyException($"{resourceName} row version is invalid.", ex); }
        if (incoming.Length == 0 || !incoming.SequenceEqual(current))
            throw new ConcurrencyException($"{resourceName} was modified by another user. Reload and try again.");
    }

    public static string Encode(byte[] value) => value is { Length: > 0 } ? Convert.ToBase64String(value) : string.Empty;
}
