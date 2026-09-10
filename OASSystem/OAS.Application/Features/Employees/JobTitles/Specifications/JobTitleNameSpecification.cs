using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.JobTitles.Specifications;

public sealed class JobTitleNameSpecification : Specification<JobTitle>
{
    public JobTitleNameSpecification(string name, Guid? excludingId = null)
    {
        var normalized = name.Trim();
        Where(x => x.Name == normalized && (!excludingId.HasValue || x.Id != excludingId.Value));
    }
}
