using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.PurchaseInvoices.Commands;

public sealed record PayPurchaseInvoiceCommand(Guid Id, PayPurchaseInvoiceRequest Request)
    : ICommand<PayPurchaseInvoiceResultDto>, IAuthorizedRequest
{
    // Quick-pay is intentionally a privileged composite operation. It is available only
    // when the same principal may create, approve and post the voucher and create the
    // allocation. Organizations enforcing maker-checker should use the normal voucher
    // workflow and allocate only after a separate approver posts the voucher.
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        PurchasingPermissions.Invoices.View,
        AccountingPermissions.PaymentVouchers.Create,
        AccountingPermissions.PaymentVouchers.Approve,
        AccountingPermissions.PaymentVouchers.Post,
        AccountingPermissions.PaymentAllocations.Create
    ];
}

public sealed class PayPurchaseInvoiceCommandHandler(
    IReadRepository<PurchaseInvoice, Guid> invoices,
    IRepository<PaymentVoucher, Guid> paymentVouchers,
    IReadRepository<PaymentVoucherLine, Guid> paymentVoucherLines,
    IRepository<PaymentAllocation, Guid> paymentAllocations,
    IReadRepository<JournalEntry, Guid> journals,
    IReadRepository<AccountingSettings, Guid> settingsRepository,
    IReadRepository<Currency, Guid> currencies,
    IVoucherSettlementResolver settlementResolver,
    IAccountingDocumentPostingService postingService,
    ISupplierSettlementFxPostingService supplierSettlementFxPosting,
    IPurchasePaymentAllocationConcurrencyPort allocationConcurrency,
    IEnumerable<IPaymentAllocationTargetValidator> targetValidators,
    IEnumerable<IPaymentAllocationSourceTargetValidator> sourceTargetValidators,
    IPurchaseInvoiceBalanceService invoiceBalances,
    ISequenceNumberGenerator sequences,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<PayPurchaseInvoiceCommand, PayPurchaseInvoiceResultDto>
{
    private const string FxSourceModule = "Accounting";
    private const string FxSourceDocumentType = "SupplierSettlementFx";

    public async Task<PayPurchaseInvoiceResultDto> Handle(PayPurchaseInvoiceCommand command, CancellationToken ct)
    {
        // Serialize every quick-pay decision on the invoice before reading status or
        // outstanding. This prevents two parallel HTTP requests from both settling the
        // same final amount.
        await allocationConcurrency.LockInvoiceAsync(command.Id, ct);

        var invoice = await invoices.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), command.Id);

        if (invoice.Status != PurchaseInvoiceStatus.Posted)
            throw new ConflictException("purchase_invoice_payment_invoice_not_posted", "لا يمكن سداد فاتورة المورد قبل ترحيلها.");

        var request = command.Request;
        if (request.Amount <= 0m)
            throw new ConflictException("purchase_invoice_payment_amount_invalid", "يجب أن يكون مبلغ السداد أكبر من صفر.");

        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);
        if (idempotencyKey is not null)
        {
            var replay = await FindAllocationByIdempotencyKeyAsync(idempotencyKey, ct);
            if (replay is not null)
                return await BuildReplayResultAsync(replay, invoice, request.Amount, ct);
        }

        var method = (PaymentMethod)(byte)request.PaymentMethod;
        if (method is not (PaymentMethod.Cash or PaymentMethod.BankTransfer))
            throw new ConflictException("purchase_invoice_payment_method_invalid", "السداد السريع يدعم النقد أو التحويل البنكي فقط.");

        ValidateSettlementSelection(method, request.CashAccountId, request.BankAccountId);

        var settings = await settingsRepository.GetByIdAsync(AccountingSettings.SingletonId, ct)
            ?? throw new ConflictException("accounting_settings_required", "يجب إعداد المحاسبة وتحديد العملة الأساسية أولاً.");
        var baseCurrency = await currencies.GetByIdAsync(settings.BaseCurrencyId, ct)
            ?? throw new ConflictException("base_currency_missing", "العملة الأساسية المحددة في إعدادات المحاسبة غير موجودة.");

        var resolved = await settlementResolver.ResolveAsync(
            request.PaymentDate,
            SettlementPartyType.Supplier,
            null,
            invoice.SupplierId,
            null,
            null,
            null,
            method,
            request.CashAccountId,
            request.BankAccountId,
            null,
            invoice.CurrencyId,
            request.Amount,
            null,
            ExchangeRateType.Accounting,
            request.ReferenceNumber,
            ct);

        var targetValidator = targetValidators.SingleOrDefault(x => x.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice)
            ?? throw new ConflictException("purchase_invoice_payment_validator_missing", "إعدادات التحقق من سداد فواتير الموردين غير متاحة.");
        var sourceTargetValidator = sourceTargetValidators.SingleOrDefault(x => x.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice);

        var validation = await targetValidator.ValidateAsync(
            invoice.Id,
            resolved.CurrencyId,
            resolved.Amount,
            resolved.BaseAmount,
            null,
            ct);

        var balanceBeforePayment = await invoiceBalances.GetAsync(invoice, null, ct);

        if (sourceTargetValidator is not null)
        {
            await sourceTargetValidator.ValidateSourceAsync(
                invoice.Id,
                new PaymentAllocationSourceContext(
                    PaymentSourceType.PaymentVoucher,
                    SettlementPartyType.Supplier,
                    null,
                    invoice.SupplierId,
                    null),
                ct);
        }

        var voucherNumber = await ReserveVoucherNumberAsync(request.PaymentDate.Year, ct);
        var voucherId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"سداد فاتورة المورد {invoice.PurchaseInvoiceCode}"
            : request.Description.Trim();
        var realizedExchangeDifferenceBase = Math.Round(
            resolved.BaseAmount - validation.TargetBaseAllocatedAmount,
            4,
            MidpointRounding.AwayFromZero);

        var voucher = PaymentVoucher.CreateSettlementDocument(
            voucherId,
            voucherNumber,
            request.PaymentDate,
            baseCurrency.Id,
            baseCurrency.Code,
            baseCurrency.DecimalPlaces,
            resolved.BaseAmount,
            description);

        var line = PaymentVoucherLine.CreateSettlement(
            lineId,
            voucherId,
            1,
            SettlementPartyType.Supplier,
            null,
            invoice.SupplierId,
            null,
            resolved.PartyNameSnapshot,
            resolved.CounterpartyAccountId,
            method,
            resolved.CashAccountId,
            resolved.BankAccountId,
            resolved.SettlementAccountId,
            resolved.CurrencyId,
            resolved.CurrencyCode,
            resolved.CurrencySymbol,
            resolved.CurrencyDecimalPlaces,
            resolved.Amount,
            resolved.ExchangeRate,
            resolved.ExchangeRateDate,
            resolved.ExchangeRateType,
            resolved.ExchangeRateSource,
            resolved.BaseAmount,
            request.ReferenceNumber,
            request.PaymentDate,
            "PurchaseInvoice",
            invoice.Id,
            description,
            validation.TargetBaseAllocatedAmount,
            realizedExchangeDifferenceBase);

        voucher.AddLine(line);
        await paymentVouchers.AddAsync(voucher, ct);

        if (!Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        voucher.Approve(voucher.Lines);
        var journalId = await postingService.PostPaymentVoucherAsync(voucher, voucher.Lines, userId, now, ct);
        voucher.SetJournalEntry(journalId);
        voucher.Post(userId, now, voucher.Lines);

        var allocation = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(),
            null,
            line.Id,
            AllocationTargetDocumentType.PurchaseInvoice,
            invoice.Id,
            resolved.CurrencyId,
            resolved.CurrencyCode,
            resolved.Amount,
            resolved.ExchangeRate,
            resolved.BaseAmount,
            now,
            validation.TargetBaseAllocatedAmount,
            targetDocumentAmount: validation.TargetDocumentAmount,
            idempotencyKey: idempotencyKey);
        await paymentAllocations.AddAsync(allocation, ct);

        var fxPosting = await supplierSettlementFxPosting.PostAsync(
            new SupplierSettlementFxPostingRequest(
                allocation.Id,
                invoice.SupplierId,
                resolved.CounterpartyAccountId,
                request.PaymentDate,
                resolved.BaseAmount,
                validation.TargetBaseAllocatedAmount,
                $"فرق صرف محقق لسداد فاتورة المورد {invoice.PurchaseInvoiceCode}"),
            userId,
            now,
            ct);

        var settledDocumentAmount = validation.TargetDocumentAmount ?? resolved.Amount;
        var outstanding = Math.Max(0m, Math.Round(
            balanceBeforePayment.OutstandingAmount - settledDocumentAmount,
            4,
            MidpointRounding.AwayFromZero));
        var paid = Math.Max(0m, Math.Round(
            balanceBeforePayment.PaidAmount + settledDocumentAmount,
            4,
            MidpointRounding.AwayFromZero));

        return new PayPurchaseInvoiceResultDto(
            voucher.Id,
            voucher.VoucherNumber,
            line.Id,
            allocation.Id,
            journalId,
            paid,
            outstanding,
            outstanding <= 0m ? "Paid" : "PartiallyPaid",
            fxPosting.RealizedExchangeDifferenceBase,
            fxPosting.JournalEntryId);
    }

    private async Task<PaymentAllocation?> FindAllocationByIdempotencyKeyAsync(string key, CancellationToken ct)
    {
        var matches = await paymentAllocations.ListAsync(
            new Specification<PaymentAllocation>()
                .Where(x => x.IdempotencyKey == key)
                .ApplyPaging(0, 2),
            ct);
        return matches.FirstOrDefault();
    }

    private async Task<PayPurchaseInvoiceResultDto> BuildReplayResultAsync(
        PaymentAllocation allocation,
        PurchaseInvoice invoice,
        decimal requestedAmount,
        CancellationToken ct)
    {
        if (allocation.TargetDocumentType != AllocationTargetDocumentType.PurchaseInvoice ||
            allocation.TargetDocumentId != invoice.Id ||
            allocation.AllocatedAmount != requestedAmount)
        {
            throw new ConflictException(
                "payment_allocation_idempotency_conflict",
                "مفتاح منع التكرار مستخدم لعملية سداد مختلفة.");
        }

        if (allocation.IsReversed)
            throw new ConflictException(
                "payment_allocation_idempotency_reversed",
                "عملية السداد بهذا المفتاح سبق عكس تخصيصها ولا يمكن إنشاؤها مرة أخرى بالمفتاح نفسه.");

        if (allocation.PaymentVoucherLineId is not Guid paymentLineId)
            throw new ConflictException("payment_allocation_replay_source_missing", "تعذر استعادة سند الصرف للعملية السابقة.");

        var line = await paymentVoucherLines.GetByIdAsync(paymentLineId, ct)
            ?? throw new NotFoundException(nameof(PaymentVoucherLine), paymentLineId);
        var voucher = await paymentVouchers.GetByIdAsync(line.PaymentVoucherId, ct)
            ?? throw new NotFoundException(nameof(PaymentVoucher), line.PaymentVoucherId);
        if (!voucher.JournalEntryId.HasValue)
            throw new ConflictException("payment_allocation_replay_journal_missing", "سند الصرف السابق غير مرتبط بقيد محاسبي صالح.");

        var fxJournals = await journals.ListAsync(
            new Specification<JournalEntry>()
                .Where(x =>
                    x.SourceModule == FxSourceModule &&
                    x.SourceDocumentType == FxSourceDocumentType &&
                    x.SourceDocumentId == allocation.Id)
                .ApplyPaging(0, 1),
            ct);
        var balance = await invoiceBalances.GetAsync(invoice, null, ct);
        var sourceBase = allocation.BaseAllocatedAmount ?? allocation.AllocatedAmount;
        var targetBase = allocation.TargetBaseAllocatedAmount ?? sourceBase;
        var fxDifference = Math.Round(sourceBase - targetBase, 4, MidpointRounding.AwayFromZero);

        return new PayPurchaseInvoiceResultDto(
            voucher.Id,
            voucher.VoucherNumber,
            line.Id,
            allocation.Id,
            voucher.JournalEntryId.Value,
            balance.PaidAmount,
            balance.OutstandingAmount,
            balance.OutstandingAmount <= 0m ? "Paid" : balance.PaidAmount > 0m ? "PartiallyPaid" : "Unpaid",
            fxDifference,
            fxJournals.FirstOrDefault()?.Id);
    }

    private static string? NormalizeIdempotencyKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > 80)
            throw new ConflictException("payment_allocation_idempotency_key_invalid", "مفتاح منع التكرار يجب ألا يتجاوز 80 حرفًا.");
        return normalized;
    }

    private static void ValidateSettlementSelection(PaymentMethod method, Guid? cashAccountId, Guid? bankAccountId)
    {
        if (method == PaymentMethod.Cash)
        {
            if (!cashAccountId.HasValue || cashAccountId == Guid.Empty)
                throw new ConflictException("purchase_invoice_cash_account_required", "اختر الصندوق الذي سيتم السداد منه.");
            if (bankAccountId.HasValue)
                throw new ConflictException("purchase_invoice_cash_bank_not_allowed", "لا يمكن تحديد حساب بنكي مع السداد النقدي.");
            return;
        }

        if (!bankAccountId.HasValue || bankAccountId == Guid.Empty)
            throw new ConflictException("purchase_invoice_bank_account_required", "اختر الحساب البنكي الذي سيتم السداد منه.");
        if (cashAccountId.HasValue)
            throw new ConflictException("purchase_invoice_bank_cash_not_allowed", "لا يمكن تحديد صندوق مع التحويل البنكي.");
    }

    private async Task<string> ReserveVoucherNumberAsync(int year, CancellationToken ct)
    {
        for (var i = 0; i < 100; i++)
        {
            var sequence = await sequences.NextAsync($"PaymentVoucher-{year}", ct);
            var number = $"PV-{year:0000}-{sequence:000000}";
            var exists = await paymentVouchers.CountAsync(
                new Specification<PaymentVoucher>().Where(x => x.VoucherNumber == number), ct);
            if (exists == 0) return number;
        }
        throw new ConflictException("payment_voucher_number_duplicate", "تعذر حجز رقم فريد لسند الصرف التلقائي.");
    }
}
