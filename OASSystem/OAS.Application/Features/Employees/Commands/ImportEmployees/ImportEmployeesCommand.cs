using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees.Import;

namespace OAS.Application.Features.Employees.Commands.ImportEmployees;

public sealed record ImportEmployeesCommand(
    byte[] FileContent)
    : ICommand<EmployeeImportResultDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [EmployeePermissions.Create];
}