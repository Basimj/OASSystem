using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesCreditExposureService
{
    Task ValidateAsync(Customer customer, SalesInvoice invoice, CancellationToken cancellationToken = default);
    Task<decimal> CalculateExposureBeforeCurrentAsync(Guid customerId, Guid? currentInvoiceId, CancellationToken cancellationToken = default);
}
