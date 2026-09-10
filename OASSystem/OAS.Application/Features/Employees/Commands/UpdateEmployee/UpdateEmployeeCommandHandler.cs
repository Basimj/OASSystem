using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Specifications;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.ValueObjects;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler(
    IRepository<Employee, Guid> employeeRepository,
    IReadRepository<JobTitle, Guid> jobTitleRepository,
    IRepository<UserAccount, Guid> userRepository)
    : IRequestHandler<UpdateEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetForUpdateAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var incomingRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!incomingRowVersion.SequenceEqual(employee.RowVersion))
            throw new ConcurrencyException("The employee was changed by another operation. Reload it and try again.");

        var dto = request.Request;
        var jobTitle = await jobTitleRepository.GetByIdAsync(dto.JobTitleId, cancellationToken);
        if (jobTitle is null)
            throw new ConflictException("job_title_not_found", "The selected job title does not exist.");
        if (!jobTitle.IsActive && employee.JobTitleId != dto.JobTitleId)
            throw new ConflictException("job_title_inactive", "The selected job title is inactive.");

        await ValidateUserLinkAsync(employee, dto.UserAccountId, employeeRepository, userRepository, cancellationToken);

        var contactInfo = ContactInfo.Create(
            dto.Phone,
            dto.Email,
            Address.Create(dto.Country, dto.Governorate, dto.City, dto.PostalCode, dto.ResidentialAddress));

        employee.UpdateDetails(
            dto.FirstName,
            dto.LastName,
            contactInfo,
            dto.JobTitleId,
            dto.HireDate,
            dto.IsCommissionEligible,
            dto.IsActive);

        employee.SetUserAccountLink(dto.UserAccountId);
        employeeRepository.Update(employee);
        return employee.Id;
    }

    private static async Task ValidateUserLinkAsync(
        Employee employee,
        Guid? requestedUserAccountId,
        IRepository<Employee, Guid> employeeRepository,
        IRepository<UserAccount, Guid> userRepository,
        CancellationToken cancellationToken)
    {
        if (employee.UserAccountId.HasValue)
        {
            if (requestedUserAccountId != employee.UserAccountId)
                throw new ConflictException("employee_user_account_immutable", "The linked user account cannot be changed or removed.");
            return;
        }

        if (requestedUserAccountId is not Guid userAccountId) return;

        var user = await userRepository.GetByIdAsync(userAccountId, cancellationToken);
        if (user is null || user.IsSuperAdmin)
            throw new ConflictException("employee_user_account_not_found", "The selected user account is not available.");

        if (await employeeRepository.CountAsync(
                new EmployeeUserAccountSpecification(userAccountId, employee.Id),
                cancellationToken) > 0)
            throw new ConflictException("employee_user_account_already_linked", "The selected user account is already linked to another employee.");
    }
}
