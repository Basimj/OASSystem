using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed class UpdateCustomerOrderCommandHandler(
    ICustomerOrderAggregateRepository repository,
    IRepository<CustomerOrderLine, Guid> lineRepository,
    IReadRepository<Customer, Guid> customers,
    IExchangeRateResolver rates,
    ISalesLineResolver lineResolver,
    ISalesPrescriptionValidator prescriptionValidator)
    : IRequestHandler<UpdateCustomerOrderCommand, CustomerOrder>
{
    public async Task<CustomerOrder> Handle(
        UpdateCustomerOrderCommand request,
        CancellationToken ct)
    {
        var order = await repository.GetAggregateAsync(
                        request.Id,
                        true,
                        ct)
                    ?? throw new NotFoundException(
                        nameof(CustomerOrder),
                        request.Id);

        SalesConcurrency.Ensure(
            request.Data.RowVersion,
            order.RowVersion,
            "طلب العميل");

        if (request.Data.Lines.Count == 0)
        {
            throw new ConflictException(
                "sales_order_lines_required",
                "يجب أن يحتوي الطلب على سطر واحد على الأقل.");
        }

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

        var term = (SalesPaymentTermType)(byte)request.Data.PaymentTermType;

        if (term == SalesPaymentTermType.Credit &&
            !customer.IsCreditAllowed)
        {
            throw new ConflictException(
                SalesErrorCodes.CreditNotAllowed,
                "البيع الآجل غير مسموح لهذا العميل.");
        }

        order.UpdateHeader(
            customer.Id,
            request.Data.PrescriptionRevisionId,
            request.Data.OrderDate,
            request.Data.RequiredDate,
            (TaxCalculationMode)(byte)request.Data.TaxCalculationMode,
            term,
            customer.PaymentTermDays,
            request.Data.Notes);

        if (order.CurrencyId != request.Data.CurrencyId)
        {
            var rate = await rates.ResolveAsync(
                request.Data.CurrencyId,
                request.Data.OrderDate,
                ExchangeRateType.Accounting,
                cancellationToken: ct);

            order.ChangeCurrency(
                rate.CurrencyId,
                rate.CurrencyCode,
                rate.CurrencySymbol,
                rate.CurrencyDecimalPlaces,
                rate.Rate,
                rate.RateDate,
                rate.RateType,
                rate.Source);
        }

        await SynchronizeLinesAsync(
            order,
            request.Data.Lines,
            lineRepository,
            ct);

        return order;
    }

    private async Task SynchronizeLinesAsync(
        CustomerOrder order,
        IReadOnlyList<CustomerOrderLineRequest> requested,
        IRepository<CustomerOrderLine, Guid> lineRepository,
        CancellationToken ct)
    {
        var requestIds = requested
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToHashSet();

        foreach (var old in order.Lines
                     .Where(x => !requestIds.Contains(x.Id))
                     .ToList())
        {
            order.RemoveLine(old.Id);
            lineRepository.Delete(old);
        }

        var nextLineNumber = order.Lines.Count == 0
            ? 1
            : order.Lines.Max(x => x.LineNumber) + 1;

        foreach (var req in requested)
        {
            var existing = req.Id.HasValue
                ? order.Lines.SingleOrDefault(
                    x => x.Id == req.Id.Value)
                : null;

            if (existing is not null &&
                IsSameStructure(existing, req, order))
            {
                if (!string.IsNullOrWhiteSpace(req.RowVersion))
                {
                    SalesConcurrency.Ensure(
                        req.RowVersion,
                        existing.RowVersion,
                        "سطر الطلب");
                }

                // إذا كان السطر مرتبطًا بمنتج،
                // السعر لا يؤخذ من المستخدم.
                //
                // البنود غير المرتبطة بمنتج مثل Service
                // يمكن أن تستقبل سعرًا يدويًا.
                var existingActualUnitPrice =
                    existing.ProductVariantId.HasValue
                        ? existing.BaseUnitPrice
                        : req.ActualUnitPrice;

                order.UpdateLinePricing(
                    existing.Id,
                    req.Quantity,
                    existing.BaseUnitPrice,
                    existingActualUnitPrice,
                    (SalesDiscountType)(byte)req.DiscountType,
                    req.DiscountValue,
                    req.TaxRate);

                continue;
            }

            if (existing is not null)
            {
                order.RemoveLine(existing.Id);
                lineRepository.Delete(existing);
            }

            var lineType =
                (SalesLineType)(byte)req.LineType;

            var resolved = await lineResolver.ResolveAsync(
                lineType,
                req.ProductVariantId,
                req.WarehouseId,
                req.Description,
                ct);

            var eye = req.PrescriptionEye.HasValue
                ? (EyeSide?)(byte)req.PrescriptionEye.Value
                : null;

            // Revision الموجودة في رأس الطلب
            // تستخدم فقط إذا كان السطر يحتاج وصفة.
            var prescriptionRevision =
                req.PrescriptionRevisionId;

            if (resolved.PrescriptionRequired &&
                !prescriptionRevision.HasValue)
            {
                prescriptionRevision =
                    order.PrescriptionRevisionId;
            }

            await prescriptionValidator.ValidateLineAsync(
                prescriptionRevision,
                eye,
                resolved.PrescriptionRequired,
                resolved.OpticalPolicy,
                ct);

            // المنتج:
            // السعر يأتي من ProductVariant.SellingPrice
            // عبر SalesLineResolver.
            //
            // الخدمة/البند بدون منتج:
            // يسمح بالسعر المرسل من المستخدم.
            var resolvedActualUnitPrice =
                resolved.ProductVariantId.HasValue
                    ? resolved.BaseUnitPrice
                    : req.ActualUnitPrice;

            var newLine = CustomerOrderLine.Create(
                Guid.NewGuid(),
                order.Id,
                nextLineNumber++,
                req.GroupId,
                lineType,
                resolved.ProductVariantId,
                resolved.WarehouseId,
                resolved.Description,
                req.Quantity,
                resolved.BaseUnitPrice,
                resolvedActualUnitPrice,
                (SalesDiscountType)(byte)req.DiscountType,
                req.DiscountValue,
                req.TaxRate,
                prescriptionRevision,
                eye,
                req.RequiresProduction,
                req.Notes,
                order.TaxCalculationMode,
                order.CurrencyDecimalPlacesSnapshot);

            order.AddLine(newLine);

            await lineRepository.AddAsync(
                newLine,
                ct);
        }
    }

    private static bool IsSameStructure(
        CustomerOrderLine line,
        CustomerOrderLineRequest req,
        CustomerOrder order)
    {
        var eye = req.PrescriptionEye.HasValue
            ? (EyeSide?)(byte)req.PrescriptionEye.Value
            : null;

        // إذا كان السطر نفسه يستخدم وصفة،
        // يمكن أن يرث Revision رأس الطلب.
        //
        // أما Frame / Accessory / Service
        // فلا نلصق بها Revision رأس الطلب.
        var lineUsesPrescription =
            line.PrescriptionRevisionId.HasValue ||
            line.PrescriptionEye.HasValue;

        var revision =
            req.PrescriptionRevisionId;

        if (lineUsesPrescription &&
            !revision.HasValue)
        {
            revision =
                order.PrescriptionRevisionId;
        }

        return
            line.GroupId == req.GroupId &&
            line.LineType ==
                (SalesLineType)(byte)req.LineType &&
            line.ProductVariantId ==
                req.ProductVariantId &&
            line.WarehouseId ==
                req.WarehouseId &&
            line.PrescriptionRevisionId ==
                revision &&
            line.PrescriptionEye ==
                eye &&
            line.RequiresProduction ==
                req.RequiresProduction &&
            line.DescriptionSnapshot ==
                (req.Description?.Trim()
                 ?? line.DescriptionSnapshot) &&
            line.Notes ==
                (string.IsNullOrWhiteSpace(req.Notes)
                    ? null
                    : req.Notes.Trim());
    }
}