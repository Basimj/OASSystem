using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using MediatR;

namespace OAS.Application.Sales.Prescriptions.Queries;

public sealed record GetPrescriptionDashboardSummaryQuery : IQuery<PrescriptionDashboardSummaryDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Prescriptions.View];
}

public sealed class GetPrescriptionDashboardSummaryQueryHandler(
    IPrescriptionAggregateRepository prescriptions,
    IReadRepository<PrescriptionRevision, Guid> revisions)
    : IRequestHandler<GetPrescriptionDashboardSummaryQuery, PrescriptionDashboardSummaryDto>
{
    public async Task<PrescriptionDashboardSummaryDto> Handle(GetPrescriptionDashboardSummaryQuery request, CancellationToken ct)
    {
        var total = await prescriptions.CountAsync(cancellationToken: ct);
        var drafts = await prescriptions.CountAsync(new Specification<Prescription>()
            .Where(x => x.Status == PrescriptionStatus.Draft), ct);
        var active = await prescriptions.CountAsync(new Specification<Prescription>()
            .Where(x => x.Status == PrescriptionStatus.Active), ct);
        var cancelled = await prescriptions.CountAsync(new Specification<Prescription>()
            .Where(x => x.Status == PrescriptionStatus.Cancelled), ct);
        var revisionCount = await revisions.CountAsync(cancellationToken: ct);
        return new(total, drafts, active, cancelled, revisionCount);
    }
}
