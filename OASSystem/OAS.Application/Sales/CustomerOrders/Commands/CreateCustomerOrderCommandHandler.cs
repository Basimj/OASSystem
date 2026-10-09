using MediatR;
using OAS.Application.Abstractions.Numbering;
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
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed class CreateCustomerOrderCommandHandler(
    ICustomerOrderAggregateRepository repository,
    IRepository<CustomerOrderLineOpticalSnapshot, Guid> snapshots,
    IReadRepository<Customer, Guid> customers,
    IExchangeRateResolver rates,
    ICustomerOrderOpticalService optical,
    ISequenceNumberGenerator sequences) : IRequestHandler<CreateCustomerOrderCommand, CustomerOrder>
{
    public async Task<CustomerOrder> Handle(CreateCustomerOrderCommand request, CancellationToken ct)
    {
        var d = request.Data;
        var customer = await customers.GetByIdAsync(d.CustomerId, ct) ?? throw new NotFoundException(nameof(Customer), d.CustomerId);
        var paymentPlan = (SalesPaymentPlan)(byte)d.PaymentPlan;
        EnsureCustomer(customer, paymentPlan);
        if (d.Lines.Count == 0) throw new ConflictException("sales_order_lines_required", "يجب أن يحتوي الطلب على سطر واحد على الأقل.");

        var code = d.OrderCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            code = CustomerOrderCodeFormatter.Format(await sequences.NextAsync("CustomerOrderCodeSequence", ct), d.OrderDate);
        if (await repository.CountAsync(new Specification<CustomerOrder>().Where(x => x.OrderCode == code), ct) > 0)
            throw new ConflictException(SalesErrorCodes.DuplicateOrderCode, "كود طلب العميل مستخدم مسبقًا.");

        var rate = await rates.ResolveAsync(d.CurrencyId, d.OrderDate, ExchangeRateType.Accounting, cancellationToken: ct);
        var order = CustomerOrder.Create(
            Guid.NewGuid(), code, customer.Id, d.PrescriptionRevisionId, d.OrderDate, d.RequiredDate,
            rate.CurrencyId, rate.CurrencyCode, rate.CurrencySymbol, rate.CurrencyDecimalPlaces, rate.Rate,
            rate.RateDate, rate.RateType, rate.Source, (TaxCalculationMode)(byte)d.TaxCalculationMode,
            paymentPlan, paymentPlan == SalesPaymentPlan.AccountCredit ? customer.PaymentTermDays : 0, d.Notes);

        var lineNo = 1;
        foreach (var req in d.Lines)
        {
            var line = await BuildLineAsync(order, lineNo++, req, ct);
            order.AddLine(line);
        }
        await repository.AddAsync(order, ct);
        return order;
    }

    private async Task<CustomerOrderLine> BuildLineAsync(CustomerOrder order, int lineNo, CustomerOrderLineRequest req, CancellationToken ct)
    {
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
        var resolved = prepared.Resolution;

        var standardUnitPrice = resolved.ProductVariantId.HasValue
            ? SalesPricingCalculator.ConvertFromBase(
                resolved.BaseUnitPrice,
                order.ExchangeRate,
                order.CurrencyDecimalPlacesSnapshot)
            : resolved.BaseUnitPrice;
        var seedStandardUnitPrice = prepared.SeedResolution.ProductVariantId.HasValue
            ? SalesPricingCalculator.ConvertFromBase(
                prepared.SeedResolution.BaseUnitPrice,
                order.ExchangeRate,
                order.CurrencyDecimalPlacesSnapshot)
            : prepared.SeedResolution.BaseUnitPrice;
        var actualUnitPrice = lineType == SalesLineType.Lens
            ? SalesLensLinePolicy.ResolveCustomerOrderActualPrice(
                req.ActualUnitPrice,
                seedStandardUnitPrice,
                standardUnitPrice,
                order.CurrencyDecimalPlacesSnapshot)
            : req.ActualUnitPrice;
        var requiresProduction = SalesLensLinePolicy.ResolveRequiresProduction(
            lineType,
            resolved,
            req.GroupId,
            req.RequiresProduction);

        var lineId = Guid.NewGuid();
        var line = CustomerOrderLine.Create(
            lineId, order.Id, lineNo, req.GroupId, lineType, resolved.ProductVariantId, resolved.WarehouseId,
            resolved.Description, req.Quantity, standardUnitPrice, actualUnitPrice,
            (SalesDiscountType)(byte)req.DiscountType, req.DiscountValue, req.TaxRate,
            prepared.PrescriptionRevisionId, prepared.PrescriptionEye,
            requiresProduction, req.Notes, order.TaxCalculationMode, order.CurrencyDecimalPlacesSnapshot);

        if (prepared.OpticalSnapshot is not null)
            await snapshots.AddAsync(CustomerOrderOpticalSnapshotFactory.Create(lineId, prepared.OpticalSnapshot), ct);

        return line;
    }

    private static void EnsureCustomer(Customer customer, SalesPaymentPlan paymentPlan)
    {
        if (!customer.IsActive) throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");
        if (paymentPlan == SalesPaymentPlan.AccountCredit && (string.IsNullOrWhiteSpace(customer.CustomerCode) || !customer.IsCreditAllowed))
            throw new ConflictException(SalesErrorCodes.CreditNotAllowed, "البيع الآجل غير مسموح لهذا العميل.");
    }
}
