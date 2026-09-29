using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesStockReservationService
{
    Task<CustomerOrderStatus> ReserveForOrderAsync(CustomerOrder order, CancellationToken cancellationToken = default);
    Task EnsureReservationsForInvoiceAsync(SalesInvoice invoice, CancellationToken cancellationToken = default);
    Task ReleaseOrderReservationsAsync(Guid orderId, DateTimeOffset releasedAtUtc, CancellationToken cancellationToken = default);
    Task ReleaseInvoiceReservationsAsync(Guid invoiceId, DateTimeOffset releasedAtUtc, CancellationToken cancellationToken = default);
    Task ValidateInvoiceReservationsAsync(SalesInvoice invoice, CancellationToken cancellationToken = default);
}
