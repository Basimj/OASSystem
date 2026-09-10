using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.JobTitles.Specifications;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.JobTitles.Commands.UpdateJobTitle;

public sealed class UpdateJobTitleCommandHandler(IRepository<JobTitle, Guid> repository)
    : IRequestHandler<UpdateJobTitleCommand, Guid>
{
    public async Task<Guid> Handle(UpdateJobTitleCommand request, CancellationToken cancellationToken)
    {
        var item = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(JobTitle), request.Id);

        var incoming = Convert.FromBase64String(request.Request.RowVersion);
        if (!incoming.SequenceEqual(item.RowVersion))
            throw new ConcurrencyException("The job title was changed by another operation. Reload it and try again.");

        var name = request.Request.Name.Trim();
        if (await repository.CountAsync(new JobTitleNameSpecification(name, request.Id), cancellationToken) > 0)
            throw new ConflictException("job_title_exists", "Job title already exists.");

        item.Update(name, request.Request.IsActive);
        repository.Update(item);
        return item.Id;
    }
}
