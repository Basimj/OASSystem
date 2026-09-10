using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.JobTitles.Specifications;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.JobTitles.Commands.CreateJobTitle;

public sealed class CreateJobTitleCommandHandler(IRepository<JobTitle, Guid> repository)
    : IRequestHandler<CreateJobTitleCommand, Guid>
{
    public async Task<Guid> Handle(CreateJobTitleCommand request, CancellationToken cancellationToken)
    {
        var name = request.Request.Name.Trim();
        if (await repository.CountAsync(new JobTitleNameSpecification(name), cancellationToken) > 0)
            throw new ConflictException("job_title_exists", "Job title already exists.");

        var item = JobTitle.Create(Guid.NewGuid(), name, request.Request.IsActive);
        await repository.AddAsync(item, cancellationToken);
        return item.Id;
    }
}
