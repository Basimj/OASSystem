using OAS.Application.Abstractions.Messaging;using OAS.Application.Sales.Authorization;using OAS.Contracts.Sales.PriceOverrides;
namespace OAS.Application.Sales.PriceOverrides.Commands;
public sealed record ApproveSalesPriceOverrideCommand(Guid OverrideId,ApproveSalesPriceOverrideRequest Request):ICommand<SalesPriceOverrideDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[SalesPermissions.PriceOverrideApprove];}
