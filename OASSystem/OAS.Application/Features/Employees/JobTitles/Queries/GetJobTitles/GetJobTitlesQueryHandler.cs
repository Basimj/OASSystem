using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.JobTitles.Mapping;
using OAS.Contracts.Features.Employees.JobTitles;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.JobTitles.Queries.GetJobTitles;

public sealed class GetJobTitlesQueryHandler(IReadRepository<JobTitle, Guid> repository)
    : IRequestHandler<GetJobTitlesQuery, IReadOnlyList<JobTitleDto>>
{
    public async Task<IReadOnlyList<JobTitleDto>> Handle(GetJobTitlesQuery request, CancellationToken cancellationToken)
    {
        var items = await repository.ListAsync(cancellationToken: cancellationToken);
        return items
            .Where(x => !request.ActiveOnly || x.IsActive)
            .OrderBy(x => x.Name)
            .Select(JobTitleMapping.ToDto)
            .ToArray();
    }
}
