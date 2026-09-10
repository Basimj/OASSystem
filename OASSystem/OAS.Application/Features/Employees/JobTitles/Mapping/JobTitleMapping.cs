using OAS.Contracts.Features.Employees.JobTitles;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.JobTitles.Mapping;

public static class JobTitleMapping
{
    public static JobTitleDto ToDto(JobTitle item) => new(
        item.Id,
        item.Name,
        item.IsActive,
        Convert.ToBase64String(item.RowVersion),
        item.CreatedAtUtc,
        item.LastModifiedAtUtc);
}
