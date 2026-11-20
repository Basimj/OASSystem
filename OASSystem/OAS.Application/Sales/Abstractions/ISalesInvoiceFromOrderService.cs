using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesInvoiceFromOrderService
{
    Task<bool> HasUnpostedInvoiceAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<SalesInvoice> CreateDraftAsync(
        CustomerOrder order,
        DateOnly invoiceDate,
        DateOnly postingDate,
        string? invoiceCode,
        string? description,
        CancellationToken cancellationToken = default,
        Guid? salesEmployeeId = null);

    Task<SalesInvoice> CreateAsync(
        CustomerOrder order,
        DateOnly invoiceDate,
        DateOnly postingDate,
        string? invoiceCode,
        string? description,
        CancellationToken cancellationToken = default);
}
