using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
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
using OAS.Domain.Sales.Rules;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed class UpdateCustomerOrderCommandHandler(
    ICustomerOrderAggregateRepository repository,
    IRepository<CustomerOrderLine, Guid> lineRepository,
    IRepository<CustomerOrderLineOpticalSnapshot, Guid> snapshotRepository,
    IReadRepository<Customer, Guid> customers,
    IExchangeRateResolver rates,
    ICustomerOrderOpticalService optical)
    : IRequestHandler<UpdateCustomerOrderCommand, CustomerOrder>
{
    public async Task<CustomerOrder> Handle(UpdateCustomerOrderCommand request, CancellationToken ct)
    {
        var order = await repository.GetAggregateAsync(request.Id, true, ct)
                    ?? throw new NotFoundException(nameof(CustomerOrder), request.Id);

        SalesConcurrency.Ensure(request.Data.RowVersion, order.RowVersion, "طلب العميل");
        if (request.Data.Lines.Count == 0)
            throw new ConflictException("sales_order_lines_required", "يجب أن يحتوي الطلب على سطر واحد على الأقل.");

        var customer = await customers.GetByIdAsync(request.Data.CustomerId, ct)
                       ?? throw new NotFoundException(nameof(Customer), request.Data.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");

        var term = (SalesPaymentTermType)(byte)request.Data.PaymentTermType;
        if (term == SalesPaymentTermType.Credit && !customer.IsCreditAllowed)
            throw new ConflictException(SalesErrorCodes.CreditNotAllowed, "البيع الآجل غير مسموح لهذا العميل.");

        var previousExchangeRate = order.ExchangeRate;
        var currencyContextChanged = order.CurrencyId != request.Data.CurrencyId || order.OrderDate != request.Data.OrderDate;

        order.UpdateHeader(
            customer.Id,
            request.Data.PrescriptionRevisionId,
            request.Data.OrderDate,
            request.Data.RequiredDate,
            (TaxCalculationMode)(byte)request.Data.TaxCalculationMode,
            term,
            customer.PaymentTermDays,
            request.Data.Notes);

        if (currencyContextChanged)
        {
            var rate = await rates.ResolveAsync(
                request.Data.CurrencyId,
                request.Data.OrderDate,
                ExchangeRateType.Accounting,
                cancellationToken: ct);
            order.ChangeCurrency(
                rate.CurrencyId, rate.CurrencyCode, rate.CurrencySymbol, rate.CurrencyDecimalPlaces,
                rate.Rate, rate.RateDate, rate.RateType, rate.Source);
        }

        await SynchronizeLinesAsync(order, request.Data.Lines, previousExchangeRate, currencyContextChanged, ct);
        return order;
    }

    private async Task SynchronizeLinesAsync(
        CustomerOrder order,
        IReadOnlyList<CustomerOrderLineRequest> requested,
        decimal previousExchangeRate,
        bool currencyContextChanged,
        CancellationToken ct)
    {
        var requestIds = requested.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        foreach (var old in order.Lines.Where(x => !requestIds.Contains(x.Id)).ToList())
        {
            await DeleteSnapshotsAsync(old.Id, ct);
            order.RemoveLine(old.Id);
            lineRepository.Delete(old);
        }

        var nextLineNumber = order.Lines.Count == 0 ? 1 : order.Lines.Max(x => x.LineNumber) + 1;
        foreach (var req in requested)
        {
            var existing = req.Id.HasValue ? order.Lines.SingleOrDefault(x => x.Id == req.Id.Value) : null;
            if (existing is not null && !string.IsNullOrWhiteSpace(req.RowVersion))
                SalesConcurrency.Ensure(req.RowVersion, existing.RowVersion, "سطر الطلب");

            var lineType = (SalesLineType)(byte)req.LineType;
            var requestEye = req.PrescriptionEye.HasValue ? (EyeSide?)(byte)req.PrescriptionEye.Value : null;
            var prepared = await optical.PrepareAsync(
                lineType,
                req.ProductVariantId,
                req.WarehouseId,
                req.Description,
                req.PrescriptionRevisionId,
                requestEye,
                order.PrescriptionRevisionId,
                req.OpticalSnapshot,
                ct);

            if (existing is not null && IsSameStructure(existing, req, prepared))
            {
                var existingBaseUnitPrice = existing.BaseUnitPrice;
                var existingActualUnitPrice = existing.ProductVariantId.HasValue ? existing.ActualUnitPrice : req.ActualUnitPrice;
                if (currencyContextChanged && existing.ProductVariantId.HasValue)
                {
                    existingBaseUnitPrice = SalesPricingCalculator.ConvertBetweenCurrencies(
                        existing.BaseUnitPrice, previousExchangeRate, order.ExchangeRate, order.CurrencyDecimalPlacesSnapshot);
                    existingActualUnitPrice = SalesPricingCalculator.ConvertBetweenCurrencies(
                        existing.ActualUnitPrice, previousExchangeRate, order.ExchangeRate, order.CurrencyDecimalPlacesSnapshot);
                }

                order.UpdateLinePricing(
                    existing.Id,
                    req.Quantity,
                    existingBaseUnitPrice,
                    existingActualUnitPrice,
                    (SalesDiscountType)(byte)req.DiscountType,
                    req.DiscountValue,
                    req.TaxRate);
                await SynchronizeSnapshotAsync(order, existing.Id, prepared.OpticalSnapshot, ct);
                continue;
            }

            if (existing is not null)
            {
                await DeleteSnapshotsAsync(existing.Id, ct);
                order.RemoveLine(existing.Id);
                lineRepository.Delete(existing);
            }

            var resolved = prepared.Resolution;
            var standardUnitPrice = resolved.ProductVariantId.HasValue
                ? SalesPricingCalculator.ConvertFromBase(resolved.BaseUnitPrice, order.ExchangeRate, order.CurrencyDecimalPlacesSnapshot)
                : resolved.BaseUnitPrice;
            var resolvedActualUnitPrice = resolved.ProductVariantId.HasValue ? standardUnitPrice : req.ActualUnitPrice;
            var newLineId = Guid.NewGuid();
            var newLine = CustomerOrderLine.Create(
                newLineId,
                order.Id,
                nextLineNumber++,
                req.GroupId,
                lineType,
                resolved.ProductVariantId,
                resolved.WarehouseId,
                resolved.Description,
                req.Quantity,
                standardUnitPrice,
                resolvedActualUnitPrice,
                (SalesDiscountType)(byte)req.DiscountType,
                req.DiscountValue,
                req.TaxRate,
                prepared.PrescriptionRevisionId,
                prepared.PrescriptionEye,
                req.RequiresProduction,
                req.Notes,
                order.TaxCalculationMode,
                order.CurrencyDecimalPlacesSnapshot);

            order.AddLine(newLine);
            await lineRepository.AddAsync(newLine, ct);
            if (prepared.OpticalSnapshot is not null)
                await snapshotRepository.AddAsync(CustomerOrderOpticalSnapshotFactory.Create(newLineId, prepared.OpticalSnapshot), ct);
        }
    }

    private async Task SynchronizeSnapshotAsync(
        CustomerOrder order,
        Guid lineId,
        OpticalSnapshotDraft? draft,
        CancellationToken ct)
    {
        var existing = (await snapshotRepository.ListAsync(
            new Specification<CustomerOrderLineOpticalSnapshot>().Where(x => x.CustomerOrderLineId == lineId && x.IsActive).Tracking(), ct))
            .SingleOrDefault();

        if (draft is null)
        {
            if (existing is not null)
                existing.DeactivateWhileDraft(order.Status);
            return;
        }

        if (existing is null)
        {
            await snapshotRepository.AddAsync(CustomerOrderOpticalSnapshotFactory.Create(lineId, draft), ct);
            return;
        }

        existing.UpdateWhileDraft(
            order.Status,
            draft.MeasurementSource,
            draft.PrescriptionRevisionId,
            draft.Eye,
            draft.SPH,
            draft.CYL,
            draft.Axis,
            draft.ADD,
            draft.Prism,
            draft.PrismBase,
            draft.PD,
            draft.MonocularPD,
            draft.VA,
            draft.FittingHeight,
            draft.LensTypeSnapshot,
            draft.MaterialSnapshot,
            draft.CoatingSnapshot,
            draft.RefractiveIndexSnapshot);
    }

    private async Task DeleteSnapshotsAsync(Guid lineId, CancellationToken ct)
    {
        var snapshots = await snapshotRepository.ListAsync(
            new Specification<CustomerOrderLineOpticalSnapshot>().Where(x => x.CustomerOrderLineId == lineId).Tracking(), ct);
        if (snapshots.Count > 0)
            snapshotRepository.DeleteRange(snapshots);
    }

    private static bool IsSameStructure(
        CustomerOrderLine line,
        CustomerOrderLineRequest req,
        PreparedCustomerOrderLine prepared)
    {
        var resolved = prepared.Resolution;
        return line.GroupId == req.GroupId &&
               line.LineType == (SalesLineType)(byte)req.LineType &&
               line.ProductVariantId == resolved.ProductVariantId &&
               line.WarehouseId == resolved.WarehouseId &&
               line.PrescriptionRevisionId == prepared.PrescriptionRevisionId &&
               line.PrescriptionEye == prepared.PrescriptionEye &&
               line.RequiresProduction == req.RequiresProduction &&
               line.DescriptionSnapshot == resolved.Description &&
               line.Notes == (string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim());
    }
}
