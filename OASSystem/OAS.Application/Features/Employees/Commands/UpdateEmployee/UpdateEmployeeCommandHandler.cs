using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Specifications;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler(
    IRepository<Employee, Guid> employeeRepository,
    IRepository<UserAccount, Guid> userRepository)
    : IRequestHandler<UpdateEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetForUpdateAsync(
            request.EmployeeId,
            cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException(
                nameof(Employee),
                request.EmployeeId);
        }

        var incomingRowVersion =
            Convert.FromBase64String(request.Request.RowVersion);

        if (!incomingRowVersion.SequenceEqual(employee.RowVersion))
        {
            throw new ConcurrencyException(
                "The employee was changed by another operation. Reload it and try again.");
        }

        var normalizedCode =
            request.Request.EmployeeCode
                .Trim()
                .ToUpperInvariant();

        var codeSpecification =
            new EmployeeCodeSpecification(
                normalizedCode,
                request.EmployeeId);

        var codeExists = await employeeRepository.CountAsync(
            codeSpecification,
            cancellationToken);

        if (codeExists > 0)
        {
            throw new ConflictException(
                "employee_code_exists",
                "Employee code already exists.");
        }

        if (request.Request.UserAccountId is Guid userAccountId)
        {
            var user = await userRepository.GetByIdAsync(
                userAccountId,
                cancellationToken);

            if (user is null)
            {
                throw new ConflictException(
                    "employee_user_account_not_found",
                    "The selected user account does not exist.");
            }

            var userSpecification =
                new EmployeeUserAccountSpecification(
                    userAccountId,
                    request.EmployeeId);

            var alreadyLinked = await employeeRepository.CountAsync(
                userSpecification,
                cancellationToken);

            if (alreadyLinked > 0)
            {
                throw new ConflictException(
                    "employee_user_account_already_linked",
                    "The selected user account is already linked to another employee.");
            }
        }

        var dto = request.Request;

        employee.UpdateDetails(
            dto.EmployeeCode,
            dto.FirstName,
            dto.LastName,
            dto.Phone,
            dto.JobTitle,
            dto.HireDate,
            dto.Notes);

        employee.SetCapabilities(
            dto.IsSalesperson,
            dto.IsTechnician,
            dto.IsCommissionEligible);

        employee.SetUserAccount(dto.UserAccountId);

        employeeRepository.Update(employee);

        return employee.Id;
    }
}