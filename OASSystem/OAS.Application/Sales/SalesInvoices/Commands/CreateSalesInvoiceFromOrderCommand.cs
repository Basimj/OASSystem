using OAS.Application.Abstractions.Messaging;using OAS.Application.Sales.Authorization;using OAS.Contracts.Sales.SalesInvoices;
namespace OAS.Application.Sales.SalesInvoices.Commands;
public sealed record CreateSalesInvoiceFromOrderCommand(Guid OrderId,CreateSalesInvoiceFromOrderRequest Request):ICommand<SalesInvoiceDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[SalesPermissions.Invoices.Create];}
