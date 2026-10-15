using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.SalesInvoices;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed record CollectSalesInvoicePaymentCommand(
    Guid InvoiceId,
    CollectSalesInvoicePaymentRequest Request)
    : ICommand<CollectSalesInvoicePaymentResultDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [SalesPermissions.Invoices.Post];
}
