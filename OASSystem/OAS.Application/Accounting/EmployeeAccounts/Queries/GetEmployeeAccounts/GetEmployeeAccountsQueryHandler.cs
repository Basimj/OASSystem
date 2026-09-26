using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Accounting.EmployeeAccounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Accounting.EmployeeAccounts.Queries.GetEmployeeAccounts;

public sealed class GetEmployeeAccountsQueryHandler(
    IReadRepository<EmployeeAccount, Guid> repository,
    IReadRepository<Employee, Guid> employees,
    IReadRepository<Account, Guid> accounts)
    : IRequestHandler<GetEmployeeAccountsQuery, PagedResult<EmployeeAccountDto>>
{
    public async Task<PagedResult<EmployeeAccountDto>> Handle(GetEmployeeAccountsQuery request, CancellationToken ct)
    {
        var pageRequest = request.Request.Normalize();
        var spec = new Specification<EmployeeAccount>();

        if (!string.IsNullOrWhiteSpace(pageRequest.Search))
        {
            var term = pageRequest.Search.Trim();
            var employeeMatches = await employees.ListAsync(
                new Specification<Employee>()
                    .Where(x => x.EmployeeCode.Contains(term) || x.FirstName.Contains(term) || x.LastName.Contains(term))
                    .ApplyPaging(0, 500), ct);
            var accountMatches = await accounts.ListAsync(
                new Specification<Account>()
                    .Where(x => x.Code.Contains(term) || x.NameAr.Contains(term) || (x.NameEn != null && x.NameEn.Contains(term)))
                    .ApplyPaging(0, 500), ct);

            var employeeIds = employeeMatches.Select(x => x.Id).ToArray();
            var accountIds = accountMatches.Select(x => x.Id).ToArray();
            spec.Where(x => employeeIds.Contains(x.EmployeeId) || accountIds.Contains(x.AccountId));
        }

        spec.AddSort(nameof(EmployeeAccount.CreatedAtUtc), SortDirection.Descending)
            .ApplyPaging((pageRequest.PageNumber - 1) * pageRequest.PageSize, pageRequest.PageSize);

        var page = await repository.GetPageAsync(spec, ct);
        var employeeIdsOnPage = page.Items.Select(x => x.EmployeeId).Distinct().ToArray();
        var accountIdsOnPage = page.Items.Select(x => x.AccountId).Distinct().ToArray();

        var employeeMap = employeeIdsOnPage.Length == 0
            ? new Dictionary<Guid, Employee>()
            : (await employees.ListAsync(new Specification<Employee>().Where(x => employeeIdsOnPage.Contains(x.Id)), ct)).ToDictionary(x => x.Id);
        var accountMap = accountIdsOnPage.Length == 0
            ? new Dictionary<Guid, Account>()
            : (await accounts.ListAsync(new Specification<Account>().Where(x => accountIdsOnPage.Contains(x.Id)), ct)).ToDictionary(x => x.Id);

        var items = page.Items.Select(mapping =>
        {
            employeeMap.TryGetValue(mapping.EmployeeId, out var employee);
            accountMap.TryGetValue(mapping.AccountId, out var account);
            return new EmployeeAccountDto(
                mapping.Id,
                mapping.EmployeeId,
                employee?.EmployeeCode ?? string.Empty,
                employee?.DisplayName ?? string.Empty,
                mapping.AccountId,
                account?.Code ?? string.Empty,
                account?.NameAr ?? string.Empty,
                mapping.IsActive,
                Convert.ToBase64String(mapping.RowVersion));
        }).ToArray();

        return new PagedResult<EmployeeAccountDto>
        {
            Items = items,
            PageNumber = pageRequest.PageNumber,
            PageSize = pageRequest.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
