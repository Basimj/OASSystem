using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.EmployeeAccounts;
namespace OAS.Application.Accounting.EmployeeAccounts.Queries.GetEmployeeAccountById;
public sealed record GetEmployeeAccountByIdQuery(Guid Id):IQuery<EmployeeAccountDto>,IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.EmployeeAccounts.View];
}
