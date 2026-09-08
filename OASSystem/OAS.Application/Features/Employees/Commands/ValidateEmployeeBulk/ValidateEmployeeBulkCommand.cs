using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Contracts.Features.Employees.Import;

namespace OAS.Application.Features.Employees.Commands.ValidateEmployeeBulk;

public sealed record ValidateEmployeeBulkCommand(
    EmployeeBulkValidationRequest Request)
    : ICommand<EmployeeBulkValidationResultDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [EmployeePermissions.Create];
}