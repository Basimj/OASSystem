using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Common;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.PurchaseReturns.Services;

public sealed class SupplierCreditSettlementService(
    IPurchaseReturnRepository purchaseReturns,
    IReadRepository<PurchaseInvoice, Guid> purchaseInvoices,
    IRepository<SupplierCreditApplication, Guid> creditApplications,
    IPurchaseInvoiceBalanceService invoiceBalances,
    ISupplierCreditConcurrencyPort concurrencyPort,
    ISupplierCreditSourcePort creditSourcePort)
    : ISupplierCreditSettlementService
{
    public async Task<SupplierCreditApplication?> AutoApplyLinkedReturnAsync(
        PurchaseReturn purchaseReturn,
        DateTime appliedAtUtc,
        string appliedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchaseReturn);

        if (purchaseReturn.Status != PurchaseReturnStatus.Posted ||
            !purchaseReturn.PurchaseInvoiceId.HasValue ||
            !purchaseReturn.JournalEntryId.HasValue)
            return null;

        var sourceCreditBase = await GetPostedSourceCreditBaseAmountAsync(purchaseReturn, cancellationToken);
        if (sourceCreditBase <= 0m) return null;

        var targetInvoiceId = purchaseReturn.PurchaseInvoiceId.Value;
        var key = AutoIdempotencyKey(purchaseReturn.Id, targetInvoiceId);
        return await ApplyCoreAsync(
            purchaseReturn,
            sourceCreditBase,
            targetInvoiceId,
            null,
            key,
            appliedAtUtc,
            appliedBy,
            allowNoOutstanding: true,
            cancellationToken);
    }

    public async Task<SupplierCreditApplication> ApplyAsync(
        Guid purchaseReturnId,
        Guid purchaseInvoiceId,
        decimal? appliedBaseAmount,
        string idempotencyKey,
        DateTime appliedAtUtc,
        string appliedBy,
        CancellationToken cancellationToken = default)
    {
        if (purchaseReturnId == Guid.Empty)
            throw new ConflictException("supplier_credit_return_required", "مرتجع المشتريات مطلوب لتطبيق ائتمان المورد.");
        if (purchaseInvoiceId == Guid.Empty)
            throw new ConflictException("supplier_credit_invoice_required", "فاتورة المورد الهدف مطلوبة لتطبيق الائتمان.");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 80)
            throw new ConflictException("supplier_credit_idempotency_required", "مفتاح منع تكرار تطبيق ائتمان المورد مطلوب ويجب ألا يتجاوز 80 حرفًا.");
        if (appliedBaseAmount.HasValue && appliedBaseAmount.Value <= 0m)
            throw new ConflictException("supplier_credit_amount_invalid", "قيمة تطبيق ائتمان المورد يجب أن تكون أكبر من صفر.");

        await concurrencyPort.LockAsync(purchaseReturnId, purchaseInvoiceId, cancellationToken);

        var purchaseReturn = await purchaseReturns.GetForUpdateAsync(purchaseReturnId, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseReturn), purchaseReturnId);
        var sourceCreditBase = await GetPostedSourceCreditBaseAmountAsync(purchaseReturn, cancellationToken);

        return await ApplyCoreAsync(
            purchaseReturn,
            sourceCreditBase,
            purchaseInvoiceId,
            appliedBaseAmount,
            idempotencyKey.Trim(),
            appliedAtUtc,
            appliedBy,
            allowNoOutstanding: false,
            cancellationToken,
            lockAlreadyAcquired: true);
    }

    public async Task<SupplierCreditApplication> ReverseAsync(
        Guid purchaseReturnId,
        Guid applicationId,
        string rowVersion,
        string reason,
        DateTime reversedAtUtc,
        string reversedBy,
        CancellationToken cancellationToken = default)
    {
        var application = await creditApplications.GetByIdAsync(applicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(SupplierCreditApplication), applicationId);
        if (application.PurchaseReturnId != purchaseReturnId)
            throw new NotFoundException(nameof(SupplierCreditApplication), applicationId);

        await concurrencyPort.LockAsync(purchaseReturnId, application.PurchaseInvoiceId, cancellationToken);

        application = await creditApplications.GetForUpdateAsync(applicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(SupplierCreditApplication), applicationId);
        PurchasingRowVersion.EnsureMatches(application.RowVersion, rowVersion, "Supplier credit application");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ConflictException("supplier_credit_reversal_reason_required", "سبب عكس تطبيق ائتمان المورد مطلوب.");

        application.Reverse(reversedAtUtc, reversedBy, reason);
        creditApplications.Update(application);
        return application;
    }

    public async Task<SupplierCreditSnapshot> GetSnapshotAsync(
        Guid purchaseReturnId,
        CancellationToken cancellationToken = default)
    {
        var purchaseReturn = await purchaseReturns.GetByIdAsync(purchaseReturnId, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseReturn), purchaseReturnId);

        var rows = await GetApplicationsAsync(purchaseReturn.Id, cancellationToken);
        var active = rows.Where(x => x.Status == SupplierCreditApplicationStatus.Active).ToArray();
        var totalCredit = await GetPostedSourceCreditBaseAmountAsync(purchaseReturn, cancellationToken);
        var applied = Round(active.Sum(x => x.AppliedSourceBaseAmount));
        var available = Math.Max(0m, Round(totalCredit - applied));

        return new SupplierCreditSnapshot(
            purchaseReturn.Id,
            purchaseReturn.SupplierId,
            totalCredit,
            applied,
            available,
            rows.OrderByDescending(x => x.AppliedAtUtc).ToArray());
    }

    private async Task<SupplierCreditApplication?> ApplyCoreAsync(
        PurchaseReturn purchaseReturn,
        decimal sourceCreditBaseAmount,
        Guid targetInvoiceId,
        decimal? requestedBaseAmount,
        string idempotencyKey,
        DateTime appliedAtUtc,
        string appliedBy,
        bool allowNoOutstanding,
        CancellationToken cancellationToken,
        bool lockAlreadyAcquired = false)
    {
        if (sourceCreditBaseAmount <= 0m)
            throw new ConflictException(
                "supplier_credit_return_not_ap_credit",
                "المرتجع المحدد لا يحتوي حركة مدينة مرحلة على حساب المورد؛ المرتجع قبل الفوترة يعكس GRNI فقط.");

        if (!lockAlreadyAcquired)
            await concurrencyPort.LockAsync(purchaseReturn.Id, targetInvoiceId, cancellationToken);

        var previousByKey = await creditApplications.ListAsync(
            new Specification<SupplierCreditApplication>().Where(x => x.IdempotencyKey == idempotencyKey),
            cancellationToken);
        var existing = previousByKey.SingleOrDefault();
        if (existing is not null)
        {
            if (existing.PurchaseReturnId != purchaseReturn.Id || existing.PurchaseInvoiceId != targetInvoiceId)
                throw new ConflictException("supplier_credit_idempotency_conflict", "مفتاح منع التكرار مستخدم لعملية ائتمان مختلفة.");
            if (existing.Status == SupplierCreditApplicationStatus.Reversed)
                throw new ConflictException("supplier_credit_idempotency_reversed", "عملية الائتمان المرتبطة بمفتاح منع التكرار تم عكسها ولا يمكن إعادة استخدامها.");
            return existing;
        }

        var targetInvoice = await purchaseInvoices.GetByIdAsync(targetInvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), targetInvoiceId);
        if (targetInvoice.Status != PurchaseInvoiceStatus.Posted)
            throw new ConflictException("supplier_credit_invoice_not_posted", "لا يمكن تطبيق ائتمان مورد إلا على فاتورة مورد مرحلة.");
        if (targetInvoice.SupplierId != purchaseReturn.SupplierId)
            throw new ConflictException("supplier_credit_supplier_mismatch", "لا يمكن تطبيق ائتمان مورد على فاتورة تخص موردًا آخر.");
        if (targetInvoice.ExchangeRate <= 0m)
            throw new ConflictException("supplier_credit_invoice_rate_invalid", "سعر الصرف التاريخي لفاتورة المورد غير صالح.");

        var sourceApplications = await GetApplicationsAsync(purchaseReturn.Id, cancellationToken);
        var activeSourceApplied = Round(sourceApplications
            .Where(x => x.Status == SupplierCreditApplicationStatus.Active)
            .Sum(x => x.AppliedSourceBaseAmount));
        var availableSourceBase = Math.Max(0m, Round(sourceCreditBaseAmount - activeSourceApplied));

        var balance = await invoiceBalances.GetAsync(targetInvoice, null, cancellationToken);
        var maximumBase = Math.Min(availableSourceBase, balance.OutstandingBaseAmount);
        if (maximumBase <= 0m)
        {
            if (allowNoOutstanding) return null;
            throw new ConflictException("supplier_credit_no_available_balance", "لا يوجد رصيد ائتمان متاح أو لا يوجد متبقٍ على فاتورة المورد الهدف.");
        }

        var appliedSourceBase = requestedBaseAmount.HasValue ? Round(requestedBaseAmount.Value) : maximumBase;
        if (appliedSourceBase <= 0m || appliedSourceBase > availableSourceBase)
            throw new ConflictException("supplier_credit_exceeds_available", "قيمة الائتمان المطلوبة تتجاوز الرصيد المتاح من المرتجع.");
        if (appliedSourceBase > balance.OutstandingBaseAmount)
            throw new ConflictException("supplier_credit_exceeds_invoice_outstanding", "قيمة الائتمان المطلوبة تتجاوز الرصيد المتبقي على فاتورة المورد.");

        var invoiceCurrencyAmount = Math.Min(
            balance.OutstandingAmount,
            Round(appliedSourceBase / targetInvoice.ExchangeRate));
        if (invoiceCurrencyAmount <= 0m)
            throw new ConflictException("supplier_credit_target_amount_invalid", "تعذر احتساب قيمة ائتمان المورد بعملة الفاتورة.");

        var application = SupplierCreditApplication.Create(
            Guid.NewGuid(),
            purchaseReturn.Id,
            targetInvoice.Id,
            purchaseReturn.SupplierId,
            appliedSourceBase,
            appliedSourceBase,
            invoiceCurrencyAmount,
            appliedAtUtc,
            appliedBy,
            idempotencyKey);

        await creditApplications.AddAsync(application, cancellationToken);
        return application;
    }

    private Task<IReadOnlyList<SupplierCreditApplication>> GetApplicationsAsync(
        Guid purchaseReturnId,
        CancellationToken cancellationToken) =>
        creditApplications.ListAsync(
            new Specification<SupplierCreditApplication>().Where(x => x.PurchaseReturnId == purchaseReturnId),
            cancellationToken);

    private async Task<decimal> GetPostedSourceCreditBaseAmountAsync(
        PurchaseReturn purchaseReturn,
        CancellationToken cancellationToken)
    {
        if (purchaseReturn.Status != PurchaseReturnStatus.Posted || !purchaseReturn.JournalEntryId.HasValue)
            return 0m;

        var amount = await creditSourcePort.GetPostedApCreditBaseAmountAsync(
            purchaseReturn.Id,
            purchaseReturn.JournalEntryId.Value,
            purchaseReturn.SupplierId,
            cancellationToken);

        return Round(Math.Max(0m, amount));
    }

    private static string AutoIdempotencyKey(Guid returnId, Guid invoiceId) =>
        $"PRC:{returnId:N}:{invoiceId:N}";

    private static decimal Round(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
