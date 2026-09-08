using OAS.Application.Features.Employees.Authorization;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Abstractions.Messaging;
using OAS.Contracts.Features.Employees.Import;

namespace OAS.Application.Features.Employees.Commands.ValidateEmployeesImport;

public sealed record ValidateEmployeesImportCommand(
    byte[] FileContent)
    : ICommand<EmployeeImportPreviewDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [EmployeePermissions.Create];
}