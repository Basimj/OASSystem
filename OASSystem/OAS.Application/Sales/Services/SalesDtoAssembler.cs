using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Application.Sales.Common;

namespace OAS.Application.Sales.Services;

public sealed class SalesDtoAssembler(
    IReadRepository<Customer, Guid> customers,
    IReadRepository<PaymentAllocation, Guid> allocations,
    IReadRepository<CustomerOrder, Guid> orders)
{
    public async Task<PrescriptionDto> PrescriptionAsync(Prescription entity, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(entity.CustomerId, ct);
        return SalesContractMapping.Prescription(entity, customer?.CustomerCode, customer?.NameAr);
    }

    public async Task<CustomerOrderDto> OrderAsync(CustomerOrder entity, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(entity.CustomerId, ct);
        return SalesContractMapping.Order(entity, customer?.CustomerCode, customer?.NameAr);
    }

    public async Task<SalesInvoicePaymentSummaryDto> PaymentSummaryAsync(SalesInvoice entity, CancellationToken ct = default)
    {
        var spec = new Specification<PaymentAllocation>()
            .Where(x => x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice && x.TargetDocumentId == entity.Id);
        var invoiceAllocations = await allocations.ListAsync(spec, ct);

        // Allocations can originate in currencies different from the invoice currency.
        // Sum them in base currency first, then translate the paid amount back to the
        // invoice currency using the invoice's immutable exchange-rate snapshot.
        var paidBase = invoiceAllocations.Sum(GetBaseAllocatedAmount);
        var paid = entity.ExchangeRate <= 0m
            ? 0m
            : Math.Round(paidBase / entity.ExchangeRate, entity.CurrencyDecimalPlacesSnapshot, MidpointRounding.AwayFromZero);
        paid = Math.Min(entity.TotalAmount, Math.Max(0m, paid));

        return new SalesInvoicePaymentSummaryDto(
            entity.Id,
            entity.TotalAmount,
            paid,
            Math.Max(0m, entity.TotalAmount - paid));
    }

    private static decimal GetBaseAllocatedAmount(PaymentAllocation allocation)
    {
        if (allocation.BaseAllocatedAmount.HasValue)
            return allocation.BaseAllocatedAmount.Value;
        if (allocation.ExchangeRate.HasValue)
            return Math.Round(allocation.AllocatedAmount * allocation.ExchangeRate.Value, 4, MidpointRounding.AwayFromZero);
        return allocation.AllocatedAmount;
    }

    public async Task<SalesInvoiceDto> InvoiceAsync(SalesInvoice entity, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(entity.CustomerId, ct);
        string? orderCode = null;
        if (entity.CustomerOrderId.HasValue)
            orderCode = (await orders.GetByIdAsync(entity.CustomerOrderId.Value, ct))?.OrderCode;
        var payment = await PaymentSummaryAsync(entity, ct);
        return SalesContractMapping.Invoice(entity, payment, customer?.CustomerCode, customer?.NameAr, orderCode);
    }
}
