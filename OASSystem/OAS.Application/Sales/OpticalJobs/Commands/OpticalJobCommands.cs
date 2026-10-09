using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.Application.Sales.OpticalJobs.Commands;

public sealed record CreateOpticalJobCommand(CreateOpticalJobRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Start];
}

public sealed record AssignOpticalJobCommand(Guid Id, AssignOpticalJobRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Assign];
}

public sealed record StartOpticalJobCommand(Guid Id, OpticalJobActionRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Start];
}

public sealed record SendOpticalJobToQualityControlCommand(Guid Id, OpticalJobActionRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Complete];
}

public sealed record PassOpticalJobQualityControlCommand(Guid Id, OpticalJobActionRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Complete];
}

public sealed record MarkOpticalJobReadyCommand(Guid Id, OpticalJobActionRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Complete];
}

public sealed class CreateOpticalJobCommandHandler(IOpticalJobService service)
    : IRequestHandler<CreateOpticalJobCommand, Guid>
{
    public Task<Guid> Handle(CreateOpticalJobCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Request, cancellationToken);
}

public sealed class AssignOpticalJobCommandHandler(IOpticalJobService service)
    : IRequestHandler<AssignOpticalJobCommand>
{
    public Task Handle(AssignOpticalJobCommand request, CancellationToken cancellationToken) =>
        service.AssignAsync(request.Id, request.Request, cancellationToken);
}

public sealed class StartOpticalJobCommandHandler(IOpticalJobService service)
    : IRequestHandler<StartOpticalJobCommand>
{
    public Task Handle(StartOpticalJobCommand request, CancellationToken cancellationToken) =>
        service.StartAsync(request.Id, request.Request, cancellationToken);
}

public sealed class SendOpticalJobToQualityControlCommandHandler(IOpticalJobService service)
    : IRequestHandler<SendOpticalJobToQualityControlCommand>
{
    public Task Handle(SendOpticalJobToQualityControlCommand request, CancellationToken cancellationToken) =>
        service.SendToQualityControlAsync(request.Id, request.Request, cancellationToken);
}

public sealed class PassOpticalJobQualityControlCommandHandler(IOpticalJobService service)
    : IRequestHandler<PassOpticalJobQualityControlCommand>
{
    public Task Handle(PassOpticalJobQualityControlCommand request, CancellationToken cancellationToken) =>
        service.PassQualityControlAsync(request.Id, request.Request, cancellationToken);
}

public sealed class MarkOpticalJobReadyCommandHandler(IOpticalJobService service)
    : IRequestHandler<MarkOpticalJobReadyCommand>
{
    public Task Handle(MarkOpticalJobReadyCommand request, CancellationToken cancellationToken) =>
        service.MarkReadyAsync(request.Id, request.Request, cancellationToken);
}
