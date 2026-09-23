using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Customers;
namespace OAS.Application.Accounting.Customers.Commands.CreateCustomer;
public sealed record CreateCustomerCommand(CreateCustomerRequest Request):ICommand<Guid>,IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; }=[AccountingPermissions.Customers.Create];
}
