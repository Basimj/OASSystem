namespace OAS.Contracts.Features.Employees.JobTitles;

public sealed record CreateJobTitleRequest(string Name, bool IsActive = true);
