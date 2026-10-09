using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.Application.Sales.OpticalJobs.Commands;

public sealed record CreateOpticalJobCommand(CreateOpticalJobRequest Request) : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Start]; }
public sealed record AssignOpticalJobCommand(Guid Id, AssignOpticalJobRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Assign]; }
public sealed record IssueOpticalJobMaterialsCommand(Guid Id, IssueOpticalJobMaterialsRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.IssueMaterials]; }
public sealed record StartOpticalJobCommand(Guid Id, OpticalJobActionRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Start]; }
public sealed record SendOpticalJobToQualityControlCommand(Guid Id, OpticalJobActionRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.QC]; }
public sealed record CompleteOpticalQualityControlCommand(Guid Id, Guid QualityCheckId, CompleteOpticalQualityCheckRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.QC]; }
public sealed record PassOpticalJobQualityControlCommand(Guid Id, OpticalJobActionRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.QC]; }
public sealed record RecordOpticalJobBreakageCommand(Guid Id, RecordOpticalJobBreakageRequest Request) : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.RecordBreakage]; }
public sealed record CreateOpticalJobRemakeCommand(Guid Id, CreateOpticalJobRemakeRequest Request) : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Remake]; }
public sealed record StartOpticalJobRemakeCommand(Guid Id, Guid RemakeId, OpticalJobRemakeActionRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Remake]; }
public sealed record SendOpticalJobRemakeToQcCommand(Guid Id, Guid RemakeId, OpticalJobRemakeActionRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Remake]; }
public sealed record MarkOpticalJobReadyCommand(Guid Id, OpticalJobActionRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.MarkReady]; }
public sealed record DeliverOpticalJobCommand(Guid Id, OpticalJobActionRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Deliver]; }
public sealed record CancelOpticalJobCommand(Guid Id, OpticalJobActionRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.Cancel]; }

public sealed class CreateOpticalJobCommandHandler(IOpticalJobService service) : IRequestHandler<CreateOpticalJobCommand, Guid>
{ public Task<Guid> Handle(CreateOpticalJobCommand r, CancellationToken ct) => service.CreateAsync(r.Request, ct); }
public sealed class AssignOpticalJobCommandHandler(IOpticalJobService service) : IRequestHandler<AssignOpticalJobCommand>
{ public Task Handle(AssignOpticalJobCommand r, CancellationToken ct) => service.AssignAsync(r.Id, r.Request, ct); }
public sealed class IssueOpticalJobMaterialsCommandHandler(IOpticalJobService service) : IRequestHandler<IssueOpticalJobMaterialsCommand>
{ public Task Handle(IssueOpticalJobMaterialsCommand r, CancellationToken ct) => service.IssueMaterialsAsync(r.Id, r.Request, ct); }
public sealed class StartOpticalJobCommandHandler(IOpticalJobService service) : IRequestHandler<StartOpticalJobCommand>
{ public Task Handle(StartOpticalJobCommand r, CancellationToken ct) => service.StartAsync(r.Id, r.Request, ct); }
public sealed class SendOpticalJobToQualityControlCommandHandler(IOpticalJobService service) : IRequestHandler<SendOpticalJobToQualityControlCommand>
{ public Task Handle(SendOpticalJobToQualityControlCommand r, CancellationToken ct) => service.SendToQualityControlAsync(r.Id, r.Request, ct); }
public sealed class CompleteOpticalQualityControlCommandHandler(IOpticalJobService service) : IRequestHandler<CompleteOpticalQualityControlCommand>
{ public Task Handle(CompleteOpticalQualityControlCommand r, CancellationToken ct) => service.CompleteQualityControlAsync(r.Id, r.QualityCheckId, r.Request, ct); }
public sealed class PassOpticalJobQualityControlCommandHandler(IOpticalJobService service) : IRequestHandler<PassOpticalJobQualityControlCommand>
{ public Task Handle(PassOpticalJobQualityControlCommand r, CancellationToken ct) => service.PassQualityControlAsync(r.Id, r.Request, ct); }
public sealed class RecordOpticalJobBreakageCommandHandler(IOpticalJobService service) : IRequestHandler<RecordOpticalJobBreakageCommand, Guid>
{ public Task<Guid> Handle(RecordOpticalJobBreakageCommand r, CancellationToken ct) => service.RecordBreakageAsync(r.Id, r.Request, ct); }
public sealed class CreateOpticalJobRemakeCommandHandler(IOpticalJobService service) : IRequestHandler<CreateOpticalJobRemakeCommand, Guid>
{ public Task<Guid> Handle(CreateOpticalJobRemakeCommand r, CancellationToken ct) => service.CreateRemakeAsync(r.Id, r.Request, ct); }
public sealed class StartOpticalJobRemakeCommandHandler(IOpticalJobService service) : IRequestHandler<StartOpticalJobRemakeCommand>
{ public Task Handle(StartOpticalJobRemakeCommand r, CancellationToken ct) => service.StartRemakeAsync(r.Id, r.RemakeId, r.Request, ct); }
public sealed class SendOpticalJobRemakeToQcCommandHandler(IOpticalJobService service) : IRequestHandler<SendOpticalJobRemakeToQcCommand>
{ public Task Handle(SendOpticalJobRemakeToQcCommand r, CancellationToken ct) => service.SendRemakeToQualityControlAsync(r.Id, r.RemakeId, r.Request, ct); }
public sealed class MarkOpticalJobReadyCommandHandler(IOpticalJobService service) : IRequestHandler<MarkOpticalJobReadyCommand>
{ public Task Handle(MarkOpticalJobReadyCommand r, CancellationToken ct) => service.MarkReadyAsync(r.Id, r.Request, ct); }
public sealed class DeliverOpticalJobCommandHandler(IOpticalJobService service) : IRequestHandler<DeliverOpticalJobCommand>
{ public Task Handle(DeliverOpticalJobCommand r, CancellationToken ct) => service.DeliverAsync(r.Id, r.Request, ct); }
public sealed class CancelOpticalJobCommandHandler(IOpticalJobService service) : IRequestHandler<CancelOpticalJobCommand>
{ public Task Handle(CancelOpticalJobCommand r, CancellationToken ct) => service.CancelAsync(r.Id, r.Request, ct); }
