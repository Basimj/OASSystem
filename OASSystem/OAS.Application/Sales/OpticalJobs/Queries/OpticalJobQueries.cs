using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.Application.Sales.OpticalJobs.Queries;

public sealed record GetOpticalJobQuery(Guid Id)
    : IQuery<OpticalJobDetailsDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.View];
}

public sealed record GetOpticalJobWorkQueueQuery
    : IQuery<IReadOnlyList<OpticalJobWorkQueueDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OpticalJobs.View];
}

public sealed class GetOpticalJobQueryHandler(IOpticalJobService service)
    : IRequestHandler<GetOpticalJobQuery, OpticalJobDetailsDto>
{
    public Task<OpticalJobDetailsDto> Handle(GetOpticalJobQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Id, cancellationToken);
}

public sealed class GetOpticalJobWorkQueueQueryHandler(IOpticalJobService service)
    : IRequestHandler<GetOpticalJobWorkQueueQuery, IReadOnlyList<OpticalJobWorkQueueDto>>
{
    public Task<IReadOnlyList<OpticalJobWorkQueueDto>> Handle(GetOpticalJobWorkQueueQuery request, CancellationToken cancellationToken) =>
        service.GetWorkQueueAsync(cancellationToken);
}
