using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class CreateSalesInvoiceFromOrderCommandHandler(
    ICustomerOrderAggregateRepository orders,
    ISalesInvoiceAggregateRepository invoices,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<Currency, Guid> currencies,
    ISalesLineResolver lineResolver,
    ISequenceNumberGenerator sequences,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler)
    : IRequestHandler<CreateSalesInvoiceFromOrderCommand, SalesInvoiceDto>
{
    public async Task<SalesInvoiceDto> Handle(CreateSalesInvoiceFromOrderCommand request, CancellationToken ct)
    {
        var order = await orders.GetAggregateAsync(request.OrderId, true, ct)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.OrderId);
        SalesConcurrency.Ensure(request.Request.OrderRowVersion, order.RowVersion, "طلب العميل");

        if (order.Status is CustomerOrderStatus.Draft or CustomerOrderStatus.Cancelled)
            throw new ConflictException("sales_order_invalid_status", "يجب تأكيد الطلب قبل إنشاء الفاتورة منه.");

        var customer = await customers.GetByIdAsync(order.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), order.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");

        var code = request.Request.InvoiceCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            code = SalesInvoiceCodeFormatter.Format(
                await sequences.NextAsync("SalesInvoiceCodeSequence", ct),
                request.Request.InvoiceDate);

        if (await invoices.CountAsync(
                new Specification<SalesInvoice>().Where(x => x.InvoiceCode == code), ct) > 0)
        {
            throw new ConflictException(SalesErrorCodes.DuplicateInvoiceCode, "كود الفاتورة مستخدم مسبقًا.");
        }

        var baseCurrency = await GetBaseAsync(ct);
        var invoice = SalesInvoice.Create(
            Guid.NewGuid(), code, order.CustomerId, order.Id, order.PrescriptionRevisionId,
            request.Request.InvoiceDate, request.Request.PostingDate,
            order.CurrencyId, order.CurrencyCodeSnapshot, order.CurrencySymbolSnapshot,
            order.CurrencyDecimalPlacesSnapshot, order.ExchangeRate, order.ExchangeRateDate,
            order.ExchangeRateType, order.ExchangeRateSource, order.TaxCalculationMode,
            order.PaymentTermType, order.PaymentTermDaysSnapshot,
            baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces, request.Request.Description);

        var lineNumber = 1;
        foreach (var orderLine in order.Lines.Where(x => x.IsActive).OrderBy(x => x.LineNumber))
        {
            var resolved = await lineResolver.ResolveAsync(
                orderLine.LineType,
                orderLine.ProductVariantId,
                orderLine.WarehouseId,
                orderLine.DescriptionSnapshot,
                ct);

            invoice.AddLine(SalesInvoiceLine.Create(
                Guid.NewGuid(), invoice.Id, lineNumber++, orderLine.Id, orderLine.GroupId, orderLine.LineType,
                orderLine.ProductVariantId, orderLine.WarehouseId, resolved.ProductCode, resolved.ProductName,
                orderLine.DescriptionSnapshot, resolved.UnitName, orderLine.Quantity, orderLine.BaseUnitPrice,
                orderLine.ActualUnitPrice, orderLine.DiscountType, orderLine.DiscountValue, orderLine.TaxRate,
                orderLine.PrescriptionRevisionId, orderLine.PrescriptionEye, orderLine.RequiresProduction,
                orderLine.Notes, invoice.TaxCalculationMode, invoice.CurrencyDecimalPlacesSnapshot,
                invoice.ExchangeRate, invoice.BaseCurrencyDecimalPlacesSnapshot));
        }

        await invoices.AddAsync(invoice, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.InvoiceAsync(invoice, ct);
    }

    private async Task<Currency> GetBaseAsync(CancellationToken ct)
    {
        var accountingSettings = await settings.GetByIdAsync(AccountingSettings.SingletonId, ct)
            ?? throw new ConflictException("accounting_settings_required", "يجب إعداد المحاسبة والعملة الأساسية أولًا.");
        return await currencies.GetByIdAsync(accountingSettings.BaseCurrencyId, ct)
            ?? throw new ConflictException("base_currency_missing", "العملة الأساسية غير موجودة.");
    }
}
