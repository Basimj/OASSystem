using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.EmployeeAccounts;
using OAS.Contracts.Common.Pagination;
namespace OAS.Application.Accounting.EmployeeAccounts.Queries.GetEmployeeAccounts;
public sealed record GetEmployeeAccountsQuery(PageRequest Request):IQuery<PagedResult<EmployeeAccountDto>>,IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.EmployeeAccounts.View];
}
