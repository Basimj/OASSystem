using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Specifications;
using OAS.Domain.Features.Employees;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.ValueObjects;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler(
    IRepository<Employee, Guid> employeeRepository,
    IReadRepository<JobTitle, Guid> jobTitleRepository,
    IRepository<UserAccount, Guid> userRepository,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var jobTitle = await jobTitleRepository.GetByIdAsync(
            dto.JobTitleId,
            cancellationToken);

        if (jobTitle is null)
            throw new ConflictException(
                "job_title_not_found",
                "The selected job title does not exist.");

        if (!jobTitle.IsActive)
            throw new ConflictException(
                "job_title_inactive",
                "The selected job title is inactive.");

        if (dto.UserAccountId is Guid userAccountId)
        {
            await EnsureUserCanBeLinkedAsync(
                userAccountId,
                employeeRepository,
                userRepository,
                cancellationToken);
        }

        var employeeCode = dto.EmployeeCode?.Trim();

        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            var next = await sequenceNumberGenerator.NextAsync(
                "EmployeeNumberSequence",
                cancellationToken);

            if (next <= 0)
            {
                throw new ConflictException(
                    "employee_code_exhausted",
                    "Employee code sequence exceeded the supported range.");
            }

            employeeCode = EmployeeCodeFormatter.Format(next);
        }

        var contactInfo = ContactInfo.Create(
            dto.Phone,
            dto.Email,
            Address.Create(
                dto.Country,
                dto.Governorate,
                dto.City,
                dto.PostalCode,
                dto.ResidentialAddress));

        var employee = Employee.Create(
            Guid.NewGuid(),
            employeeCode,
            dto.FirstName,
            dto.LastName,
            contactInfo,
            dto.JobTitleId,
            dto.HireDate,
            dto.IsCommissionEligible,
            dto.IsActive,
            dto.UserAccountId);

        await employeeRepository.AddAsync(
            employee,
            cancellationToken);

        return employee.Id;
    }

    private static async Task EnsureUserCanBeLinkedAsync(
        Guid userAccountId,
        IRepository<Employee, Guid> employeeRepository,
        IRepository<UserAccount, Guid> userRepository,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            userAccountId,
            cancellationToken);

        if (user is null || user.IsSuperAdmin)
        {
            throw new ConflictException(
                "employee_user_account_not_found",
                "The selected user account is not available.");
        }

        if (await employeeRepository.CountAsync(
                new EmployeeUserAccountSpecification(userAccountId),
                cancellationToken) > 0)
        {
            throw new ConflictException(
                "employee_user_account_already_linked",
                "The selected user account is already linked to another employee.");
        }
    }
}