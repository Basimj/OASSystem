namespace OAS.Contracts.Features.Employees.JobTitles;

public sealed record UpdateJobTitleRequest(string Name, bool IsActive, string RowVersion);
