using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class SalesInvoiceFromOrderService(
    ISalesInvoiceAggregateRepository invoices,
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<Currency, Guid> currencies,
    IReadRepository<CustomerOrderLineOpticalSnapshot, Guid> orderSnapshots,
    IRepository<SalesInvoiceLinePrescriptionSnapshot, Guid> invoiceSnapshots,
    ISalesLineResolver lineResolver,
    ISequenceNumberGenerator sequences) : ISalesInvoiceFromOrderService
{
    public async Task<SalesInvoice> CreateAsync(
        CustomerOrder order,
        DateOnly invoiceDate,
        DateOnly postingDate,
        string? invoiceCode,
        string? description,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Status is not (CustomerOrderStatus.Confirmed or CustomerOrderStatus.ReadyForProduction or CustomerOrderStatus.ReadyForDelivery))
            throw new ConflictException("sales_order_invalid_status", "يمكن إنشاء الفاتورة فقط بعد اكتمال توفر مواد الطلب.");

        var existingHeader = (await invoices.ListAsync(
            new Specification<SalesInvoice>().Where(x => x.CustomerOrderId == order.Id && x.Status != SalesInvoiceStatus.Cancelled),
            cancellationToken)).FirstOrDefault();
        if (existingHeader is not null)
            return await invoices.GetAggregateAsync(existingHeader.Id, true, cancellationToken) ?? existingHeader;

        var code = invoiceCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            code = SalesInvoiceCodeFormatter.Format(
                await sequences.NextAsync("SalesInvoiceCodeSequence", cancellationToken),
                invoiceDate);

        if (await invoices.CountAsync(
                new Specification<SalesInvoice>().Where(x => x.InvoiceCode == code), cancellationToken) > 0)
            throw new ConflictException(SalesErrorCodes.DuplicateInvoiceCode, "كود الفاتورة مستخدم مسبقًا.");

        var accountingSettings = await settings.GetByIdAsync(AccountingSettings.SingletonId, cancellationToken)
            ?? throw new ConflictException("accounting_settings_required", "يجب إعداد المحاسبة والعملة الأساسية أولًا.");
        var baseCurrency = await currencies.GetByIdAsync(accountingSettings.BaseCurrencyId, cancellationToken)
            ?? throw new ConflictException("base_currency_required", "العملة الأساسية غير موجودة.");

        var invoice = SalesInvoice.Create(
            Guid.NewGuid(), code, order.CustomerId, order.Id, order.PrescriptionRevisionId,
            invoiceDate, postingDate,
            order.CurrencyId, order.CurrencyCodeSnapshot, order.CurrencySymbolSnapshot,
            order.CurrencyDecimalPlacesSnapshot, order.ExchangeRate, order.ExchangeRateDate,
            order.ExchangeRateType, order.ExchangeRateSource, order.TaxCalculationMode,
            order.PaymentPlan, order.PaymentTermDaysSnapshot,
            baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces, description);

        var lineNumber = 1;
        foreach (var orderLine in order.Lines.Where(x => x.IsActive).OrderBy(x => x.LineNumber))
        {
            var resolved = await lineResolver.ResolveAsync(
                orderLine.LineType,
                orderLine.ProductVariantId,
                orderLine.WarehouseId,
                orderLine.DescriptionSnapshot,
                cancellationToken);

            var invoiceLine = SalesInvoiceLine.Create(
                Guid.NewGuid(), invoice.Id, lineNumber++, orderLine.Id, orderLine.GroupId, orderLine.LineType,
                orderLine.ProductVariantId, orderLine.WarehouseId, resolved.ProductCode, resolved.ProductName,
                orderLine.DescriptionSnapshot, resolved.UnitName, orderLine.Quantity, orderLine.BaseUnitPrice,
                orderLine.ActualUnitPrice, orderLine.DiscountType, orderLine.DiscountValue, orderLine.TaxRate,
                orderLine.PrescriptionRevisionId, orderLine.PrescriptionEye, orderLine.RequiresProduction,
                orderLine.Notes, invoice.TaxCalculationMode, invoice.CurrencyDecimalPlacesSnapshot,
                invoice.ExchangeRate, invoice.BaseCurrencyDecimalPlacesSnapshot);
            invoice.AddLine(invoiceLine);

            var optical = (await orderSnapshots.ListAsync(
                new Specification<CustomerOrderLineOpticalSnapshot>()
                    .Where(x => x.CustomerOrderLineId == orderLine.Id && x.IsActive), cancellationToken))
                .SingleOrDefault();
            if (optical is null)
                continue;

            var snapshot = SalesInvoiceLinePrescriptionSnapshot.Create(
                Guid.NewGuid(),
                invoiceLine.Id,
                optical.PrescriptionRevisionId,
                optical.Eye,
                optical.SPH,
                optical.CYL,
                optical.Axis,
                optical.ADD,
                optical.Prism,
                optical.PrismBase,
                optical.PD,
                optical.MonocularPD,
                optical.VA,
                optical.FittingHeight);
            invoice.SetLinePrescriptionSnapshot(invoiceLine.Id, snapshot);
            await invoiceSnapshots.AddAsync(snapshot, cancellationToken);
        }

        await invoices.AddAsync(invoice, cancellationToken);
        return invoice;
    }
}
