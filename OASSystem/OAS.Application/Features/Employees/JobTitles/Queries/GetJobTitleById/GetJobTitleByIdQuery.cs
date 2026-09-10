using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees.JobTitles;

namespace OAS.Application.Features.Employees.JobTitles.Queries.GetJobTitleById;

public sealed record GetJobTitleByIdQuery(Guid Id) : IQuery<JobTitleDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [EmployeePermissions.View];
}
