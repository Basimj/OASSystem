using OAS.Application.Abstractions.Messaging;using OAS.Application.Sales.Authorization;using OAS.Contracts.Sales.Common;
namespace OAS.Application.Sales.SalesInvoices.Queries;
public sealed record PreValidateSalesInvoicePostingQuery(Guid InvoiceId):IQuery<SalesPostingPreValidationDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[SalesPermissions.Invoices.Post];}
