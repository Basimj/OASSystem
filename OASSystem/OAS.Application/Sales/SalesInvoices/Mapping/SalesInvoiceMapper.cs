using OAS.Application.CRUD.Mapping;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.SalesInvoices.Mapping;

public sealed class SalesInvoiceMapper : ICrudMapper<SalesInvoice, Guid, SalesInvoiceDto, CreateSalesInvoiceRequest, UpdateSalesInvoiceRequest>
{
    public SalesInvoice Create(CreateSalesInvoiceRequest source) =>
        throw new NotSupportedException("SalesInvoice creation requires customer/currency/product resolution and must use CreateSalesInvoiceCommand.");

    public void Update(UpdateSalesInvoiceRequest source, SalesInvoice destination) =>
        throw new NotSupportedException("SalesInvoice update requires aggregate line synchronization and must use UpdateSalesInvoiceCommand.");

    public SalesInvoiceDto ToRead(SalesInvoice source) =>
        SalesContractMapping.Invoice(source, new SalesInvoicePaymentSummaryDto(source.Id, source.TotalAmount, 0m, source.TotalAmount));
}
