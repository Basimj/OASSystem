using OAS.Application.Abstractions.Messaging;using OAS.Application.Sales.Authorization;using OAS.Contracts.Sales.PriceOverrides;
namespace OAS.Application.Sales.PriceOverrides.Commands;
public sealed record RejectSalesPriceOverrideCommand(Guid OverrideId,RejectSalesPriceOverrideRequest Request):ICommand<SalesPriceOverrideDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[SalesPermissions.PriceOverrideApprove];}
