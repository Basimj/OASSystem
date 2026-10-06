using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesInvoiceConfirmationService
{
    Task ConfirmAsync(SalesInvoice invoice, CancellationToken cancellationToken = default);
}
