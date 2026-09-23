using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Customers;
namespace OAS.Application.Accounting.Customers.Commands.UpdateCustomer;
public sealed record UpdateCustomerCommand(Guid Id,UpdateCustomerRequest Request):ICommand,IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; }=[AccountingPermissions.Customers.Edit];
}
