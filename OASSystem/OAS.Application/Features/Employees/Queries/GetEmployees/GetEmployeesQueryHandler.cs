using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.Mapping;
using OAS.Application.Features.Employees.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryHandler(
    IReadRepository<Employee, Guid> employeeRepository)
    : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeDto>>
{
    public async Task<PagedResult<EmployeeDto>> Handle(
        GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();

        var specification =
            new EmployeePageSpecification(normalized);

        var page = await employeeRepository.GetPageAsync(
            specification,
            cancellationToken);

        return new PagedResult<EmployeeDto>
        {
            Items = page.Items
                .Select(EmployeeMapping.ToDto)
                .ToArray(),

            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}