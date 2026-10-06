using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.CustomerDemand;

namespace OAS.Application.Purchasing.CustomerDemand.Commands;

public sealed record AssignCustomerDemandSupplierCommand(Guid PurchaseRequestLineId, AssignCustomerDemandSupplierRequest Request)
    : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.CustomerDemand.EditSupplier]; }

public sealed record ScheduleCustomerDemandCommand(Guid PurchaseRequestLineId, ScheduleCustomerDemandRequest Request)
    : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.CustomerDemand.EditSupplier]; }

public sealed record CreateCustomerDemandPurchaseOrderCommand(Guid PurchaseRequestLineId, CreateCustomerDemandPurchaseOrderRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.CustomerDemand.CreatePO]; }

public sealed record ResourceCustomerDemandRemainingCommand(Guid PurchaseRequestLineId, ResourceCustomerDemandRemainingRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.CustomerDemand.Resupply]; }

public sealed class AssignCustomerDemandSupplierCommandHandler(ICustomerDemandSourcingService service)
    : IRequestHandler<AssignCustomerDemandSupplierCommand>
{ public Task Handle(AssignCustomerDemandSupplierCommand request, CancellationToken ct) => service.AssignSupplierAsync(request.PurchaseRequestLineId, request.Request, ct); }

public sealed class ScheduleCustomerDemandCommandHandler(ICustomerDemandSourcingService service)
    : IRequestHandler<ScheduleCustomerDemandCommand>
{ public Task Handle(ScheduleCustomerDemandCommand request, CancellationToken ct) => service.ScheduleAsync(request.PurchaseRequestLineId, request.Request, ct); }

public sealed class CreateCustomerDemandPurchaseOrderCommandHandler(ICustomerDemandSourcingService service)
    : IRequestHandler<CreateCustomerDemandPurchaseOrderCommand, Guid>
{ public Task<Guid> Handle(CreateCustomerDemandPurchaseOrderCommand request, CancellationToken ct) => service.CreatePurchaseOrderAsync(request.PurchaseRequestLineId, request.Request, ct); }

public sealed class ResourceCustomerDemandRemainingCommandHandler(ICustomerDemandSourcingService service)
    : IRequestHandler<ResourceCustomerDemandRemainingCommand, Guid>
{ public Task<Guid> Handle(ResourceCustomerDemandRemainingCommand request, CancellationToken ct) => service.ResourceRemainingAsync(request.PurchaseRequestLineId, request.Request, ct); }
