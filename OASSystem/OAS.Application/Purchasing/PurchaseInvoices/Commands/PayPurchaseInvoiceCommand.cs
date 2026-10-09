using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
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
    IRepository<PaymentAllocation, Guid> paymentAllocations,
    IReadRepository<AccountingSettings, Guid> settingsRepository,
    IReadRepository<Currency, Guid> currencies,
    IVoucherSettlementResolver settlementResolver,
    IAccountingDocumentPostingService postingService,
    IEnumerable<IPaymentAllocationTargetValidator> targetValidators,
    IEnumerable<IPaymentAllocationSourceTargetValidator> sourceTargetValidators,
    ISequenceNumberGenerator sequences,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<PayPurchaseInvoiceCommand, PayPurchaseInvoiceResultDto>
{
    public async Task<PayPurchaseInvoiceResultDto> Handle(PayPurchaseInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), command.Id);

        if (invoice.Status != PurchaseInvoiceStatus.Posted)
            throw new ConflictException("purchase_invoice_payment_invoice_not_posted", "لا يمكن سداد فاتورة المورد قبل ترحيلها.");

        var request = command.Request;
        if (request.Amount <= 0m)
            throw new ConflictException("purchase_invoice_payment_amount_invalid", "يجب أن يكون مبلغ السداد أكبر من صفر.");

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
            description);

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
            validation.TargetBaseAllocatedAmount);
        await paymentAllocations.AddAsync(allocation, ct);

        var existing = await paymentAllocations.ListAsync(
            new Specification<PaymentAllocation>().Where(x =>
                x.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice &&
                x.TargetDocumentId == invoice.Id),
            ct);
        var allocatedTargetBase = existing.Sum(x =>
            x.TargetBaseAllocatedAmount ??
            x.BaseAllocatedAmount ??
            (x.ExchangeRate.HasValue ? Math.Round(x.AllocatedAmount * x.ExchangeRate.Value, 4, MidpointRounding.AwayFromZero) : x.AllocatedAmount));
        allocatedTargetBase += validation.TargetBaseAllocatedAmount;
        var baseOutstanding = Math.Max(0m, invoice.BaseTotalAmount - allocatedTargetBase);
        var outstanding = invoice.ExchangeRate > 0m
            ? Math.Min(invoice.TotalAmount, Math.Round(baseOutstanding / invoice.ExchangeRate, 4, MidpointRounding.AwayFromZero))
            : Math.Max(0m, invoice.TotalAmount - resolved.Amount);
        var paid = Math.Max(0m, invoice.TotalAmount - outstanding);

        return new PayPurchaseInvoiceResultDto(
            voucher.Id,
            voucher.VoucherNumber,
            line.Id,
            allocation.Id,
            journalId,
            paid,
            outstanding,
            outstanding <= 0m ? "Paid" : "PartiallyPaid");
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
