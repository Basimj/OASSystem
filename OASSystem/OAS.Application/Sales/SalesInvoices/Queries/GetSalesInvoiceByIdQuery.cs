using OAS.Application.Abstractions.Messaging;using OAS.Application.Sales.Authorization;using OAS.Contracts.Sales.SalesInvoices;
namespace OAS.Application.Sales.SalesInvoices.Queries;
public sealed record GetSalesInvoiceByIdQuery(Guid Id):IQuery<SalesInvoiceDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[SalesPermissions.Invoices.View];}
