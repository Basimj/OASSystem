using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class CreateSalesInvoiceCommandHandler(
    ISalesInvoiceAggregateRepository repository,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<Currency, Guid> currencies,
    IExchangeRateResolver rates,
    ISalesLineResolver lineResolver,
    ISalesPrescriptionValidator prescriptionValidator,
    ISequenceNumberGenerator sequences)
    : IRequestHandler<CreateSalesInvoiceCommand, SalesInvoice>
{
    public async Task<SalesInvoice> Handle(CreateSalesInvoiceCommand request, CancellationToken ct)
    {
        var d = request.Data;
        var customer = await customers.GetByIdAsync(d.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), d.CustomerId);

        EnsureCustomer(customer, (SalesPaymentTermType)(byte)d.PaymentTermType);

        if (d.Lines.Count == 0)
            throw new ConflictException("sales_invoice_lines_required", "يجب أن تحتوي الفاتورة على سطر واحد على الأقل.");

        var code = d.InvoiceCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            code = SalesInvoiceCodeFormatter.Format(
                await sequences.NextAsync("SalesInvoiceCodeSequence", ct),
                d.InvoiceDate);

        if (await repository.CountAsync(
                new Specification<SalesInvoice>().Where(x => x.InvoiceCode == code), ct) > 0)
            throw new ConflictException(SalesErrorCodes.DuplicateInvoiceCode, "كود الفاتورة مستخدم مسبقًا.");

        var rate = await rates.ResolveAsync(
            d.CurrencyId,
            d.InvoiceDate,
            ExchangeRateType.Accounting,
            cancellationToken: ct);

        var baseCurrency = await GetBaseCurrencyAsync(ct);

        var invoice = SalesInvoice.Create(
            Guid.NewGuid(),
            code,
            customer.Id,
            null,
            d.PrescriptionRevisionId,
            d.InvoiceDate,
            d.PostingDate,
            rate.CurrencyId,
            rate.CurrencyCode,
            rate.CurrencySymbol,
            rate.CurrencyDecimalPlaces,
            rate.Rate,
            rate.RateDate,
            rate.RateType,
            rate.Source,
            (TaxCalculationMode)(byte)d.TaxCalculationMode,
            (SalesPaymentTermType)(byte)d.PaymentTermType,
            customer.PaymentTermDays,
            baseCurrency.Id,
            baseCurrency.Code,
            baseCurrency.DecimalPlaces,
            d.Description);

        var lineNumber = 1;
        foreach (var req in d.Lines)
            invoice.AddLine(await BuildLineAsync(invoice, lineNumber++, req, ct));

        await repository.AddAsync(invoice, ct);
        return invoice;
    }

    private async Task<Currency> GetBaseCurrencyAsync(CancellationToken ct)
    {
        var accountingSettings = await settings.GetByIdAsync(AccountingSettings.SingletonId, ct)
            ?? throw new ConflictException(
                "accounting_settings_required",
                "يجب إعداد المحاسبة والعملة الأساسية أولًا.");

        return await currencies.GetByIdAsync(accountingSettings.BaseCurrencyId, ct)
            ?? throw new ConflictException("base_currency_missing", "العملة الأساسية غير موجودة.");
    }

    private async Task<SalesInvoiceLine> BuildLineAsync(
        SalesInvoice invoice,
        int number,
        SalesInvoiceLineRequest req,
        CancellationToken ct)
    {
        var type = (SalesLineType)(byte)req.LineType;
        var resolved = await lineResolver.ResolveAsync(
            type,
            req.ProductVariantId,
            req.WarehouseId,
            req.Description,
            ct);

        var revision = req.PrescriptionRevisionId ?? invoice.PrescriptionRevisionId;
        var eye = req.PrescriptionEye.HasValue
            ? (EyeSide?)(byte)req.PrescriptionEye.Value
            : null;

        await prescriptionValidator.ValidateLineAsync(
            revision,
            eye,
            resolved.PrescriptionRequired,
            resolved.OpticalPolicy,
            ct);

        var standardUnitPrice = resolved.ProductVariantId.HasValue
            ? SalesPricingCalculator.ConvertFromBase(
                resolved.BaseUnitPrice,
                invoice.ExchangeRate,
                invoice.CurrencyDecimalPlacesSnapshot)
            : resolved.BaseUnitPrice;

        var actualUnitPrice = resolved.ProductVariantId.HasValue
            ? standardUnitPrice
            : req.ActualUnitPrice;

        return SalesInvoiceLine.Create(
            Guid.NewGuid(),
            invoice.Id,
            number,
            req.CustomerOrderLineId,
            req.GroupId,
            type,
            resolved.ProductVariantId,
            resolved.WarehouseId,
            resolved.ProductCode,
            resolved.ProductName,
            resolved.Description,
            resolved.UnitName,
            req.Quantity,
            standardUnitPrice,
            actualUnitPrice,
            (SalesDiscountType)(byte)req.DiscountType,
            req.DiscountValue,
            req.TaxRate,
            revision,
            eye,
            req.RequiresProduction,
            req.Notes,
            invoice.TaxCalculationMode,
            invoice.CurrencyDecimalPlacesSnapshot,
            invoice.ExchangeRate,
            invoice.BaseCurrencyDecimalPlacesSnapshot);
    }

    private static void EnsureCustomer(Customer customer, SalesPaymentTermType paymentTerm)
    {
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");

        if (paymentTerm == SalesPaymentTermType.Credit && !customer.IsCreditAllowed)
            throw new ConflictException(SalesErrorCodes.CreditNotAllowed, "البيع الآجل غير مسموح لهذا العميل.");
    }
}
