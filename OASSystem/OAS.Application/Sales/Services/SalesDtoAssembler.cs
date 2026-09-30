using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Services;

public sealed class SalesDtoAssembler(
    IReadRepository<Customer, Guid> customers,
    IReadRepository<PaymentAllocation, Guid> allocations,
    IReadRepository<CustomerOrder, Guid> orders,
    IReadRepository<PrescriptionRevision, Guid> revisions,
    IReadRepository<Prescription, Guid> prescriptions,
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<Product, Guid> products,
    IReadRepository<Warehouse, Guid> warehouses)
{
    public async Task<PrescriptionDto> PrescriptionAsync(Prescription entity, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(entity.CustomerId, ct);
        return SalesContractMapping.Prescription(entity, customer?.CustomerCode, customer?.NameAr);
    }

    public async Task<CustomerOrderDto> OrderAsync(CustomerOrder entity, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(entity.CustomerId, ct);
        var prescription = await ResolvePrescriptionAsync(entity.PrescriptionRevisionId, ct);

        var lines = new List<CustomerOrderLineDto>(entity.Lines.Count);
        foreach (var line in entity.Lines.OrderBy(x => x.LineNumber))
            lines.Add(await OrderLineAsync(line, ct));

        return SalesContractMapping.Order(entity, customer?.CustomerCode, customer?.NameAr) with
        {
            Lines = lines,
            PrescriptionCode = prescription.Code,
            PrescriptionRevisionNumber = prescription.RevisionNumber
        };
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
        var prescription = await ResolvePrescriptionAsync(entity.PrescriptionRevisionId, ct);

        var lines = new List<SalesInvoiceLineDto>(entity.Lines.Count);
        foreach (var line in entity.Lines.OrderBy(x => x.LineNumber))
            lines.Add(await InvoiceLineAsync(line, ct));

        return SalesContractMapping.Invoice(
            entity,
            payment,
            customer?.CustomerCode,
            customer?.NameAr,
            orderCode) with
        {
            Lines = lines,
            PrescriptionCode = prescription.Code,
            PrescriptionRevisionNumber = prescription.RevisionNumber
        };
    }

    private async Task<CustomerOrderLineDto> OrderLineAsync(CustomerOrderLine line, CancellationToken ct)
    {
        var product = await ResolveProductAsync(line.ProductVariantId, ct);
        var warehouse = await ResolveWarehouseAsync(line.WarehouseId, ct);
        var prescription = await ResolvePrescriptionAsync(line.PrescriptionRevisionId, ct);

        return SalesContractMapping.OrderLine(line) with
        {
            ProductCode = product.Code,
            ProductName = product.Name,
            WarehouseCode = warehouse.Code,
            WarehouseName = warehouse.Name,
            PrescriptionCode = prescription.Code,
            PrescriptionRevisionNumber = prescription.RevisionNumber
        };
    }

    private async Task<SalesInvoiceLineDto> InvoiceLineAsync(SalesInvoiceLine line, CancellationToken ct)
    {
        var warehouse = await ResolveWarehouseAsync(line.WarehouseId, ct);
        var prescription = await ResolvePrescriptionAsync(line.PrescriptionRevisionId, ct);

        return SalesContractMapping.InvoiceLine(line) with
        {
            WarehouseCode = warehouse.Code,
            WarehouseName = warehouse.Name,
            PrescriptionCode = prescription.Code,
            PrescriptionRevisionNumber = prescription.RevisionNumber
        };
    }

    private async Task<(string? Code, string? Name)> ResolveProductAsync(Guid? variantId, CancellationToken ct)
    {
        if (!variantId.HasValue)
            return (null, null);

        var variant = await variants.GetByIdAsync(variantId.Value, ct);
        if (variant is null)
            return (null, null);

        var product = await products.GetByIdAsync(variant.ProductId, ct);
        if (product is null)
            return (null, variant.VariantName);

        var name = string.IsNullOrWhiteSpace(variant.VariantName)
            ? product.NameAr
            : $"{product.NameAr} - {variant.VariantName}";

        return (product.ProductCode, name);
    }

    private async Task<(string? Code, string? Name)> ResolveWarehouseAsync(Guid? warehouseId, CancellationToken ct)
    {
        if (!warehouseId.HasValue)
            return (null, null);

        var warehouse = await warehouses.GetByIdAsync(warehouseId.Value, ct);
        return warehouse is null
            ? (null, null)
            : (warehouse.Code, warehouse.NameAr);
    }

    private async Task<(string? Code, int? RevisionNumber)> ResolvePrescriptionAsync(Guid? revisionId, CancellationToken ct)
    {
        if (!revisionId.HasValue)
            return (null, null);

        var revision = await revisions.GetByIdAsync(revisionId.Value, ct);
        if (revision is null)
            return (null, null);

        var prescription = await prescriptions.GetByIdAsync(revision.PrescriptionId, ct);
        return (prescription?.PrescriptionCode, revision.RevisionNumber);
    }
}
