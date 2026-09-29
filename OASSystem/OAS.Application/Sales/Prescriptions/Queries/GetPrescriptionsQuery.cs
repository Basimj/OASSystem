using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.Application.Sales.Prescriptions.Queries;

public sealed record GetPrescriptionsQuery(PageRequest Request) : IQuery<PagedResult<PrescriptionDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Prescriptions.View];
}
