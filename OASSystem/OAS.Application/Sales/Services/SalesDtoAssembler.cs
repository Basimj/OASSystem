using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Services;

public sealed class SalesDtoAssembler(
    IReadRepository<Customer, Guid> customers,
    ISalesInvoiceBalanceService invoiceBalances,
    IReadRepository<JournalEntry, Guid> journals,
    IReadRepository<CustomerOrder, Guid> orders,
    IReadRepository<PrescriptionRevision, Guid> revisions,
    IReadRepository<Prescription, Guid> prescriptions,
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<Product, Guid> products,
    IReadRepository<ProductType, Guid> productTypes,
    IReadRepository<ProductCategory, Guid> categories,
    IReadRepository<Warehouse, Guid> warehouses,
    IReadRepository<CustomerOrderLineOpticalSnapshot, Guid> orderOpticalSnapshots,
    IReadRepository<Employee, Guid> employees)
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
        var balance = await invoiceBalances.GetAsync(entity, cancellationToken: ct);
        return new SalesInvoicePaymentSummaryDto(
            entity.Id,
            entity.TotalAmount,
            balance.AllocatedAmount,
            balance.OutstandingAmount,
            balance.ReturnedAmount);
    }

    public async Task<SalesInvoiceDto> InvoiceAsync(SalesInvoice entity, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(entity.CustomerId, ct);
        string? orderCode = null;
        if (entity.CustomerOrderId.HasValue)
            orderCode = (await orders.GetByIdAsync(entity.CustomerOrderId.Value, ct))?.OrderCode;

        var payment = await PaymentSummaryAsync(entity, ct);
        var prescription = await ResolvePrescriptionAsync(entity.PrescriptionRevisionId, ct);
        var salesEmployeeName = entity.SalesEmployeeId.HasValue
            ? (await employees.GetByIdAsync(entity.SalesEmployeeId.Value, ct))?.DisplayName
            : null;
        var journalNumber = entity.JournalEntryId.HasValue
            ? (await journals.GetByIdAsync(entity.JournalEntryId.Value, ct))?.JournalNumber
            : null;

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
            PrescriptionRevisionNumber = prescription.RevisionNumber,
            JournalEntryNumber = journalNumber,
            SalesEmployeeName = salesEmployeeName
        };
    }

    private async Task<CustomerOrderLineDto> OrderLineAsync(CustomerOrderLine line, CancellationToken ct)
    {
        var product = await ResolveProductAsync(line.ProductVariantId, ct);
        var warehouse = await ResolveWarehouseAsync(line.WarehouseId, ct);
        var prescription = await ResolvePrescriptionAsync(line.PrescriptionRevisionId, ct);
        var snapshot = (await orderOpticalSnapshots.ListAsync(
            new Specification<CustomerOrderLineOpticalSnapshot>().Where(x => x.CustomerOrderLineId == line.Id && x.IsActive), ct))
            .SingleOrDefault();

        return SalesContractMapping.OrderLine(line) with
        {
            ProductCode = product.Code,
            ProductName = product.Name,
            ProductCategoryId = product.CategoryId,
            ProductCategoryCode = product.CategoryCode,
            ProductCategoryName = product.CategoryName,
            ProductTypeId = product.ProductTypeId,
            ProductTypeCode = product.ProductTypeCode,
            ProductTypeName = product.ProductTypeName,
            ProductTypeSystemKey = product.ProductTypeSystemKey,
            ProductIsStockItem = product.IsStockItem,
            WarehouseCode = warehouse.Code,
            WarehouseName = warehouse.Name,
            PrescriptionCode = prescription.Code,
            PrescriptionRevisionNumber = prescription.RevisionNumber,
            OpticalSnapshot = snapshot is null ? null : new CustomerOrderLineOpticalSnapshotDto(
                snapshot.Id,
                snapshot.CustomerOrderLineId,
                (OAS.Contracts.Sales.Enums.OpticalMeasurementSource)(byte)snapshot.MeasurementSource,
                snapshot.PrescriptionRevisionId,
                (OAS.Contracts.Sales.Enums.EyeSide)(byte)snapshot.Eye,
                snapshot.SPH, snapshot.CYL, snapshot.Axis, snapshot.ADD, snapshot.Prism,
                snapshot.PrismBase.HasValue ? (OAS.Contracts.Sales.Enums.PrismBaseDirection?)(byte)snapshot.PrismBase.Value : null,
                snapshot.PD, snapshot.MonocularPD, snapshot.VA, snapshot.FittingHeight,
                snapshot.LensTypeSnapshot, snapshot.MaterialSnapshot, snapshot.CoatingSnapshot, snapshot.RefractiveIndexSnapshot,
                snapshot.IsActive, Convert.ToBase64String(snapshot.RowVersion),
                snapshot.CreatedAtUtc, snapshot.CreatedBy, snapshot.LastModifiedAtUtc, snapshot.LastModifiedBy)
        };
    }

    private async Task<SalesInvoiceLineDto> InvoiceLineAsync(SalesInvoiceLine line, CancellationToken ct)
    {
        var product = await ResolveProductAsync(line.ProductVariantId, ct);
        var warehouse = await ResolveWarehouseAsync(line.WarehouseId, ct);
        var prescription = await ResolvePrescriptionAsync(line.PrescriptionRevisionId, ct);

        return SalesContractMapping.InvoiceLine(line) with
        {
            ProductCategoryId = product.CategoryId,
            ProductCategoryCode = product.CategoryCode,
            ProductCategoryName = product.CategoryName,
            ProductTypeId = product.ProductTypeId,
            ProductTypeCode = product.ProductTypeCode,
            ProductTypeName = product.ProductTypeName,
            ProductTypeSystemKey = product.ProductTypeSystemKey,
            ProductIsStockItem = product.IsStockItem,
            ReturnedQuantity = line.ReturnedQuantity,
            WarehouseCode = warehouse.Code,
            WarehouseName = warehouse.Name,
            PrescriptionCode = prescription.Code,
            PrescriptionRevisionNumber = prescription.RevisionNumber
        };
    }

    private async Task<(
        string? Code,
        string? Name,
        Guid? CategoryId,
        string? CategoryCode,
        string? CategoryName,
        Guid? ProductTypeId,
        string? ProductTypeCode,
        string? ProductTypeName,
        string? ProductTypeSystemKey,
        bool? IsStockItem)> ResolveProductAsync(Guid? variantId, CancellationToken ct)
    {
        if (!variantId.HasValue)
            return (null, null, null, null, null, null, null, null, null, null);

        var variant = await variants.GetByIdAsync(variantId.Value, ct);
        if (variant is null)
            return (null, null, null, null, null, null, null, null, null, null);

        var product = await products.GetByIdAsync(variant.ProductId, ct);
        if (product is null)
            return (null, variant.VariantName, null, null, null, null, null, null, null, null);

        var category = await categories.GetByIdAsync(product.CategoryId, ct);
        var productType = await productTypes.GetByIdAsync(product.ProductTypeId, ct);
        var name = string.IsNullOrWhiteSpace(variant.VariantName)
            ? product.NameAr
            : $"{product.NameAr} - {variant.VariantName}";

        return (
            product.ProductCode,
            name,
            product.CategoryId,
            category?.Code,
            category?.NameAr,
            product.ProductTypeId,
            productType?.Code,
            productType?.NameAr,
            productType?.SystemKey,
            product.IsStockItem);
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
