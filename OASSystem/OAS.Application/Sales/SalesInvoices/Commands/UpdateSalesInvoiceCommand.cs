using OAS.Application.Abstractions.Messaging;using OAS.Application.CRUD.Commands;using OAS.Application.Sales.Authorization;using OAS.Contracts.Sales.SalesInvoices;using OAS.Domain.Sales.Entities;
namespace OAS.Application.Sales.SalesInvoices.Commands;
public sealed record UpdateSalesInvoiceCommand(Guid Id,UpdateSalesInvoiceRequest Data):UpdateEntityCommand<SalesInvoice,Guid,UpdateSalesInvoiceRequest>(Id,Data),IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[SalesPermissions.Invoices.Edit];}
