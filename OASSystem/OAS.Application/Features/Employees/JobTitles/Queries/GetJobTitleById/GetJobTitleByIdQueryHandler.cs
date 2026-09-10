using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.JobTitles.Mapping;
using OAS.Contracts.Features.Employees.JobTitles;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.JobTitles.Queries.GetJobTitleById;

public sealed class GetJobTitleByIdQueryHandler(IReadRepository<JobTitle, Guid> repository)
    : IRequestHandler<GetJobTitleByIdQuery, JobTitleDto>
{
    public async Task<JobTitleDto> Handle(GetJobTitleByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(JobTitle), request.Id);
        return JobTitleMapping.ToDto(item);
    }
}
