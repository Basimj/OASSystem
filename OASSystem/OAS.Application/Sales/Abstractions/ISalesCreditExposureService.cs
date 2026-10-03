using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public sealed record SalesCreditAssessment(
    bool IsAllowed,
    string? ErrorCode,
    string? Message,
    decimal ExposureBeforeCurrent,
    decimal InvoiceBaseAmount,
    decimal CreditLimit,
    decimal NewExposure);

public interface ISalesCreditExposureService
{
    Task<SalesCreditAssessment> EvaluateAsync(
        Customer customer,
        SalesInvoice invoice,
        CancellationToken cancellationToken = default);

    Task ValidateAsync(Customer customer, SalesInvoice invoice, CancellationToken cancellationToken = default);
    Task<decimal> CalculateExposureBeforeCurrentAsync(Guid customerId, Guid? currentInvoiceId, CancellationToken cancellationToken = default);
}
