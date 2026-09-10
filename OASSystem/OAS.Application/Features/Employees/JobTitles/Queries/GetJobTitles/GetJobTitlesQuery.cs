using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees.JobTitles;

namespace OAS.Application.Features.Employees.JobTitles.Queries.GetJobTitles;

public sealed record GetJobTitlesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<JobTitleDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [EmployeePermissions.View];
}
