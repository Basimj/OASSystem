using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

/// <summary>
/// Keeps the commercial lines of an invoice-first draft and its operational order
/// synchronized inside the outer command transaction. Neither document may be
/// changed independently after confirmation/reservation.
/// </summary>
public sealed class LinkedSalesDraftSynchronizer(
    ISalesInvoiceAggregateRepository invoices,
    IRepository<SalesInvoiceLine, Guid> invoiceLines,
    IRepository<SalesInvoiceLinePrescriptionSnapshot, Guid> invoiceSnapshots,
    IReadRepository<CustomerOrderLineOpticalSnapshot, Guid> orderSnapshots,
    IReadRepository<SalesPriceOverride, Guid> priceOverrides,
    ISalesLineResolver resolver)
{
    public async Task<SalesInvoice?> FindLinkedInvoiceAsync(Guid orderId, CancellationToken ct)
    {
        var linked = await invoices.ListAsync(
            new Specification<SalesInvoice>()
                .Where(x => x.CustomerOrderId == orderId && x.Status != SalesInvoiceStatus.Cancelled), ct);
        if (linked.Count > 1)
            throw new ConflictException("sales_order_multiple_invoices",
                "هذا الطلب مرتبط بأكثر من فاتورة غير ملغاة. يجب مراجعة الربط أولًا لمنع تعديل مبالغ فواتير تاريخية.");
        if (linked.Count == 0) return null;
        return await invoices.GetAggregateAsync(linked[0].Id, true, ct);
    }

    public static void EnsureEditable(CustomerOrder order, SalesInvoice invoice)
    {
        if (order.Status != CustomerOrderStatus.Draft || invoice.Status != SalesInvoiceStatus.Draft)
            throw new ConflictException("sales_linked_draft_locked",
                "يمكن تعديل بنود طلب العميل والفاتورة المرتبطة فقط ما داما مسودتين. بعد التأكيد استخدم إجراءات التصحيح المعتمدة.");
    }

    public async Task SynchronizeInvoiceAsync(CustomerOrder order, SalesInvoice invoice, CancellationToken ct)
    {
        EnsureEditable(order, invoice);
        // Preserve invoice-specific posting date and salesperson, while keeping the
        // shared customer, payment policy, tax and currency coherent with the order.
        invoice.UpdateHeader(order.CustomerId, order.Id, order.PrescriptionRevisionId,
            order.OrderDate, invoice.PostingDate, order.TaxCalculationMode, order.PaymentPlan,
            order.PaymentTermDaysSnapshot, invoice.Description);
        if (invoice.CurrencyId != order.CurrencyId || invoice.ExchangeRate != order.ExchangeRate ||
            invoice.CurrencyDecimalPlacesSnapshot != order.CurrencyDecimalPlacesSnapshot)
        {
            invoice.ChangeCurrency(order.CurrencyId, order.CurrencyCodeSnapshot,
                order.CurrencySymbolSnapshot, order.CurrencyDecimalPlacesSnapshot,
                order.ExchangeRate, order.ExchangeRateDate, order.ExchangeRateType,
                order.ExchangeRateSource, invoice.BaseCurrencyId,
                invoice.BaseCurrencyCodeSnapshot, invoice.BaseCurrencyDecimalPlacesSnapshot);
        }

        var currentOrderIds = order.Lines.Where(x => x.IsActive).Select(x => x.Id).ToHashSet();
        // Remove deleted/replaced order lines before inserting new ones. The invoice
        // retains the same line ID for price-only edits to preserve audit references.
        foreach (var old in invoice.Lines.Where(x => !x.CustomerOrderLineId.HasValue ||
                     !currentOrderIds.Contains(x.CustomerOrderLineId.Value)).ToArray())
            await RemoveInvoiceLineAsync(invoice, old, ct);

        foreach (var orderLine in order.Lines.Where(x => x.IsActive).OrderBy(x => x.LineNumber))
        {
            var existing = invoice.Lines.SingleOrDefault(x => x.CustomerOrderLineId == orderLine.Id);
            var sameStructure = existing is not null &&
                existing.GroupId == orderLine.GroupId &&
                existing.LineType == orderLine.LineType &&
                existing.ProductVariantId == orderLine.ProductVariantId &&
                existing.WarehouseId == orderLine.WarehouseId &&
                existing.PrescriptionRevisionId == orderLine.PrescriptionRevisionId &&
                existing.PrescriptionEye == orderLine.PrescriptionEye &&
                existing.RequiresProduction == orderLine.RequiresProduction &&
                existing.Notes == orderLine.Notes &&
                existing.DescriptionSnapshot == orderLine.DescriptionSnapshot;

            if (existing is not null && !sameStructure)
            {
                await RemoveInvoiceLineAsync(invoice, existing, ct);
                existing = null;
            }
            if (existing is null)
            {
                var product = await resolver.ResolveAsync(orderLine.LineType,
                    orderLine.ProductVariantId, orderLine.WarehouseId,
                    orderLine.DescriptionSnapshot, ct);
                existing = SalesInvoiceLine.Create(Guid.NewGuid(), invoice.Id,
                    invoice.Lines.Count + 1, orderLine.Id, orderLine.GroupId, orderLine.LineType,
                    orderLine.ProductVariantId, orderLine.WarehouseId,
                    product.ProductCode, product.ProductName, orderLine.DescriptionSnapshot,
                    product.UnitName, orderLine.Quantity, orderLine.BaseUnitPrice,
                    orderLine.ActualUnitPrice, orderLine.DiscountType, orderLine.DiscountValue,
                    orderLine.TaxRate, orderLine.PrescriptionRevisionId, orderLine.PrescriptionEye,
                    orderLine.RequiresProduction, orderLine.Notes, invoice.TaxCalculationMode,
                    invoice.CurrencyDecimalPlacesSnapshot, invoice.ExchangeRate,
                    invoice.BaseCurrencyDecimalPlacesSnapshot);
                invoice.AddLine(existing);
                await invoiceLines.AddAsync(existing, ct);
            }
            else if (existing.Quantity != orderLine.Quantity || existing.BaseUnitPrice != orderLine.BaseUnitPrice ||
                     existing.ActualUnitPrice != orderLine.ActualUnitPrice ||
                     existing.DiscountType != orderLine.DiscountType ||
                     existing.DiscountValue != orderLine.DiscountValue ||
                     existing.TaxRate != orderLine.TaxRate)
            {
                if (existing.ActualUnitPrice != orderLine.ActualUnitPrice)
                {
                    var previousOverrides = await priceOverrides.ListAsync(
                        new Specification<SalesPriceOverride>()
                            .Where(x => x.SalesInvoiceLineId == existing.Id), ct);
                    // A manually entered price is valid for an unpriced product.
                    // A stored-price product must use the explicit approval flow.
                    if (orderLine.BaseUnitPrice > 0m &&
                        orderLine.ActualUnitPrice != orderLine.BaseUnitPrice &&
                        !previousOverrides.Any(x => x.IsApprovedFor(orderLine.ActualUnitPrice)))
                        throw new ConflictException("sales_linked_price_override_required",
                            "لتغيير سعر صنف له سعر معتمد، استخدم طلب تعديل السعر واعتماده من الفاتورة أولًا.");
                    if (previousOverrides.Count > 0 &&
                        !previousOverrides.Any(x => x.IsApprovedFor(orderLine.ActualUnitPrice)))
                        throw new ConflictException("sales_linked_price_override_exists",
                            "هذا البند لديه سجل تعديل سعر؛ راجع الاعتمادات قبل تغيير سعره.");
                }
                invoice.UpdateLinePricing(existing.Id, orderLine.Quantity, orderLine.BaseUnitPrice,
                    orderLine.ActualUnitPrice, orderLine.DiscountType,
                    orderLine.DiscountValue, orderLine.TaxRate);
            }
            await SynchronizeOpticalAsync(invoice, existing, orderLine.Id, ct);
        }
        // Header discount may be edited from the order screen. Mirror it as well.
        if (invoice.HeaderDiscountType != order.HeaderDiscountType ||
            invoice.HeaderDiscountValue != order.HeaderDiscountValue ||
            invoice.HeaderDiscountReason != order.HeaderDiscountReason)
            invoice.SetHeaderDiscount(order.HeaderDiscountType, order.HeaderDiscountValue,
                order.HeaderDiscountReason);
        if (order.HeaderDiscountApprovedAtUtc.HasValue &&
            order.HeaderDiscountApprovedBy is not null &&
            invoice.HeaderDiscountAmount > 0m && !invoice.HeaderDiscountApprovedAtUtc.HasValue)
            invoice.ApproveHeaderDiscount(order.HeaderDiscountApprovedAtUtc.Value,
                order.HeaderDiscountApprovedBy);
    }

    private async Task RemoveInvoiceLineAsync(SalesInvoice invoice, SalesInvoiceLine line, CancellationToken ct)
    {
        if (await priceOverrides.CountAsync(new Specification<SalesPriceOverride>()
                .Where(x => x.SalesInvoiceLineId == line.Id), ct) > 0)
            throw new ConflictException("sales_invoice_line_has_price_override_history",
                "لا يمكن حذف أو استبدال بند له سجل تغيير سعر. راجع السجل أولًا.");
        foreach (var optical in await invoiceSnapshots.ListAsync(
                     new Specification<SalesInvoiceLinePrescriptionSnapshot>()
                         .Where(x => x.SalesInvoiceLineId == line.Id).Tracking(), ct))
            invoiceSnapshots.Delete(optical);
        invoice.ClearLinePrescriptionSnapshot(line.Id);
        invoice.RemoveLine(line.Id);
        invoiceLines.Delete(line);
    }

    private async Task SynchronizeOpticalAsync(SalesInvoice invoice, SalesInvoiceLine invoiceLine,
        Guid orderLineId, CancellationToken ct)
    {
        var source = (await orderSnapshots.ListAsync(
            new Specification<CustomerOrderLineOpticalSnapshot>()
                .Where(x => x.CustomerOrderLineId == orderLineId && x.IsActive), ct)).SingleOrDefault();
        var existing = (await invoiceSnapshots.ListAsync(
            new Specification<SalesInvoiceLinePrescriptionSnapshot>()
                .Where(x => x.SalesInvoiceLineId == invoiceLine.Id && x.IsActive).Tracking(), ct)).SingleOrDefault();
        if (source is null && existing is null) return;
        if (source is not null && existing is not null &&
            source.PrescriptionRevisionId == existing.PrescriptionRevisionId &&
            source.Eye == existing.Eye && source.SPH == existing.SPH &&
            source.CYL == existing.CYL && source.Axis == existing.Axis &&
            source.ADD == existing.ADD && source.Prism == existing.Prism &&
            source.PrismBase == existing.PrismBase && source.PD == existing.PD &&
            source.MonocularPD == existing.MonocularPD && source.VA == existing.VA &&
            source.FittingHeight == existing.FittingHeight) return;
        if (existing is not null)
        {
            invoiceSnapshots.Delete(existing);
            invoice.ClearLinePrescriptionSnapshot(invoiceLine.Id);
        }
        if (source is null) return;
        var copy = SalesInvoiceLinePrescriptionSnapshot.Create(Guid.NewGuid(), invoiceLine.Id,
            source.PrescriptionRevisionId, source.Eye, source.SPH, source.CYL,
            source.Axis, source.ADD, source.Prism, source.PrismBase, source.PD,
            source.MonocularPD, source.VA, source.FittingHeight);
        invoice.SetLinePrescriptionSnapshot(invoiceLine.Id, copy);
        await invoiceSnapshots.AddAsync(copy, ct);
    }
}
