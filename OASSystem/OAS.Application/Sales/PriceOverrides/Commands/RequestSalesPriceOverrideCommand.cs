using OAS.Application.Abstractions.Messaging;using OAS.Application.Sales.Authorization;using OAS.Contracts.Sales.PriceOverrides;
namespace OAS.Application.Sales.PriceOverrides.Commands;
public sealed record RequestSalesPriceOverrideCommand(Guid InvoiceId,RequestSalesPriceOverrideRequest Request):ICommand<SalesPriceOverrideDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[SalesPermissions.PriceOverrideRequest];}
