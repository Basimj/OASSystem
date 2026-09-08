
using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Specifications;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler(
    IRepository<Employee, Guid> employeeRepository,
    IRepository<UserAccount, Guid> userRepository)
    : IRequestHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        if (string.IsNullOrWhiteSpace(dto.EmployeeCode))
        {
            throw new ConflictException(
                "employee_code_required",
                "Employee code is required.");
        }

        var employeeCode = dto.EmployeeCode.Trim();

        var employeeCodeSpecification =
            new EmployeeCodeSpecification(employeeCode);

        var codeExists = await employeeRepository.CountAsync(
            employeeCodeSpecification,
            cancellationToken);

        if (codeExists > 0)
        {
            throw new ConflictException(
                "employee_code_already_exists",
                "The employee code already exists.");
        }

        if (dto.UserAccountId is Guid userAccountId)
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
                new EmployeeUserAccountSpecification(userAccountId);

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

        var employee = Employee.Create(
            Guid.NewGuid(),
            employeeCode,
            dto.FirstName,
            dto.LastName,
            dto.Phone,
            dto.JobTitle,
            dto.HireDate,
            dto.Notes,
            dto.IsSalesperson,
            dto.IsTechnician,
            dto.IsCommissionEligible,
            dto.IsActive,
            dto.UserAccountId);

        await employeeRepository.AddAsync(
            employee,
            cancellationToken);

        return employee.Id;
    }
}

