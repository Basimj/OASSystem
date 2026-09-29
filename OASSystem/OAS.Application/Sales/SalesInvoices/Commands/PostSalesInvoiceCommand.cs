using OAS.Application.Abstractions.Messaging;using OAS.Application.Sales.Authorization;using OAS.Contracts.Sales.SalesInvoices;
namespace OAS.Application.Sales.SalesInvoices.Commands;
public sealed record PostSalesInvoiceCommand(Guid InvoiceId,PostSalesInvoiceRequest Request):ICommand<SalesInvoicePostingResultDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[SalesPermissions.Invoices.Post];}
