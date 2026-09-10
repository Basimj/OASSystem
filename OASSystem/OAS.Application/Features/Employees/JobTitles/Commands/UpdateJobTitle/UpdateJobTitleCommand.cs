using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees.JobTitles;

namespace OAS.Application.Features.Employees.JobTitles.Commands.UpdateJobTitle;

public sealed record UpdateJobTitleCommand(Guid Id, UpdateJobTitleRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [EmployeePermissions.Edit];
}
