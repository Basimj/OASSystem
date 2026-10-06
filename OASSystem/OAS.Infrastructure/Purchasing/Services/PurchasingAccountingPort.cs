using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Infrastructure.Purchasing.Services;

/// <summary>
/// Purchasing-to-Accounting adapter. This class only translates purchasing contexts into
/// accounting posting contexts. Posting-profile resolution, account validation, journal
/// construction, balancing and idempotency are owned by Accounting.
/// </summary>
public sealed class PurchasingAccountingPort(
    IProfileAccountingPostingService postingService,
    IReadRepository<PurchaseInvoice, Guid> purchaseInvoices,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IPurchasingAccountingPort
{
    private const string PurchasingModule = "Purchasing";
    private const string ReceiptDocumentType = "PurchaseReceipt";
    private const string InvoiceDocumentType = "PurchaseInvoice";
    private const string ReturnDocumentType = "PurchaseReturn";
    private const string InventoryRole = "Inventory";
    private const string GrniRole = "GoodsReceivedNotInvoiced";
    private const string PurchaseTaxRole = "PurchaseTax";
    private const string PriceVarianceRole = "PurchasePriceVariance";

    public Task ValidatePostingPeriodAsync(DateOnly postingDate, CancellationToken cancellationToken = default)
        => postingService.ValidatePostingPeriodAsync(postingDate, cancellationToken);

    public Task ValidateSupplierAccountAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => postingService.ValidateSupplierSubledgerAsync(supplierId, cancellationToken);

    public async Task<PurchasingAccountingPostingResult> PostPurchaseReceiptJournalAsync(
        PurchasingReceiptAccountingContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Lines.Count == 0)
            throw new ConflictException("purchasing_receipt_no_accounting_lines", "لا توجد بنود مقبولة لإنشاء قيد الاستلام.");

        var lines = new List<ProfileAccountingPostingLine>();
        var total = 0m;
        foreach (var source in context.Lines.Where(x => x.BaseAmount > 0m))
        {
            var amount = Math.Round(source.BaseAmount, 4);
            total += amount;
            lines.Add(new ProfileAccountingPostingLine(
                ReceiptDocumentType, InventoryRole, false,
                amount, 0m, false,
                $"Inventory receipt {context.ReceiptCode}",
                ProductVariantId: source.ProductVariantId,
                SourceLineId: source.PurchaseReceiptLineId));
        }

        if (total <= 0m)
            throw new ConflictException("purchasing_receipt_value_invalid", "قيمة الاستلام المحاسبية يجب أن تكون أكبر من صفر.");

        lines.Add(new ProfileAccountingPostingLine(
            ReceiptDocumentType, GrniRole, false,
            0m, total, false,
            $"GRNI {context.ReceiptCode}",
            SupplierId: context.SupplierId));

        var journalId = await postingService.PostAsync(
            new ProfileAccountingPostingRequest(
                PurchasingModule, ReceiptDocumentType, context.PurchaseReceiptId,
                context.PostingDate, context.PostingDate,
                $"Purchase receipt {context.ReceiptCode}",
                null, 1m, lines),
            ResolveUserGuid(), timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return new PurchasingAccountingPostingResult(journalId);
    }

    public async Task<PurchasingAccountingPostingResult> PostPurchaseInvoiceJournalAsync(
        PurchasingInvoiceAccountingContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.ExchangeRate <= 0m)
            throw new ConflictException("purchasing_exchange_rate_invalid", "سعر الصرف يجب أن يكون أكبر من صفر.");
        if (context.SupplierPayableBaseAmount <= 0m)
            throw new ConflictException("purchasing_supplier_payable_invalid", "قيمة مديونية المورد يجب أن تكون أكبر من صفر.");

        var lines = new List<ProfileAccountingPostingLine>();
        if (context.GrniBaseAmount > 0m)
            lines.Add(RoleDebit(InvoiceDocumentType, GrniRole, context.GrniBaseAmount, true,
                $"GRNI clearance {context.PurchaseInvoiceCode}", context.SupplierId));
        if (context.PurchaseTaxBaseAmount > 0m)
            lines.Add(RoleDebit(InvoiceDocumentType, PurchaseTaxRole, context.PurchaseTaxBaseAmount, true,
                $"Purchase tax {context.PurchaseInvoiceCode}", context.SupplierId));
        if (context.PurchasePriceVarianceBaseAmount > 0m)
            lines.Add(RoleDebit(InvoiceDocumentType, PriceVarianceRole, context.PurchasePriceVarianceBaseAmount, true,
                $"Purchase price variance {context.PurchaseInvoiceCode}", context.SupplierId));
        else if (context.PurchasePriceVarianceBaseAmount < 0m)
            lines.Add(RoleCredit(InvoiceDocumentType, PriceVarianceRole, Math.Abs(context.PurchasePriceVarianceBaseAmount), true,
                $"Purchase price variance {context.PurchaseInvoiceCode}", context.SupplierId));

        lines.Add(new ProfileAccountingPostingLine(
            InvoiceDocumentType, null, true,
            0m, context.SupplierPayableBaseAmount, true,
            $"Supplier payable {context.PurchaseInvoiceCode}",
            SupplierId: context.SupplierId));

        var journalId = await postingService.PostAsync(
            new ProfileAccountingPostingRequest(
                PurchasingModule, InvoiceDocumentType, context.PurchaseInvoiceId,
                context.PostingDate, context.PostingDate,
                $"Purchase invoice {context.PurchaseInvoiceCode}",
                context.CurrencyId, context.ExchangeRate, lines),
            ResolveUserGuid(), timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return new PurchasingAccountingPostingResult(journalId);
    }

    public async Task<PurchasingAccountingPostingResult> PostPurchaseReturnJournalAsync(
        PurchasingReturnAccountingContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.InventoryCostBaseAmount < 0m || context.ReceiptCostBaseAmount < 0m ||
            context.SupplierNetBaseAmount < 0m || context.SupplierTaxBaseAmount < 0m || context.SupplierGrossBaseAmount < 0m)
            throw new ConflictException("purchase_return_amount_invalid", "قيم مرتجع المشتريات المحاسبية غير صالحة.");

        var lines = new List<ProfileAccountingPostingLine>();
        Guid? transactionCurrencyId = null;
        var exchangeRate = 1m;

        if (context.PurchaseInvoiceId.HasValue)
        {
            var invoice = await purchaseInvoices.GetByIdAsync(context.PurchaseInvoiceId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(PurchaseInvoice), context.PurchaseInvoiceId.Value);
            if (invoice.SupplierId != context.SupplierId || invoice.Status != PurchaseInvoiceStatus.Posted)
                throw new ConflictException("purchase_return_invoice_invalid", "فاتورة المورد المرتبطة بالمرتجع غير صالحة للترحيل العكسي.");
            if (context.SupplierGrossBaseAmount <= 0m)
                throw new ConflictException("purchase_return_supplier_amount_invalid", "قيمة عكس مديونية المورد يجب أن تكون أكبر من صفر.");

            transactionCurrencyId = invoice.CurrencyId;
            exchangeRate = invoice.ExchangeRate;
            lines.Add(new ProfileAccountingPostingLine(
                InvoiceDocumentType, null, true,
                context.SupplierGrossBaseAmount, 0m, true,
                $"Supplier return {context.ReturnCode}", SupplierId: context.SupplierId));
            if (context.SupplierTaxBaseAmount > 0m)
                lines.Add(RoleCredit(InvoiceDocumentType, PurchaseTaxRole, context.SupplierTaxBaseAmount, true,
                    $"Purchase tax reversal {context.ReturnCode}", context.SupplierId));
            if (context.InventoryCostBaseAmount > 0m)
                lines.Add(RoleCredit(ReceiptDocumentType, InventoryRole, context.InventoryCostBaseAmount, false,
                    $"Inventory purchase return {context.ReturnCode}", context.SupplierId));
            AddVarianceLine(lines, context.PurchasePriceVarianceBaseAmount, context.ReturnCode, context.SupplierId);
        }
        else
        {
            if (context.ReceiptCostBaseAmount <= 0m)
                throw new ConflictException("purchase_return_receipt_amount_invalid", "قيمة تكلفة الاستلام للمرتجع يجب أن تكون أكبر من صفر.");
            lines.Add(RoleDebit(ReceiptDocumentType, GrniRole, context.ReceiptCostBaseAmount, false,
                $"GRNI reversal {context.ReturnCode}", context.SupplierId));
            lines.Add(RoleCredit(ReceiptDocumentType, InventoryRole, context.InventoryCostBaseAmount, false,
                $"Inventory purchase return {context.ReturnCode}", context.SupplierId));
            AddVarianceLine(lines, context.PurchasePriceVarianceBaseAmount, context.ReturnCode, context.SupplierId);
        }

        var journalId = await postingService.PostAsync(
            new ProfileAccountingPostingRequest(
                PurchasingModule, ReturnDocumentType, context.PurchaseReturnId,
                context.PostingDate, context.PostingDate,
                $"Purchase return {context.ReturnCode}",
                transactionCurrencyId, exchangeRate, lines),
            ResolveUserGuid(), timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return new PurchasingAccountingPostingResult(journalId);
    }

    private static ProfileAccountingPostingLine RoleDebit(string profileType, string role, decimal amount, bool tx, string description, Guid supplierId)
        => new(profileType, role, false, amount, 0m, tx, description, SupplierId: supplierId);

    private static ProfileAccountingPostingLine RoleCredit(string profileType, string role, decimal amount, bool tx, string description, Guid supplierId)
        => new(profileType, role, false, 0m, amount, tx, description, SupplierId: supplierId);

    private static void AddVarianceLine(List<ProfileAccountingPostingLine> lines, decimal variance, string returnCode, Guid supplierId)
    {
        if (variance < 0m)
            lines.Add(RoleDebit(InvoiceDocumentType, PriceVarianceRole, Math.Abs(variance), false,
                $"Purchase return variance {returnCode}", supplierId));
        else if (variance > 0m)
            lines.Add(RoleCredit(InvoiceDocumentType, PriceVarianceRole, variance, false,
                $"Purchase return variance {returnCode}", supplierId));
    }

    private Guid ResolveUserGuid()
    {
        if (Guid.TryParse(currentUser.UserId, out var userId) && userId != Guid.Empty) return userId;
        throw new ConflictException("purchasing_posting_user_invalid", "تعذر تحديد المستخدم المسؤول عن الترحيل المحاسبي.");
    }
}
