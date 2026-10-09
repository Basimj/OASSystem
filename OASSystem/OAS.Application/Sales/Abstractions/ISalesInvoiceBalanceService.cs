using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public sealed record SalesInvoiceBalance(
    decimal InvoiceAmount,
    decimal ReturnedAmount,
    decimal NetInvoiceAmount,
    decimal AllocatedAmount,
    decimal OutstandingAmount,
    decimal InvoiceBaseAmount,
    decimal ReturnedBaseAmount,
    decimal NetInvoiceBaseAmount,
    decimal AllocatedBaseAmount,
    decimal OutstandingBaseAmount);

public interface ISalesInvoiceBalanceService
{
    Task<SalesInvoiceBalance> GetAsync(
        SalesInvoice invoice,
        Guid? excludingAllocationId = null,
        CancellationToken cancellationToken = default);
}
