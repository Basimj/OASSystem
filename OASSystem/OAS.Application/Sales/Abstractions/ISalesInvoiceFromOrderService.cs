using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesInvoiceFromOrderService
{
    Task<SalesInvoice> CreateAsync(
        CustomerOrder order,
        DateOnly invoiceDate,
        DateOnly postingDate,
        string? invoiceCode,
        string? description,
        CancellationToken cancellationToken = default);
}
