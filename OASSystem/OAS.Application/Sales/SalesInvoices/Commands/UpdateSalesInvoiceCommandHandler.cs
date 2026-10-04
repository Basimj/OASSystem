using MediatR;
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
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class UpdateSalesInvoiceCommandHandler(
    ISalesInvoiceAggregateRepository repository,
    IRepository<SalesInvoiceLine, Guid> lineRepository,
    IRepository<SalesPriceOverride, Guid> priceOverrides,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<Currency, Guid> currencies,
    IReadRepository<Employee, Guid> employees,
    IExchangeRateResolver rates,
    ISalesLineResolver lineResolver,
    ISalesPrescriptionValidator prescriptionValidator)
    : IRequestHandler<UpdateSalesInvoiceCommand, SalesInvoice>
{
    public async Task<SalesInvoice> Handle(
        UpdateSalesInvoiceCommand request,
        CancellationToken ct)
    {
        var invoice = await repository.GetAggregateAsync(
                          request.Id,
                          true,
                          ct)
                      ?? throw new NotFoundException(
                          nameof(SalesInvoice),
                          request.Id);

        SalesConcurrency.Ensure(
            request.Data.RowVersion,
            invoice.RowVersion,
            "فاتورة المبيعات");

        var customer = await customers.GetByIdAsync(
                           request.Data.CustomerId,
                           ct)
                       ?? throw new NotFoundException(
                           nameof(Customer),
                           request.Data.CustomerId);

        if (!customer.IsActive)
        {
            throw new ConflictException(
                SalesErrorCodes.CustomerInactive,
                "العميل غير فعال.");
        }

        var term =
            (SalesPaymentTermType)(byte)request.Data.PaymentTermType;

        if (term == SalesPaymentTermType.Credit &&
            !customer.IsCreditAllowed)
        {
            throw new ConflictException(
                SalesErrorCodes.CreditNotAllowed,
                "البيع الآجل غير مسموح لهذا العميل.");
        }

        var previousExchangeRate = invoice.ExchangeRate;
        var currencyContextChanged =
            invoice.CurrencyId != request.Data.CurrencyId ||
            invoice.InvoiceDate != request.Data.InvoiceDate;

        invoice.UpdateHeader(
            customer.Id,
            invoice.CustomerOrderId,
            request.Data.PrescriptionRevisionId,
            request.Data.InvoiceDate,
            request.Data.PostingDate,
            (TaxCalculationMode)(byte)request.Data.TaxCalculationMode,
            term,
            customer.PaymentTermDays,
            request.Data.Description);

        if (request.Data.SalesEmployeeId.HasValue)
        {
            var employee = await employees.GetByIdAsync(request.Data.SalesEmployeeId.Value, ct)
                ?? throw new NotFoundException(nameof(Employee), request.Data.SalesEmployeeId.Value);
            if (!employee.IsActive)
                throw new ConflictException("sales_employee_inactive", "موظف المبيعات غير فعال.");
            invoice.SetSalesEmployee(employee.Id);
        }
        else
        {
            invoice.SetSalesEmployee(null);
        }

        if (currencyContextChanged)
        {
            var rate = await rates.ResolveAsync(
                request.Data.CurrencyId,
                request.Data.InvoiceDate,
                ExchangeRateType.Accounting,
                cancellationToken: ct);

            var baseCurrency =
                await GetBaseAsync(ct);

            invoice.ChangeCurrency(
                rate.CurrencyId,
                rate.CurrencyCode,
                rate.CurrencySymbol,
                rate.CurrencyDecimalPlaces,
                rate.Rate,
                rate.RateDate,
                rate.RateType,
                rate.Source,
                baseCurrency.Id,
                baseCurrency.Code,
                baseCurrency.DecimalPlaces);
        }

        await SynchronizeLinesAsync(
            invoice,
            request.Data.Lines,
            previousExchangeRate,
            currencyContextChanged,
            ct);

        return invoice;
    }

    private async Task SynchronizeLinesAsync(
        SalesInvoice invoice,
        IReadOnlyList<SalesInvoiceLineRequest> requested,
        decimal previousExchangeRate,
        bool currencyContextChanged,
        CancellationToken ct)
    {
        if (requested.Count == 0)
        {
            throw new ConflictException(
                "sales_invoice_lines_required",
                "يجب أن تحتوي الفاتورة على سطر واحد على الأقل.");
        }

        var requestedIds = requested
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToHashSet();

        foreach (var old in invoice.Lines
                     .Where(x => !requestedIds.Contains(x.Id))
                     .ToList())
        {
            await EnsureLineCanBeRemovedAsync(
                old.Id,
                ct);

            invoice.RemoveLine(old.Id);

            lineRepository.Delete(old);
        }

        var nextLineNumber =
            invoice.Lines.Count == 0
                ? 1
                : invoice.Lines.Max(
                    x => x.LineNumber) + 1;

        foreach (var req in requested)
        {
            var existing = req.Id.HasValue
                ? invoice.Lines.SingleOrDefault(
                    x => x.Id == req.Id.Value)
                : null;

            if (existing is not null &&
                HasSameStructure(
                    existing,
                    req,
                    invoice))
            {
                if (!string.IsNullOrWhiteSpace(
                        req.RowVersion))
                {
                    SalesConcurrency.Ensure(
                        req.RowVersion,
                        existing.RowVersion,
                        "سطر الفاتورة");
                }

                // للأسطر المرتبطة بمنتج:
                // لا نأخذ السعر من Client.
                //
                // إذا كان هناك Price Override معتمد،
                // ActualUnitPrice المحفوظة هي السعر
                // الذي نعتمد عليه ونبقيه كما هو.
                //
                // البنود بدون ProductVariant فقط
                // يمكن تعديل السعر فيها مباشرة.
                var existingBaseUnitPrice = existing.BaseUnitPrice;
                var existingActualUnitPrice = existing.ProductVariantId.HasValue
                    ? existing.ActualUnitPrice
                    : req.ActualUnitPrice;

                if (currencyContextChanged && existing.ProductVariantId.HasValue)
                {
                    existingBaseUnitPrice = SalesPricingCalculator.ConvertBetweenCurrencies(
                        existing.BaseUnitPrice,
                        previousExchangeRate,
                        invoice.ExchangeRate,
                        invoice.CurrencyDecimalPlacesSnapshot);
                    existingActualUnitPrice = SalesPricingCalculator.ConvertBetweenCurrencies(
                        existing.ActualUnitPrice,
                        previousExchangeRate,
                        invoice.ExchangeRate,
                        invoice.CurrencyDecimalPlacesSnapshot);
                }

                await InvalidatePriceOverridesWhenPriceChangesAsync(
                    existing,
                    existingActualUnitPrice,
                    ct);

                invoice.UpdateLinePricing(
                    existing.Id,
                    req.Quantity,
                    existingBaseUnitPrice,
                    existingActualUnitPrice,
                    (SalesDiscountType)(byte)req.DiscountType,
                    req.DiscountValue,
                    req.TaxRate);

                continue;
            }

            if (existing is not null)
            {
                await EnsureLineCanBeRemovedAsync(
                    existing.Id,
                    ct);

                invoice.RemoveLine(
                    existing.Id);

                lineRepository.Delete(
                    existing);
            }

            var type =
                (SalesLineType)(byte)req.LineType;

            var resolved =
                await lineResolver.ResolveAsync(
                    type,
                    req.ProductVariantId,
                    req.WarehouseId,
                    req.Description,
                    ct);

            var revision =
                req.PrescriptionRevisionId ??
                invoice.PrescriptionRevisionId;

            var eye =
                req.PrescriptionEye.HasValue
                    ? (EyeSide?)(byte)req.PrescriptionEye.Value
                    : null;

            await prescriptionValidator.ValidateLineAsync(
                revision,
                eye,
                resolved.PrescriptionRequired,
                resolved.OpticalPolicy,
                ct);

            // المنتج يأخذ السعر المخزن مسبقًا.
            //
            // إذا لم يكن السطر مرتبطًا بمنتج
            // يسمح بالقيمة اليدوية مثل Service.
            var standardUnitPrice = resolved.ProductVariantId.HasValue
                ? SalesPricingCalculator.ConvertFromBase(
                    resolved.BaseUnitPrice,
                    invoice.ExchangeRate,
                    invoice.CurrencyDecimalPlacesSnapshot)
                : resolved.BaseUnitPrice;

            var resolvedActualUnitPrice =
                resolved.ProductVariantId.HasValue
                    ? standardUnitPrice
                    : req.ActualUnitPrice;

            var newLine =
                SalesInvoiceLine.Create(
                    Guid.NewGuid(),
                    invoice.Id,
                    nextLineNumber++,
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
                    resolvedActualUnitPrice,
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

            invoice.AddLine(newLine);

            await lineRepository.AddAsync(
                newLine,
                ct);
        }
    }

    private async Task InvalidatePriceOverridesWhenPriceChangesAsync(
        SalesInvoiceLine line,
        decimal newActualUnitPrice,
        CancellationToken ct)
    {
        if (line.ActualUnitPrice ==
            newActualUnitPrice)
        {
            return;
        }

        var spec =
            new Specification<SalesPriceOverride>()
                .Where(x =>
                    x.SalesInvoiceLineId == line.Id &&
                    x.IsActive &&
                    (
                        x.Status ==
                            SalesPriceOverrideStatus.Pending ||
                        x.Status ==
                            SalesPriceOverrideStatus.Approved
                    ))
                .Tracking();

        var activeOverrides =
            await priceOverrides.ListAsync(
                spec,
                ct);

        foreach (var priceOverride in activeOverrides)
        {
            // إذا كانت نفس القيمة التي سبق اعتمادها
            // فلا تعتبر تغيير سعر جديد.
            if (priceOverride.Status ==
                    SalesPriceOverrideStatus.Approved &&
                priceOverride.IsApprovedFor(
                    newActualUnitPrice))
            {
                continue;
            }

            priceOverride.Cancel();

            priceOverrides.Update(
                priceOverride);
        }
    }

    private async Task EnsureLineCanBeRemovedAsync(
        Guid lineId,
        CancellationToken ct)
    {
        var count =
            await priceOverrides.CountAsync(
                new Specification<SalesPriceOverride>()
                    .Where(x =>
                        x.SalesInvoiceLineId == lineId),
                ct);

        if (count > 0)
        {
            throw new ConflictException(
                "sales_invoice_line_has_price_override_history",
                "لا يمكن حذف أو استبدال سطر فاتورة لديه سجل تغيير سعر. احتفظ بالسطر أو ألغِ الفاتورة.");
        }
    }

    private static bool HasSameStructure(
        SalesInvoiceLine line,
        SalesInvoiceLineRequest request,
        SalesInvoice invoice)
    {
        var revision =
            request.PrescriptionRevisionId ??
            invoice.PrescriptionRevisionId;

        var eye =
            request.PrescriptionEye.HasValue
                ? (EyeSide?)(byte)request.PrescriptionEye.Value
                : null;

        return
            line.CustomerOrderLineId ==
                request.CustomerOrderLineId &&
            line.GroupId ==
                request.GroupId &&
            line.LineType ==
                (SalesLineType)(byte)request.LineType &&
            line.ProductVariantId ==
                request.ProductVariantId &&
            line.WarehouseId ==
                request.WarehouseId &&
            line.PrescriptionRevisionId ==
                revision &&
            line.PrescriptionEye ==
                eye &&
            line.RequiresProduction ==
                request.RequiresProduction &&
            line.Notes ==
                (string.IsNullOrWhiteSpace(
                    request.Notes)
                    ? null
                    : request.Notes.Trim());
    }

    private async Task<Currency> GetBaseAsync(
        CancellationToken ct)
    {
        var settingsEntity =
            await settings.GetByIdAsync(
                AccountingSettings.SingletonId,
                ct)
            ?? throw new ConflictException(
                "accounting_settings_required",
                "يجب إعداد المحاسبة والعملة الأساسية أولًا.");

        return await currencies.GetByIdAsync(
                   settingsEntity.BaseCurrencyId,
                   ct)
               ?? throw new ConflictException(
                   "base_currency_missing",
                   "العملة الأساسية غير موجودة.");
    }
}