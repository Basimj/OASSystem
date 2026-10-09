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
using OAS.Domain.Features.Employees.Entities;
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
    IReadRepository<Employee, Guid> employees,
    IExchangeRateResolver rates,
    ICustomerOrderOpticalService optical,
    ISequenceNumberGenerator sequences)
    : IRequestHandler<CreateSalesInvoiceCommand, SalesInvoice>
{
    public async Task<SalesInvoice> Handle(CreateSalesInvoiceCommand request, CancellationToken ct)
    {
        var d = request.Data;
        var customer = await customers.GetByIdAsync(d.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), d.CustomerId);

        var paymentPlan = (SalesPaymentPlan)(byte)d.PaymentPlan;
        if (paymentPlan == SalesPaymentPlan.PayOnPickup)
            throw new ConflictException(
                "direct_sale_pay_on_pickup_not_supported",
                "الدفع عند الاستلام يتطلب طلب عميل ومسار Checkout. البيع المباشر يدعم الدفع الكامل أو الجزئي أو البيع الآجل فقط.");
        EnsureCustomer(customer, paymentPlan);

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
            paymentPlan,
            paymentPlan == SalesPaymentPlan.AccountCredit ? customer.PaymentTermDays : 0,
            baseCurrency.Id,
            baseCurrency.Code,
            baseCurrency.DecimalPlaces,
            d.Description);

        if (d.SalesEmployeeId.HasValue)
        {
            var employee = await employees.GetByIdAsync(d.SalesEmployeeId.Value, ct)
                ?? throw new NotFoundException(nameof(Employee), d.SalesEmployeeId.Value);
            if (!employee.IsActive)
                throw new ConflictException("sales_employee_inactive", "موظف المبيعات غير فعال.");
            invoice.SetSalesEmployee(employee.Id);
        }

        var lineNumber = 1;
        foreach (var req in d.Lines)
        {
            var prepared = await BuildLineAsync(invoice, lineNumber++, req, ct);
            invoice.AddLine(prepared.Line);
            if (prepared.Snapshot is not null)
                invoice.SetLinePrescriptionSnapshot(prepared.Line.Id, prepared.Snapshot);
        }

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

    private async Task<PreparedInvoiceLine> BuildLineAsync(
        SalesInvoice invoice,
        int number,
        SalesInvoiceLineRequest req,
        CancellationToken ct)
    {
        var type = (SalesLineType)(byte)req.LineType;
        var requestEye = req.PrescriptionEye.HasValue
            ? (EyeSide?)(byte)req.PrescriptionEye.Value
            : null;
        var prepared = await optical.PrepareAsync(
            type,
            req.ProductVariantId,
            req.WarehouseId,
            req.Description,
            req.PrescriptionRevisionId,
            requestEye,
            invoice.PrescriptionRevisionId,
            req.OpticalSnapshot,
            ct);
        var resolved = prepared.Resolution;

        var standardUnitPrice = resolved.ProductVariantId.HasValue
            ? SalesPricingCalculator.ConvertFromBase(
                resolved.BaseUnitPrice,
                invoice.ExchangeRate,
                invoice.CurrencyDecimalPlacesSnapshot)
            : resolved.BaseUnitPrice;

        // Direct invoices preserve the approval model: a product with a configured price
        // starts from that configured price. Products without a price can be priced manually.
        var actualUnitPrice = resolved.ProductVariantId.HasValue && standardUnitPrice > 0m
            ? standardUnitPrice
            : req.ActualUnitPrice;
        var requiresProduction = SalesLensLinePolicy.ResolveRequiresProduction(
            type,
            resolved,
            req.GroupId,
            req.RequiresProduction);

        var line = SalesInvoiceLine.Create(
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
            prepared.PrescriptionRevisionId,
            prepared.PrescriptionEye,
            requiresProduction,
            req.Notes,
            invoice.TaxCalculationMode,
            invoice.CurrencyDecimalPlacesSnapshot,
            invoice.ExchangeRate,
            invoice.BaseCurrencyDecimalPlacesSnapshot);

        var snapshot = prepared.OpticalSnapshot is null
            ? null
            : SalesInvoiceOpticalSnapshotFactory.Create(line.Id, prepared.OpticalSnapshot);

        return new PreparedInvoiceLine(line, snapshot);
    }

    private sealed record PreparedInvoiceLine(
        SalesInvoiceLine Line,
        SalesInvoiceLinePrescriptionSnapshot? Snapshot);

    private static void EnsureCustomer(Customer customer, SalesPaymentPlan paymentPlan)
    {
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");

        if (paymentPlan == SalesPaymentPlan.AccountCredit && (string.IsNullOrWhiteSpace(customer.CustomerCode) || !customer.IsCreditAllowed))
            throw new ConflictException(SalesErrorCodes.CreditNotAllowed, "البيع الآجل غير مسموح لهذا العميل.");
    }
}
