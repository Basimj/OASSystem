using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Common;
namespace OAS.Application.Sales.SalesInvoices.Commands;
public sealed record ReserveSalesInvoiceCodeCommand(DateOnly InvoiceDate):ICommand<SalesCodeReservationDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[SalesPermissions.Invoices.Create];}
